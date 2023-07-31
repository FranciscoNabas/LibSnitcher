#pragma once

#pragma unmanaged

#include "Common.h"
#include "PeHelper.h"

#pragma managed

#include <vcclr.h>

using namespace System;
using namespace System::IO;
using namespace System::Reflection;
using namespace System::Collections::Generic;
using namespace System::Runtime::Serialization;
using namespace System::Runtime::InteropServices;

namespace LibSnitcher
{
	public ref class DependencyEntry
	{
	public:
		property String^ Name {
			String^ get() { return _name; }
			void set(String^ value) { _name = value; }
		}
		property String^ Path {
			String^ get() { return _path; }
			void set(String^ value) { _path = value; }
		}
		property Boolean Loaded {
			Boolean get() { return _loaded; }
			void set(Boolean value) { _loaded = value; }
		}
		property Boolean IsClr {
			Boolean get() { return _is_clr; }
			void set(Boolean value) { _is_clr = value; }
		}
		property array<String^>^ Dependencies {
			array<String^>^ get() { return _dependencies; }
			void set(array<String^>^ value) { _dependencies = value; }
		}

		DependencyEntry() { }
		DependencyEntry(String^ name, Boolean loaded, Boolean is_clr)
			: _name(name), _loaded(loaded), _is_clr(is_clr) { }

		DependencyEntry(String^ name, String^ path, Boolean is_clr, Boolean loaded)
			: _name(name), _path(path), _loaded(loaded), _is_clr(is_clr) { }

	private:
		String^ _name;
		String^ _path;
		Boolean _loaded;
		Boolean _is_clr;
		array<String^>^ _dependencies;
	};

	[Serializable()]
	public ref class NativeException : public Exception
	{
	public:
		property Int32 ErrorCode { Int32 get() { return _error_code; } }
		property String^ CompactTrace { String^ get() { return _compact_trace; } }

		NativeException(Int32 error_code)
			: Exception(gcnew String(Core::LSRESULT::GetErrorMessage(error_code).GetBuffer())),
			  _error_code(error_code) { }

		NativeException(Int32 error_code, String^ message)
			: Exception(message), _error_code(error_code) { }

		NativeException(Int32 error_code, String^ message, Exception^ inner_exception)
			: Exception(message, inner_exception), _error_code(error_code) { }

		NativeException(Core::LSRESULT& ls_error)
			: Exception(gcnew String(ls_error.Message.GetBuffer())),
			  _error_code(ls_error.Result),
			  _compact_trace(gcnew String(ls_error.CompactTrace.GetBuffer())) { }

	protected:
		NativeException()
			: Exception() { }

		NativeException(SerializationInfo^ info, StreamingContext context)
			: Exception(info, context) { }

	private:
		Int32 _error_code;
		String^ _compact_trace;
	};
}

namespace LibSnitcher::Core {
	public ref class Wrapper
	{
	public:
		void TestLoadImageFile(String^ file_name);
		DependencyEntry^ GetDependencyList(String^ file_path);

	private:
		PeHelper* pe_helper;
	};

	static WuString GetNarrowFromManagedString(String^ str);
	static WWuString GetWideFromManagedString(String^ str)
	{
		pin_ptr<const wchar_t> pinned_str = PtrToStringChars(str);
		WWuString output((LPWSTR)pinned_str);
		pinned_str = nullptr;

		return output;
	}

	static DateTime GetDateTimeFromTimeT(DWORD seconds) {
		double sec = static_cast<double>(seconds);
		return DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind::Utc).AddSeconds(sec);
	}
}