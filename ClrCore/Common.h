#pragma once

#include "pch.h"

namespace LibSnitcher::Core
{
	typedef struct _LSRESULT
	{
		LONG Result;
		WWuString Message;

		// 'FileName:LineNumber'
		WWuString CompactTrace;

		_LSRESULT() {
			Result = ERROR_SUCCESS;
		}

		_LSRESULT(LONG error_code, LPWSTR file_path, DWORD line_number, bool is_nt = false) {
			Result = error_code;
			Message = GetErrorMessage(error_code, is_nt);
			CompactTrace = GetCompactTrace(file_path, line_number);
		}

		_LSRESULT(LONG error_code, LPWSTR message, LPWSTR file_path, DWORD line_number) {
			Result = error_code;
			Message = message;
			CompactTrace = GetCompactTrace(file_path, line_number);
		}

		~_LSRESULT() { }

		_NODISCARD static WWuString GetErrorMessage(long error_code, bool is_nt = false) {
			if (is_nt) {
				HMODULE module = GetModuleHandle(L"ntdll.dll");
				if (module == NULL)
					return WWuString();

				LPWSTR buffer = NULL;
				DWORD flags = FORMAT_MESSAGE_ALLOCATE_BUFFER | FORMAT_MESSAGE_FROM_HMODULE | FORMAT_MESSAGE_IGNORE_INSERTS;
				DWORD result = FormatMessage(flags, module, error_code, NULL, (LPWSTR)&buffer, 0, NULL);
				
				if (result == 0)
					return WWuString();

				WWuString output(buffer);
				LocalFree(buffer);

				return output;
			}
			else {
				LPWSTR buffer = NULL;
				DWORD flags = FORMAT_MESSAGE_ALLOCATE_BUFFER | FORMAT_MESSAGE_FROM_SYSTEM | FORMAT_MESSAGE_IGNORE_INSERTS;
				DWORD result = FormatMessage(flags, NULL, error_code, NULL, (LPWSTR)&buffer, 0, NULL);

				if (result == 0)
					return WWuString();

				WWuString output(buffer);
				LocalFree(buffer);

				return output;
			}
		}

		_NODISCARD static WWuString GetCompactTrace(LPWSTR file_path, DWORD line_number) {
			WWuString file_name(file_path);
			PathStripPath(file_name.GetBuffer());
			return WWuString::Format(L"%ws:%d", file_name.GetBuffer(), line_number);
		}

	} LSRESULT, *PLSRESULT;
}