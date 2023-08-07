#include "pch.h"

#include "PeHelper.h"

namespace LibSnitcher::Core
{
	const LSRESULT PeHelper::GetPeHeaders(WWuString image_path, PLS_PORTABLE_EXECUTABLE pe_headers)
	{
		LARGE_INTEGER file_size;

		if (!PathFileExists(image_path.GetBuffer()))
			return LSRESULT(GetLastError(), __FILEW__, __LINE__);

		WWuString image_name(image_path);
		PathStripPath(image_name.GetBuffer());

		// Opening the file, and getting its size.
		HANDLE h_file = CreateFile(image_path.GetBuffer(), GENERIC_READ, FILE_SHARE_READ | FILE_SHARE_WRITE, NULL, OPEN_EXISTING, NULL, NULL);
		if (h_file == NULL)
			return LSRESULT(GetLastError(), __FILEW__, __LINE__);

		if (!GetFileSizeEx(h_file, &file_size))
		{
			CloseHandle(h_file);
			return LSRESULT(GetLastError(), __FILEW__, __LINE__);
		}

		// Creating file mapping, and view.
		WWuString map_name = WWuString::Format(L"LibSnitcher-%ws", image_name.GetBuffer());
		HANDLE h_map = CreateFileMapping(h_file, NULL, PAGE_READONLY, 0, 0, map_name.GetBuffer());
		if (h_map == NULL)
		{
			CloseHandle(h_file);
			return LSRESULT(GetLastError(), __FILEW__, __LINE__);
		}

		PVOID map_view = MapViewOfFile(h_map, FILE_MAP_READ, 0, 0, 0);
		if (map_view == NULL)
		{
			CloseHandle(h_map);
			CloseHandle(h_file);
			return LSRESULT(GetLastError(), __FILEW__, __LINE__);
		}

		// Testing if it's a valid image.
		if (file_size.QuadPart < 20)
			return LSRESULT(ERROR_BAD_FORMAT, __FILEW__, __LINE__);

		bool coff_only;
		DWORD pe_sig_ra;
		if (!CheckImageFormat(map_view, coff_only, pe_sig_ra))
		{
			UnmapViewOfFile(map_view);
			CloseHandle(h_map);
			CloseHandle(h_file);
			return LSRESULT(ERROR_BAD_FORMAT, __FILEW__, __LINE__);
		}

		if (coff_only)
		{
			pe_headers->IsCoffOnly = true;
			pe_headers->IsConsoleApplication = false;
			pe_headers->IsDll = false;
			pe_headers->IsExe = false;
			pe_headers->CoffHeaderOffset = 0;
			
			// Copying the COFF header;
			RtlCopyMemory(&pe_headers->CoffHeader, map_view, sizeof(IMAGE_FILE_HEADER));

			if (file_size.QuadPart < (__int64)((pe_headers->CoffHeader.NumberOfSections * sizeof(IMAGE_SECTION_HEADER)) + 20))
				return LSRESULT(ERROR_BAD_FORMAT, __FILEW__, __LINE__);

			// Copying the section headers.
			LPVOID section_offset = (LPVOID)((char*)map_view + __LINE__);
			for (size_t i = 0; i < pe_headers->CoffHeader.NumberOfSections; i++)
			{
				IMAGE_SECTION_HEADER sec_header = { 0 };
				memcpy(&sec_header, section_offset, sizeof(IMAGE_SECTION_HEADER));

				pe_headers->SectionHeaders.push_back(sec_header);

				section_offset = (LPVOID)((char*)section_offset + sizeof(IMAGE_SECTION_HEADER));
			}

			// Calculating metadata location (if any).
			bool cor_found = false;
			for (IMAGE_SECTION_HEADER& header : pe_headers->SectionHeaders)
			{
				LPSTR section_name = reinterpret_cast<char*>(header.Name);
				if (strcmp(section_name, ".cormeta") == 0)
				{
					cor_found = true;
					pe_headers->MetadataSize = header.SizeOfRawData;
					pe_headers->MetadataStartOffset = header.PointerToRawData;
				}
			}
			if (!cor_found)
			{
				pe_headers->CorHeaderOffset = -1;
				pe_headers->MetadataSize = 0;
				pe_headers->MetadataStartOffset = 0;
			}
		}
		else
		{
			UnmapViewOfFile(map_view);
			CloseHandle(h_map);
			CloseHandle(h_file);

			pe_headers->IsCoffOnly = false;
			
			PathStripPath(image_name.GetBuffer());
			DWORD dw_result = PathCchRemoveFileSpec(image_path.GetBuffer(), image_path.Length());
			if (dw_result != S_OK)
				return LSRESULT(dw_result, __FILEW__, __LINE__);

			WuString narrow_name = WWuStringToNarrow(image_name);
			WuString narrow_path = WWuStringToNarrow(image_path);

			PLOADED_IMAGE loaded_image = ImageLoad(narrow_name.GetBuffer(), narrow_path.GetBuffer());
			if (loaded_image == NULL)
				return LSRESULT(GetLastError(), __FILEW__, __LINE__);
			
			// Calculating COFF header offset.
			pe_headers->CoffHeaderOffset = pe_sig_ra + 4;

			pe_headers->OptionalHeaderOffset = pe_headers->CoffHeaderOffset + 20;
			pe_headers->IsDll = (loaded_image->FileHeader->FileHeader.Characteristics & IMAGE_FILE_DLL) != 0;
			pe_headers->IsExe = (loaded_image->FileHeader->FileHeader.Characteristics & IMAGE_FILE_DLL) == 0;
			pe_headers->IsConsoleApplication = loaded_image->FileHeader->OptionalHeader.Subsystem == IMAGE_SUBSYSTEM_WINDOWS_CUI;

			// Copying NT headers.
			pe_headers->Magic = loaded_image->FileHeader->OptionalHeader.Magic;
			if (pe_headers->Magic == IMAGE_NT_OPTIONAL_HDR32_MAGIC)
				RtlCopyMemory(&pe_headers->NtHeaders32, loaded_image->FileHeader, sizeof(IMAGE_NT_HEADERS32));
			else
				RtlCopyMemory(&pe_headers->NtHeaders64, loaded_image->FileHeader, sizeof(IMAGE_NT_HEADERS64));
			
			// Copying section headers.
			PIMAGE_SECTION_HEADER section_offset = loaded_image->Sections;
			for (size_t i = 0; i < loaded_image->NumberOfSections; i++)
			{
				IMAGE_SECTION_HEADER sec_header = { 0 };
				RtlCopyMemory(&sec_header, section_offset, sizeof(IMAGE_SECTION_HEADER));
				pe_headers->SectionHeaders.push_back(sec_header);

				section_offset = (PIMAGE_SECTION_HEADER)((char*)section_offset + sizeof(IMAGE_SECTION_HEADER));
			}

			// Attempting to get COR header, and offsets;
			DWORD cor_rel_offset = 0;
			pe_headers->MetadataSize = 0;
			pe_headers->MetadataStartOffset = 0;
			LSRESULT result = GetDirectoryOffset(
				loaded_image->FileHeader->OptionalHeader.DataDirectory[IMAGE_DIRECTORY_ENTRY_COM_DESCRIPTOR],
				loaded_image->Sections,
				(DWORD)loaded_image->NumberOfSections,
				cor_rel_offset,
				false
			);
			if (cor_rel_offset > 0)
			{
				pe_headers->CorHeaderOffset = cor_rel_offset;

				LPVOID cor_offset = (LPVOID)((char*)loaded_image->MappedAddress + cor_rel_offset);
				RtlCopyMemory(&pe_headers->CorHeader, cor_offset, sizeof(IMAGE_COR20_HEADER));

				DWORD meta_rel_offset = 0;
				result = GetDirectoryOffset(
					pe_headers->CorHeader.MetaData,
					loaded_image->Sections,
					(DWORD)loaded_image->NumberOfSections,
					meta_rel_offset,
					false
				);

				if (meta_rel_offset == 0)
				{
					ImageUnload(loaded_image);
					return LSRESULT(ERROR_BAD_FORMAT, L"COR header missing data directory.", __FILEW__, __LINE__);
				}

				DWORD meta_size = pe_headers->CorHeader.MetaData.Size;
				if (meta_rel_offset < 0 ||
					meta_rel_offset >= loaded_image->SizeOfImage ||
					meta_size <= 0 ||
					meta_rel_offset > loaded_image->SizeOfImage - meta_size)
				{
					ImageUnload(loaded_image);
					return LSRESULT(ERROR_BAD_FORMAT, L"Invalid COR metadata section span.", __FILEW__, __LINE__);
				}

				pe_headers->MetadataSize = meta_size;
				pe_headers->MetadataStartOffset = meta_rel_offset;
			}
			else
				pe_headers->CorHeaderOffset = -1;

			ImageUnload(loaded_image);
		}

		return LSRESULT();
	}

