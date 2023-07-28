using System;
using System.IO;
using System.Text;
using System.Reflection;
using System.Runtime.InteropServices;
using LibSnitcher.Interop;
using System.Collections.Generic;

#nullable enable

namespace LibSnitcher;

public class Helpers
{
    // For .NET assemblies, the full name.
    // For libraries, the name.
    private readonly List<string> _processedNames = new();

    public LibraryInfo GetLibraryInfo(string filePath)
    {
        LibraryInfo output = new(Path.GetFileName(filePath)) {
            Path = filePath,
        };

        try
        {
            Assembly inputAssembly = Assembly.LoadFrom(filePath);
            output.IsLoaded = true;
            output.IsClr = true;
            output.ClrAssembly = inputAssembly;

            foreach (AssemblyName managedAssembly in inputAssembly.GetReferencedAssemblies())
            {
                LibraryInfo? info = GetReferencedAssemblyInfo(managedAssembly, inputAssembly.FullName);
                if (info is not null)
                    output.ClrReferencedAssemblies.Add(info);
            }

            using SafeModuleHandle hModule = NativeFunctions.LoadLibrary(filePath);
            PortableExecutable peHeaders = new(hModule);
            if (peHeaders.OptionalHeaders is not null)
                GetLibraryTables(inputAssembly.FullName, peHeaders.OptionalHeaders, ref output, hModule.DangerousGetHandle());
        }
        catch (Exception ex)
        {
            output.LoadException = ex;
        }

        return output;
    }

    internal LibraryInfo? GetReferencedAssemblyInfo(AssemblyName refAssemblyName, string? parent)
    {
        if (_processedNames.Contains(refAssemblyName.FullName))
            return null;

        LibraryInfo output = new(refAssemblyName.Name) {
            Parent = parent
        };

        try
        {
            // Attempting to get current assembly information.
            Assembly currentAssembly = Assembly.Load(refAssemblyName);
            output.IsLoaded = true;
            output.IsClr = true;
            output.ClrAssembly = currentAssembly;
            output.Path = currentAssembly.Location;

            // Attempting to get managed referenced assembly information recursively.
            foreach (AssemblyName assemblyName in currentAssembly.GetReferencedAssemblies())
            {
                LibraryInfo? info = GetReferencedAssemblyInfo(assemblyName, refAssemblyName.FullName);
                if (info is not null)
                    output.ClrReferencedAssemblies.Add(info);
            }

            // Attempting to get import, and delay load table information (unmanaged libraries).
            using SafeModuleHandle hModule = NativeFunctions.LoadLibrary(currentAssembly.Location);
            using FileStream stream = new(currentAssembly.Location, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            PortableExecutable peHeaders = new(stream);

            if (peHeaders.OptionalHeaders is not null)
                GetLibraryTables(refAssemblyName.FullName, peHeaders.OptionalHeaders, ref output, hModule.DangerousGetHandle());
        }
        catch (Exception ex)
        {
            output.LoadException = ex;
        }

        _processedNames.Add(refAssemblyName.FullName);

        return output;
    }

    internal LibraryInfo? GetLibraryModuleInfo(string libName, string? parent)
    {
        if (_processedNames.Contains(libName))
            return null;

        LibraryInfo output = new(libName) {
            Parent = parent
        };

        try
        {
            // Calling 'LoadLibrary' instead of 'GetModuleHandle' so we can call 'GetModuleFileName'.
            using SafeModuleHandle hModule = NativeFunctions.LoadLibrary(libName);
            if (hModule is null || hModule.IsInvalid)
                throw new NativeException(Marshal.GetLastWin32Error());

            output.IsLoaded = true;

            // Attempting to get the module path.
            StringBuilder pathBuffer = new(1024);
            int getPathResult = NativeFunctions.GetModuleFileName(hModule, pathBuffer, 1024);
            if (getPathResult != 0 && getPathResult != NativeConstants.ERROR_INSUFFICIENT_BUFFER)
                output.Path = pathBuffer.ToString();

            PortableExecutable peHeaders = new(hModule);
            if (peHeaders.OptionalHeaders is not null)
                GetLibraryTables(libName, peHeaders.OptionalHeaders, ref output, hModule.DangerousGetHandle());
        }
        catch (Exception ex)
        {
            output.LoadException = ex;
        }

        _processedNames.Add(libName);

        return output;
    }

    private void GetLibraryTables(string libName, OptionalHeaders optionalHeaders, ref LibraryInfo libraryInfo, IntPtr dangerousModuleHandle)
    {
        if (_processedNames.Contains(libName))
            return;

        // Import tables.
        if (optionalHeaders.ImportTableDirectory.Size != 0 &&
            optionalHeaders.ImportTableDirectory.VirtualAddress != 0)
        {
            IntPtr offset = (IntPtr)((ulong)dangerousModuleHandle + optionalHeaders.ImportTableDirectory.VirtualAddress);
            int bufferSize = Marshal.SizeOf(typeof(IMAGE_IMPORT_DESCRIPTOR));
            uint nameRva = 0;
            do
            {
                IMAGE_IMPORT_DESCRIPTOR? impDesc = (IMAGE_IMPORT_DESCRIPTOR?)Marshal.PtrToStructure(offset, typeof(IMAGE_IMPORT_DESCRIPTOR));
                if (impDesc is not null)
                {
                    nameRva = impDesc.Value.Name;
                    if (impDesc.Value.Name == 0)
                        break;

                    string? impLibName = Marshal.PtrToStringAnsi((IntPtr)((ulong)dangerousModuleHandle + impDesc.Value.Name));
                    if (impLibName is not null)
                    {
                        // Attempting to get module information recursively.
                        LibraryInfo? info = GetLibraryModuleInfo(impLibName, libName);
                        if (info is not null)
                            libraryInfo.ImportList.Add(info);
                    }
                        
                }

                offset += bufferSize;

            } while (nameRva != 0);
        }

        // Delay load table.
        if (optionalHeaders.DelayImportTableDirectory.Size != 0 &&
            optionalHeaders.DelayImportTableDirectory.VirtualAddress != 0)
        {
            IntPtr offset = (IntPtr)((ulong)dangerousModuleHandle + optionalHeaders.DelayImportTableDirectory.VirtualAddress);
            int bufferSize = Marshal.SizeOf(typeof(IMAGE_DELAYLOAD_DESCRIPTOR));
            uint nameRva = 0;
            do
            {
                IMAGE_DELAYLOAD_DESCRIPTOR? delayLoadDesc = (IMAGE_DELAYLOAD_DESCRIPTOR?)Marshal.PtrToStructure(offset, typeof(IMAGE_DELAYLOAD_DESCRIPTOR));
                if (delayLoadDesc is not null)
                {
                    nameRva = delayLoadDesc.Value.Name;
                    if (delayLoadDesc.Value.Name == 0)
                        break;

                    string? delayLoadLibName = Marshal.PtrToStringAnsi((IntPtr)((ulong)dangerousModuleHandle + delayLoadDesc.Value.Name));
                    if (delayLoadLibName is not null)
                    {
                        // Attempting to get module information recursively.
                        LibraryInfo? info = GetLibraryModuleInfo(delayLoadLibName, libName);
                        if (info is not null)
                            libraryInfo.DelayLoadList.Add(info);
                    }
                        
                }

                offset += bufferSize;

            } while (nameRva != 0);
        }
    }
}