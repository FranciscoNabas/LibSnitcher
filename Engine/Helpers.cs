using System;
using System.IO;
using System.Linq;
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

    public static void PrintLibraryInfo(LibraryInfo info, ref string text)
    {
        Dictionary<string, int> disposition = new();
        PrintLibraryInfoInternal(info, ref text, ref disposition);
    }

    private static void PrintLibraryInfoInternal(LibraryInfo info, ref string text, ref Dictionary<string, int> disposition)
    {
        if (disposition.TryGetValue(info.Name, out int dummy))
            return;

        int depth;
        if (!string.IsNullOrEmpty(info.Parent))
        {
            if (!disposition.TryGetValue(info.Parent, out depth))
                depth = disposition.OrderBy(x => x.Value).Last().Value + 1;
            else
                depth++;

        }
        else
        {
            if (!disposition.TryGetValue(info.Name, out depth))
                depth = 0;
        }

        disposition.Add(info.Name, depth);

        for (int i = 0; i < depth; i++)
            text += ' ';

        if (info.IsClr)
            text += $"{info?.ClrAssembly?.FullName}\r\n";
        else
            text += $"{info.Name}\r\n";

        List<LibraryInfo> allDep = new();
        if (info?.ImportList.Count > 0)
            allDep.AddRange(info?.ImportList);

        if (info?.DelayLoadList.Count > 0)
            allDep.AddRange(info?.DelayLoadList);

        if (info?.ClrReferencedAssemblies.Count > 0)
            allDep.AddRange(info?.ClrReferencedAssemblies);

        foreach (LibraryInfo libInfo in allDep)
            PrintLibraryInfoInternal(libInfo, ref text, ref disposition);
    }

    public LibraryInfo GetLibraryInfo(string filePath)
    {
        string libName = Path.GetFileName(filePath);
        LibraryInfo output = new(libName)
        {
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

            // Attempting to get import, and delay load table information (unmanaged libraries).
            GetLibraryModuleInfo(filePath, ref output);
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

        _processedNames.Add(refAssemblyName.FullName);

        LibraryInfo output = new(refAssemblyName.Name)
        {
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
            LibraryInfo? info;
            foreach (AssemblyName assemblyName in currentAssembly.GetReferencedAssemblies())
            {
                info = GetReferencedAssemblyInfo(assemblyName, refAssemblyName.FullName);
                if (info is not null)
                    output.ClrReferencedAssemblies.Add(info);
            }

            // Attempting to get import, and delay load table information (unmanaged libraries).
            GetLibraryModuleInfo(currentAssembly.Location, ref output);

        }
        catch (Exception ex)
        {
            output.LoadException = ex;
        }

        return output;
    }

    internal void GetLibraryModuleInfo(string libName, ref LibraryInfo libInfo)
    {
        if (_processedNames.Contains(libName))
            return;

        _processedNames.Add(libName);

        try
        {
            // Calling 'LoadLibrary' instead of 'GetModuleHandle' so we can call 'GetModuleFileName'.
            using SafeModuleHandle hModule = NativeFunctions.LoadLibrary(libName);
            if (hModule is null || hModule.IsInvalid)
                throw new NativeException(Marshal.GetLastWin32Error());

            libInfo.IsLoaded = true;

            // Attempting to get the module path.
            StringBuilder pathBuffer = new(1024);
            int getPathResult = NativeFunctions.GetModuleFileName(hModule, pathBuffer, 1024);
            if (getPathResult != 0 && getPathResult != NativeConstants.ERROR_INSUFFICIENT_BUFFER && string.IsNullOrEmpty(libInfo.Path))
                libInfo.Path = pathBuffer.ToString();

            // TODO: Fix alignment issues when constructing from the module handle.
            FileStream stream = new(pathBuffer.ToString(), FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            PortableExecutable peHeaders;
            try
            {
                peHeaders = new(stream);
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                stream.Dispose();
            }
            if (peHeaders.OptionalHeaders is not null)
            {
                // Import tables.
                if (peHeaders.OptionalHeaders.ImportTableDirectory.Size != 0 &&
                    peHeaders.OptionalHeaders.ImportTableDirectory.VirtualAddress != 0)
                {
                    IntPtr dangerousModuleHandle = hModule.DangerousGetHandle();
                    IntPtr offset = (IntPtr)((ulong)dangerousModuleHandle + peHeaders.OptionalHeaders.ImportTableDirectory.VirtualAddress);
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
                                LibraryInfo info = new(impLibName)
                                {
                                    Parent = Path.GetFileName(libName)
                                };
                                GetLibraryModuleInfo(impLibName, ref info);
                                libInfo.ImportList.Add(info);
                            }

                        }

                        offset += bufferSize;

                    } while (nameRva != 0);
                }

                // Delay load table.
                if (peHeaders.OptionalHeaders.DelayImportTableDirectory.Size != 0 &&
                    peHeaders.OptionalHeaders.DelayImportTableDirectory.VirtualAddress != 0)
                {
                    IntPtr dangerousModuleHandle = hModule.DangerousGetHandle();
                    IntPtr offset = (IntPtr)((ulong)dangerousModuleHandle + peHeaders.OptionalHeaders.DelayImportTableDirectory.VirtualAddress);
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
                                LibraryInfo info = new(delayLoadLibName)
                                {
                                    Parent = Path.GetFileName(libName)
                                };
                                GetLibraryModuleInfo(delayLoadLibName, ref info);
                                libInfo.DelayLoadList.Add(info);
                            }

                        }

                        offset += bufferSize;

                    } while (nameRva != 0);
                }
            }
        }
        catch (Exception ex)
        {
            libInfo.LoadException = ex;
        }
    }
}