	const LSRESULT PeHelper::GetImageBasicInformation(HMODULE hmodule, PLS_IMAGE_BASIC_INFORMATION image_info) noexcept
	{
		// Checking if the file is a valid image.
		bool coff_only = false;
		DWORD pe_sig_ra = 0;
		if (!CheckImageFormat(hmodule, coff_only, pe_sig_ra))
			return LSRESULT(ERROR_BAD_FORMAT, __FILEW__, __LINE__);
		
		if (coff_only)
			return LSRESULT(ERROR_BAD_FORMAT, L"File is not a valid image.", __FILEW__, __LINE__);

		// Getting the size of the optional header, and magic number.
		WORD opt_header_size = *static_cast<WORD*>((LPVOID)((char*)hmodule + pe_sig_ra + 20));
		LPVOID opt_header_offset = (LPVOID)((char*)hmodule + pe_sig_ra + 24);
		WORD magic = *static_cast<WORD*>(opt_header_offset);

		// Getting size for the 'fixed' part of the optional header.
		WORD fixed_opt_header_size = 112;
		if (magic == IMAGE_NT_OPTIONAL_HDR32_MAGIC)
			fixed_opt_header_size = 96;
		
		// Number of data directories.
		DWORD nr_rva_sizes = *static_cast<DWORD*>((LPVOID)((char*)opt_header_offset + fixed_opt_header_size - 4));

		// Checking if there's a data directory for the COR header.
		PIMAGE_DATA_DIRECTORY data_dir = static_cast<PIMAGE_DATA_DIRECTORY>((LPVOID)((char*)opt_header_offset + fixed_opt_header_size));
		if (nr_rva_sizes >= 15)
		{
			if (opt_header_size < (sizeof(IMAGE_DATA_DIRECTORY) * 15) + fixed_opt_header_size)
				return LSRESULT(ERROR_BAD_FORMAT, L"Optional header size inconsistent with number of data directories.", __FILEW__, __LINE__);

			if (data_dir[IMAGE_DIRECTORY_ENTRY_COM_DESCRIPTOR].Size != 0 &&
				data_dir[IMAGE_DIRECTORY_ENTRY_COM_DESCRIPTOR].VirtualAddress != 0
				)
				image_info->IsClr = true;
		}

		// Attempting to get import and delay load table information.
		if (nr_rva_sizes >= 1)
			image_info->ImportTableRva = data_dir[IMAGE_DIRECTORY_ENTRY_IMPORT].VirtualAddress;

		if (nr_rva_sizes >= 13)
			image_info->DelayLoadTableRva = data_dir[IMAGE_DIRECTORY_ENTRY_DELAY_IMPORT].VirtualAddress;

		GetModuleDependencyTables(hmodule, image_info);

		return LSRESULT();
	}

