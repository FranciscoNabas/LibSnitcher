#include "pch.h"

#include "Wrapper.h"

namespace LibSnitcher::Core
{
	void Wrapper::TestLoadImageFile(String^ file_path)
	{
		
	}

	DependencyEntry^ Wrapper::GetDependencyList(String^ file_path)
	{
		bool is_free = false;
		WWuString wrapped_path = GetWideFromManagedString(file_path);
		HMODULE hmodule = GetModuleHandle(wrapped_path.GetBuffer());
		if (hmodule == NULL)
		{
			hmodule = LoadLibrary(wrapped_path.GetBuffer());
			if (hmodule == NULL)
				throw gcnew NativeException(GetLastError());

			is_free = true;
		}

		auto basic_info = make_wuunique<PeHelper::LS_IMAGE_BASIC_INFORMATION>();
		LSRESULT result = pe_helper->GetImageBasicInformation(hmodule, basic_info.get());
		if (result.Result != ERROR_SUCCESS)
			throw gcnew NativeException(result);

		DependencyEntry^ output = gcnew DependencyEntry(Path::GetFileName(file_path), file_path, true, basic_info->IsClr);
		output->Dependencies = gcnew array<String^>(static_cast<int>(basic_info->Dependencies->size()));
		int index = 0;
		for (WuString& dep : *basic_info->Dependencies)
			output->Dependencies[index++] = gcnew String(dep.GetBuffer());

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