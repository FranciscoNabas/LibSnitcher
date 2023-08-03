#pragma once

#pragma unmanaged

#include "Common.h"
#include "Expressions.h"

namespace LibSnitcher::Core
{
	extern "C" public class __declspec(dllexport) PeHelper
	{
	public:
		typedef struct _LS_PORTABLE_EXECUTABLE
		{
			WORD Magic;
			DWORD MetadataStartOffset;
			DWORD MetadataSize;
			bool IsCoffOnly;
			bool IsConsoleApplication;
			bool IsDll;
			bool IsExe;
			DWORD CoffHeaderOffset;
			DWORD OptionalHeadersOffset;
			DWORD CorHeaderOffset;
			union
			{
				PIMAGE_NT_HEADERS32 NtHeaders32;
				PIMAGE_NT_HEADERS64 NtHeaders64;
				PIMAGE_FILE_HEADER CoffHeader;
			};
			PIMAGE_COR20_HEADER CorHeader;
			wuvector<IMAGE_SECTION_HEADER>* SectionHeaders;

			_LS_PORTABLE_EXECUTABLE() 
				: Magic(0), MetadataStartOffset(0), MetadataSize(0), IsCoffOnly(false), IsConsoleApplication(false),
					IsDll(0), IsExe(0), CoffHeaderOffset(0), OptionalHeadersOffset(0), CorHeaderOffset(0), NtHeaders64(NULL), CorHeader(0)
			{
				SectionHeaders = new wuvector<IMAGE_SECTION_HEADER>();
			}
			~_LS_PORTABLE_EXECUTABLE() {
				delete SectionHeaders;
				delete NtHeaders32;
			}

		} LS_PORTABLE_EXECUTABLE, * PLS_PORTABLE_EXECUTABLE;

		typedef struct _LS_IMAGE_BASIC_INFORMATION
		{
			bool IsClr;
			DWORD ImportTableRva;
			DWORD DelayLoadTableRva;
			wuvector<WuString>* Dependencies;

			_LS_IMAGE_BASIC_INFORMATION()
				: IsClr(false), ImportTableRva(0), DelayLoadTableRva(0) {
				Dependencies = new wuvector<WuString>();
			}

			~_LS_IMAGE_BASIC_INFORMATION() {
				delete Dependencies;
			}

		} LS_IMAGE_BASIC_INFORMATION, *PLS_IMAGE_BASIC_INFORMATION;

		// This function attempts to get all PE header information from the image.
		// It is relatively expensive, and should be called only when full image
		// information is required.
		const LSRESULT GetPeHeaders(WWuString image_path, PLS_PORTABLE_EXECUTABLE pe_headers);

		const LSRESULT GetImageBasicInformation(HMODULE hmodule, PLS_IMAGE_BASIC_INFORMATION image_info) noexcept;

		// This function attempts to list the module names in the image's import, and delay load tables.
		void GetModuleDependencyTables(HMODULE hmodule, PLS_IMAGE_BASIC_INFORMATION img_info) noexcept;
	};

	static const LSRESULT GetDirectoryOffset(IMAGE_DATA_DIRECTORY directory, PIMAGE_SECTION_HEADER sections, DWORD section_count, DWORD& offset, bool is_loaded);
	static bool CheckImageFormat(HMODULE hmodule, bool& coff_only, DWORD& pe_sig_ra);
	static bool CheckImageFormat(LPVOID mapped_view, bool& coff_only, DWORD& pe_sig_ra);
}