	void PeHelper::GetModuleDependencyTables(HMODULE hmodule, PeHelper::PLS_IMAGE_BASIC_INFORMATION img_info) noexcept
	{
		if (img_info->ImportTableRva > 0)
		{
			PIMAGE_IMPORT_DESCRIPTOR imptab_opffset = (PIMAGE_IMPORT_DESCRIPTOR)((char*)hmodule + img_info->ImportTableRva);
			while (imptab_opffset->Name != NULL)
			{
				LPSTR lib_name = (LPSTR)((char*)hmodule + imptab_opffset->Name);
				if (lib_name != NULL)
					img_info->Dependencies.push_back(lib_name);

				imptab_opffset++;
			}
		}

		if (img_info->DelayLoadTableRva > 0)
		{
			PIMAGE_DELAYLOAD_DESCRIPTOR delload_opffset = (PIMAGE_DELAYLOAD_DESCRIPTOR)((char*)hmodule + img_info->DelayLoadTableRva);
			while (delload_opffset->DllNameRVA != 0)
			{
				LPSTR lib_name = (LPSTR)((char*)hmodule + delload_opffset->DllNameRVA);
				if (lib_name != NULL)
					img_info->Dependencies.push_back(lib_name);

				delload_opffset++;
			}
		}
	}

	const LSRESULT GetDirectoryOffset(IMAGE_DATA_DIRECTORY directory, PIMAGE_SECTION_HEADER sections, DWORD section_count, DWORD& offset, bool is_loaded)
	{
		if (directory.VirtualAddress == 0)
		{
			offset = 0;
			return LSRESULT();
		}

		for (DWORD i = 0; i < section_count; i++)
		{
			if (sections[i].VirtualAddress <= directory.VirtualAddress && directory.VirtualAddress < sections[i].VirtualAddress + sections[i].Misc.VirtualSize)
			{
				DWORD diff_num = directory.VirtualAddress - sections[i].VirtualAddress;
				if (directory.Size > sections[i].Misc.VirtualSize + diff_num)
					return LSRESULT(ERROR_BAD_FORMAT, L"COR section too small.", __FILEW__, __LINE__);

				offset = is_loaded ? directory.VirtualAddress : sections[i].PointerToRawData + diff_num;
				break;
			}
		}

		return LSRESULT();
	}

