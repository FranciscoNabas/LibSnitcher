#include "pch.h"

#include "Wrapper.h"

namespace LibSnitcher::Core
{
	ModuleBase^ Wrapper::GetDependencyList(String^ file_name, DependencySource source)
	{
		String^ name;
		String^ path;
		WWuString wrapped_path = GetWideFromManagedString(file_name);
		if (PathFileExists(wrapped_path.GetBuffer()))
		{
			name = Path::GetFileName(file_name);
			path = file_name;
		}
		else
		{
			name = file_name;
			path = nullptr;
		}

		HMODULE hmodule;
		ModuleBase^ output;
		Assembly^ assembly;
		if (source == DependencySource::None || source == DependencySource::PeTables)
		{
			// Attempting to get a module handle.
			DWORD last_error;
			bool fallback = false;

			/*
			* A word about LoadLibrary.
			* LoadLibrary loads the dll references, and calls the DllMain function.
			* A shitty module can cause an access violation exception.
			* 
			* LoadLibraryEx with DONT_RESOLVE_DLL_REFERENCES do not load the dll
			* references, and most importantly, don't call DllMain on loading, and
			* freeing.
			*/
			hmodule = LoadLibraryEx(wrapped_path.GetBuffer(), NULL, DONT_RESOLVE_DLL_REFERENCES);
			if (hmodule == NULL)
			{
				last_error = GetLastError();
				fallback = true;
			}

			// Trying to fallback to reflection.
			if (fallback)
			{
				Exception^ loader_exception;
				if (!TryLoadAssembly(name, path, assembly, loader_exception))
					return gcnew ModuleBase(name, path, String::Empty, false, false, gcnew NativeException(last_error));

				path = assembly->Location;
				name = Path::GetFileName(assembly->Location);
				wrapped_path = GetWideFromManagedString(path);
				
				wrapped_path = GetWideFromManagedString(path);
				hmodule = LoadLibraryEx(wrapped_path.GetBuffer(), NULL, DONT_RESOLVE_DLL_REFERENCES);
				if (hmodule == NULL)
					return gcnew ModuleBase(name, path, assembly->FullName, false, true, gcnew NativeException(GetLastError()));

				// Attempting to get basic PE information.
				auto basic_info = make_wushared<PeHelper::LS_IMAGE_BASIC_INFORMATION>();
				LSRESULT result = pe_helper->GetImageBasicInformation(hmodule, basic_info.get());
				if (result.Result != ERROR_SUCCESS)
					return gcnew ModuleBase(name, path, assembly->FullName, true, true, gcnew NativeException(result));

				output = gcnew ModuleBase(name, path, assembly->FullName, true, nullptr, basic_info.get());

				for each (AssemblyName ^ ref_ass in assembly->GetReferencedAssemblies())
					output->Dependencies->Add(gcnew DependencyEntry(ref_ass->FullName, DependencySource::ReferencedAssemblies));
			}
			else
			{
				// Getting module path;
				WCHAR buffer[MAX_PATH]{ 0 };
				GetModuleFileName(hmodule, buffer, MAX_PATH);
				if (buffer != NULL)
					path = gcnew String(buffer);

				// Attempting to get basic PE information.
				auto basic_info = make_wushared<PeHelper::LS_IMAGE_BASIC_INFORMATION>();
				LSRESULT result = pe_helper->GetImageBasicInformation(hmodule, basic_info.get());
				if (result.Result != ERROR_SUCCESS)
					return gcnew ModuleBase(name, path, assembly->FullName, true, false, gcnew NativeException(result));

				output = gcnew ModuleBase(name, path, String::Empty, true, nullptr, basic_info.get());

				// Attempting to get the managed referenced assemblies list.
				if (basic_info->IsClr)
				{
					// If it fails to load the main assembly we don't want to continue.
					Exception^ loader_exception;
					if (!TryLoadAssembly(name, path, assembly, loader_exception))
						return gcnew ModuleBase(name, path, String::Empty, true, true, loader_exception);

					output->AssemblyFullName = assembly->FullName;

					for each (AssemblyName^ ref_ass in assembly->GetReferencedAssemblies())
						output->Dependencies->Add(gcnew DependencyEntry(ref_ass->FullName, DependencySource::ReferencedAssemblies));
				}
			}
		}
		else
		{
			// The file name is a fully qualified assembly name,
			// so we use reflection first.
			
			Exception^ loader_exception;
			if (TryLoadAssembly(name, path, assembly, loader_exception))
			{
				path = assembly->Location;
				name = assembly->FullName;
				wrapped_path = GetWideFromManagedString(path);

				hmodule = LoadLibraryEx(wrapped_path.GetBuffer(), NULL, DONT_RESOLVE_DLL_REFERENCES);
				if (hmodule == NULL)
					return gcnew ModuleBase(name, path, assembly->FullName, true, true, loader_exception);

				auto basic_info = make_wushared<PeHelper::LS_IMAGE_BASIC_INFORMATION>();
				LSRESULT result = pe_helper->GetImageBasicInformation(hmodule, basic_info.get());
				if (result.Result != ERROR_SUCCESS)
					return gcnew ModuleBase(name, path, assembly->FullName, true, true, gcnew NativeException(result));

				output = gcnew ModuleBase(name, path, assembly->FullName, true, nullptr, basic_info.get());
				
				for each (AssemblyName ^ ref_ass in assembly->GetReferencedAssemblies())
					output->Dependencies->Add(gcnew DependencyEntry(ref_ass->FullName, DependencySource::ReferencedAssemblies));
			}
			else
			{
				hmodule = LoadLibraryEx(wrapped_path.GetBuffer(), NULL, DONT_RESOLVE_DLL_REFERENCES);
				if (hmodule == NULL)
					return gcnew ModuleBase(name, path, assembly->FullName, false, false, loader_exception);

				auto basic_info = make_wushared<PeHelper::LS_IMAGE_BASIC_INFORMATION>();
				LSRESULT result = pe_helper->GetImageBasicInformation(hmodule, basic_info.get());
				if (result.Result != ERROR_SUCCESS)
					return gcnew ModuleBase(name, path, assembly->FullName, true, false, gcnew NativeException(result));

				output = gcnew ModuleBase(name, path, String::Empty, true, nullptr, basic_info.get());

				if (basic_info->IsClr)
				{
					// Try one more time with the new path.
					WCHAR buffer[MAX_PATH]{ 0 };
					GetModuleFileName(hmodule, buffer, MAX_PATH);
					if (buffer != NULL)
						path = gcnew String(buffer);

					if (!TryLoadAssembly(name, path, assembly, loader_exception))
						return gcnew ModuleBase(name, path, assembly->FullName, true, true, loader_exception);
				}
			}
		}
		
		FreeLibrary(hmodule);

		return output;
	}

	static WuString GetNarrowFromManagedString(String^ str)
	{
		IntPtr pinned_str = Marshal::StringToHGlobalAnsi(str);
		WuString output((LPSTR)pinned_str.ToPointer());
		Marshal::FreeHGlobal(pinned_str);

		return output;
	}

	static bool TryLoadAssembly(String^ name, String^ path, Assembly^& assembly, Exception^& loader_exception) {

		try {
			assembly = Assembly::Load(name);
		}
		catch (Exception^) {
			try {
				assembly = Assembly::LoadFrom(name);
			}
			catch (Exception^) {
				if (!String::IsNullOrEmpty(path)) {
					try {
						assembly = Assembly::LoadFrom(path);
					}
					catch (Exception^ ex) {
						loader_exception = ex;
						return false;
					}
				}
				else {
					return false;
				}
			}
		}

		loader_exception = nullptr;
		return true;
	}
}