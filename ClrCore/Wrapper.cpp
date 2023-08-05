#include "pch.h"

#include "Wrapper.h"

namespace LibSnitcher::Core
{
	LibInfo^ Wrapper::GetLibBasicInfo(String^ file_name, DependencySource source)
	{
		LibInfo^ output;
		Assembly^ assembly;
		String^ name;
		String^ path;
		HMODULE hmodule = NULL;
		bool fallback = false;
		
		pin_ptr<const wchar_t> wrapped_path = PtrToStringChars(file_name);
		if (PathFileExists(wrapped_path))
		{
			name = Path::GetFileName(file_name);
			path = file_name;
		}
		else
		{
			name = file_name;
			path = nullptr;
		}
		
		if (source == DependencySource::None || source == DependencySource::PeTables)
		{
			hmodule = LoadLibrary(wrapped_path);
			if (hmodule == NULL)
				fallback = true;

			if (fallback)
			{
				Exception^ loader_exception;
				if (!TryLoadAssembly(name, path, assembly, loader_exception))
				{
					output = gcnew LibInfo(name, path, false, false);
					output->LoaderError = loader_exception;
					return output;
				}

				path = assembly->Location;
				name = assembly->FullName;

				output = gcnew LibInfo(name, path, true, true);
			}
			else
			{
				WCHAR buffer[MAX_PATH]{ 0 };
				GetModuleFileName(hmodule, buffer, MAX_PATH);
				if (buffer != NULL)
					path = gcnew String(buffer);

				output = gcnew LibInfo(name, path, false, true);
			}
		}
		else
		{
			Exception^ loader_exception;
			output = gcnew LibInfo(name, path, false, false);
			if (!TryLoadAssembly(name, path, assembly, loader_exception))
				fallback = true;

			if (fallback)
			{
				hmodule = LoadLibrary(wrapped_path);
				if (hmodule != NULL)
				{
					WCHAR buffer[MAX_PATH]{ 0 };
					GetModuleFileName(hmodule, buffer, MAX_PATH);
					if (buffer != NULL)
						output->Path = gcnew String(buffer);

					output->Loaded = true;
				}
			}
			else
			{
				output->Loaded = true;
				output->IsClr = true;
				output->Path = assembly->Location;
				output->Name = assembly->FullName;
			}
		}

		if (hmodule != NULL)
			FreeLibrary(hmodule);

		return output;
	}