	bool CheckImageFormat(HMODULE hmodule, bool& coff_only, DWORD& pe_sig_ra)
	{
		WORD dos_sig = *static_cast<WORD*>((LPVOID)hmodule);
		WORD sig_off = *static_cast<WORD*>((LPVOID)((char*)hmodule + 2));

		if (dos_sig != 23117)
		{
			if (dos_sig == 0 && sig_off == 65535)
				return false;

			coff_only = true;
		}
		else
			coff_only = false;

		if (!coff_only)
		{
			pe_sig_ra = *static_cast<DWORD*>((LPVOID)((char*)hmodule + 60));
			DWORD pe_sig = *static_cast<DWORD*>((LPVOID)((char*)hmodule + pe_sig_ra));
			if (pe_sig != 17744)
				return false;
		}

		return true;
	}

	bool CheckImageFormat(LPVOID mapped_view, bool& coff_only, DWORD& pe_sig_ra)
	{
		WORD dos_sig = *static_cast<WORD*>(mapped_view);
		WORD sig_off = *static_cast<WORD*>((LPVOID)((char*)mapped_view + 2));

		if (dos_sig != 23117)
		{
			if (dos_sig == 0 && sig_off == 65535)
				return false;

			coff_only = true;
		}
		else
			coff_only = false;

		if (!coff_only)
		{
			pe_sig_ra = *static_cast<DWORD*>((LPVOID)((char*)mapped_view + 60));
			DWORD pe_sig = *static_cast<DWORD*>((LPVOID)((char*)mapped_view + pe_sig_ra));
			if (pe_sig != 17744)
				return false;
		}

		return true;
	}
}