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
	public enum class DependencySource
	{
		None,
		PeTables,
		ReferencedAssemblies
	};

	public ref class DependencyEntry
	{
	public:
		property String^ Name { String^ get() { return _name; } }
		property DependencySource Source { DependencySource get() { return _source; } }

		DependencyEntry(String^ name, DependencySource source)
			: _name(name), _source(source) { }

	private:
		String^ _name;
		DependencySource _source;
	};

	public ref class ModuleBase
	{
	public:
		property String^ Name { String^ get() { return _name; } }
		property String^ Path { String^ get() { return _path; } }
		property String^ AssemblyFullName {
			String^ get() { return _ass_full_name; }
			void set(String^ value) { _ass_full_name = value; }
		}

		property bool Loaded { bool get() { return _loaded; } }
		property bool IsClr {
			bool get() {
				if (_wrapper != NULL)
					return _wrapper->IsClr;

				return false;
			}
		}
		property Exception^ LoaderException { Exception^ get() { return _loader_exception; } }

		property List<DependencyEntry^>^ Dependencies {
			List<DependencyEntry^>^ get() {
				auto output = gcnew List<DependencyEntry^>();
				
				if (_wrapper != NULL)
					for (WuString& dependency : _wrapper->Dependencies)
						output->Add(gcnew DependencyEntry(gcnew String(dependency.GetBuffer()), DependencySource::PeTables));

				return output;
			}
		}

		ModuleBase(String^ name, String^ path, String^ ass_full_name,
			bool loaded, Exception^ loader_exception, Core::PeHelper::PLS_IMAGE_BASIC_INFORMATION basic_info)
			: _name(name), _path(path), _ass_full_name(ass_full_name), _loaded(loaded), _loader_exception(loader_exception)
		{
			_wrapper = new Core::PeHelper::LS_IMAGE_BASIC_INFORMATION();
			_wrapper->DelayLoadTableRva = basic_info->DelayLoadTableRva;
			_wrapper->ImportTableRva = basic_info->ImportTableRva;
			_wrapper->IsClr = basic_info->IsClr;

			for (WuString& dep : basic_info->Dependencies)
				_wrapper->Dependencies.push_back(dep);
		}

		ModuleBase(String^ name, String^ path, String^ ass_full_name, bool loaded, bool is_clr, Exception^ loader_exception)
			: _name(name), _path(path), _ass_full_name(ass_full_name), _loaded(loaded), _loader_exception(loader_exception), _wrapper(NULL)
		{ }

		~ModuleBase() {
			if (_wrapper != NULL)
				delete _wrapper;
		}

	protected:
		!ModuleBase() {
			if (_wrapper != NULL)
				delete _wrapper;
		}

	private:
		String^ _name;
		String^ _path;
		String^ _ass_full_name;
		bool _loaded;
		Exception^ _loader_exception;
		Core::PeHelper::PLS_IMAGE_BASIC_INFORMATION _wrapper;
	};

	[Serializable()]
	public ref class NativeException : public Exception
	{
	public:
		property Int32 ErrorCode { Int32 get() { return _error_code; } }
		property String^ CompactTrace { String^ get() { return _compact_trace; } }

		NativeException(Int32 error_code)
			: Exception((gcnew String(Core::LSRESULT::GetErrorMessage(error_code).GetBuffer()))->Trim()),
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
		ModuleBase^ GetDependencyList(String^ file_name, DependencySource source);

	private:
		PeHelper* pe_helper;
	};

	static WuString GetNarrowFromManagedString(String^ str);
	static bool TryLoadAssembly(String^ name, String^ path, Assembly^& assembly, Exception^& loader_exception);
	
	static DateTime GetDateTimeFromTimeT(DWORD seconds) {
		double sec = static_cast<double>(seconds);
		return DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind::Utc).AddSeconds(sec);
	}

	static WWuString GetWideFromManagedString(String^ str)
	{
		pin_ptr<const wchar_t> pinned_str = PtrToStringChars(str);
		WWuString output((LPWSTR)pinned_str);
		pinned_str = nullptr;

		return output;
	}
}