	LibInfo^ Wrapper::GetDependencyList(String^ file_name, DependencySource source)
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
		Assembly^ assembly;
		LibInfo^ output;
		if (source == DependencySource::None || source == DependencySource::PeTables)
		{
			// Attempting to get a module handle.
			DWORD last_error;
			bool fallback = false;
			hmodule = LoadLibrary(wrapped_path.GetBuffer());
			if (hmodule == NULL)
			{
				last_error = GetLastError();
				fallback = true;
			}

			// Trying to fallback to reflection.
			if (fallback)
			{
				Assembly^ assembly;
				Exception^ loader_exception;
				if (!TryLoadAssembly(name, path, assembly, loader_exception))
				{
					output = gcnew LibInfo(name, path, false, false);
					output->LoaderError = gcnew NativeException(last_error);
					return output;
				}

				path = assembly->Location;
				name = assembly->FullName;
				
				List<DependencyEntry^>^ dep_entries = gcnew List<DependencyEntry^>();
				pin_ptr<const wchar_t> full_name = PtrToStringChars(path);
				hmodule = LoadLibrary((LPWSTR)full_name);
				if (hmodule == NULL)
				{
					output = gcnew LibInfo(name, path, true, true);
					output->LoaderError = gcnew NativeException(GetLastError());
					return output;
				}

				// Attempting to get basic PE information.
				auto basic_info = make_wuunique<PeHelper::LS_IMAGE_BASIC_INFORMATION>();
				LSRESULT result = pe_helper->GetImageBasicInformation(hmodule, basic_info.get());
				if (result.Result != ERROR_SUCCESS)
				{
					FreeLibrary(hmodule);
					output = gcnew LibInfo(name, path, true, true);
					output->LoaderError = gcnew NativeException(result);
					return output;
				}

				output = gcnew LibInfo(name, path, true, true);
				for (WuString& dep : *basic_info->Dependencies)
				{
					if (dep != GetNarrowFromManagedString(name))
						dep_entries->Add(gcnew DependencyEntry(gcnew String(dep.GetBuffer()), DependencySource::PeTables));
				}

				for each (AssemblyName ^ ref_ass in assembly->GetReferencedAssemblies())
					dep_entries->Add(gcnew DependencyEntry(ref_ass->FullName, DependencySource::ReferencedAssemblies));

				output->Dependencies = dep_entries->ToArray();
			}
			else
			{
				// Getting module path;
				WCHAR buffer[MAX_PATH]{ 0 };
				GetModuleFileName(hmodule, buffer, MAX_PATH);
				if (buffer != NULL)
					path = gcnew String(buffer);

				// Attempting to get basic PE information.
				auto basic_info = make_wuunique<PeHelper::LS_IMAGE_BASIC_INFORMATION>();
				LSRESULT result = pe_helper->GetImageBasicInformation(hmodule, basic_info.get());
				if (result.Result != ERROR_SUCCESS)
				{
					FreeLibrary(hmodule);
					output = gcnew LibInfo(name, path, false, false);
					output->LoaderError = gcnew NativeException(result);
					return output;
				}

				output = gcnew LibInfo(name, path, basic_info->IsClr, true);
				List<DependencyEntry^>^ dep_entries = gcnew List<DependencyEntry^>();
				for (WuString& dep : *basic_info->Dependencies)
				{
					if (dep != GetNarrowFromManagedString(name))
						dep_entries->Add(gcnew DependencyEntry(gcnew String(dep.GetBuffer()), DependencySource::PeTables));
				}

				// Attempting to get the managed referenced assemblies list.
				if (basic_info->IsClr)
				{
					// If it fails to load the main assembly we don't want to continue.
					Exception^ loader_exception;
					if (!TryLoadAssembly(name, path, assembly, loader_exception))
					{
						FreeLibrary(hmodule);
						output->LoaderError = loader_exception;
						return output;
					}

					output->Path = assembly->Location;
					output->Name = assembly->FullName;

					for each (AssemblyName^ ref_ass in assembly->GetReferencedAssemblies())
						dep_entries->Add(gcnew DependencyEntry(ref_ass->FullName, DependencySource::ReferencedAssemblies));
				}

				output->Dependencies = dep_entries->ToArray();
			}
		}
		else
		{
			// The file name can be a fully qualified assembly name,
			// so we use reflection first.
			bool failed_to_load = false;
			Exception^ loader_exception;
			if (!TryLoadAssembly(name, path, assembly, loader_exception))
				failed_to_load = true;

			List<DependencyEntry^>^ dep_entries = gcnew List<DependencyEntry^>();
			if (!failed_to_load)
			{
				path = assembly->Location;
				name = assembly->FullName;

				output = gcnew LibInfo(name, path, true, true);

				for each (AssemblyName ^ ref_ass in assembly->GetReferencedAssemblies())
					dep_entries->Add(gcnew DependencyEntry(ref_ass->FullName, DependencySource::ReferencedAssemblies));

				// Attempting to get PE information.
				pin_ptr<const wchar_t> full_name = PtrToStringChars(assembly->Location);
				hmodule = LoadLibrary((LPWSTR)full_name);
				if (hmodule == NULL)
				{
					output->LoaderError = gcnew NativeException(GetLastError());
					return output;
				}
			}
			else
			{
				pin_ptr<const wchar_t> wrapped_name = PtrToStringChars(name);
				hmodule = LoadLibrary((LPWSTR)wrapped_name);
				if (hmodule == NULL)
				{
					output = gcnew LibInfo(name, path, false, false);
					output->LoaderError = loader_exception;
					return output;
				}

				output = gcnew LibInfo(name, path, false, true);
			}

			auto basic_info = make_wuunique<PeHelper::LS_IMAGE_BASIC_INFORMATION>();
			LSRESULT result = pe_helper->GetImageBasicInformation(hmodule, basic_info.get());
			if (result.Result != ERROR_SUCCESS)
			{
				FreeLibrary(hmodule);
				output->LoaderError = gcnew NativeException(result);
				return output;
			}

			output->IsClr = basic_info->IsClr;
			if (basic_info->IsClr && failed_to_load)
			{
				// Try one more time with the new path.
				WCHAR buffer[MAX_PATH]{ 0 };
				GetModuleFileName(hmodule, buffer, MAX_PATH);
				if (buffer != NULL)
					path = gcnew String(buffer);

				if (!TryLoadAssembly(name, path, assembly, loader_exception))
				{
					FreeLibrary(hmodule);
					output->LoaderError = loader_exception;
					return output;
				}
			}

			for (WuString& dep : *basic_info->Dependencies)
			{
				if (dep != GetNarrowFromManagedString(name))
					dep_entries->Add(gcnew DependencyEntry(gcnew String(dep.GetBuffer()), DependencySource::PeTables));
			}

			output->Dependencies = dep_entries->ToArray();
		}
		
		FreeLibrary(hmodule);

		return output;
	}

	WuString GetNarrowFromManagedString(String^ str)
	{
		IntPtr pinned_str = Marshal::StringToHGlobalAnsi(str);
		WuString output((LPSTR)pinned_str.ToPointer());
		Marshal::FreeHGlobal(pinned_str);

		return output;
	}
}