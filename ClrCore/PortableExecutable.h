#pragma once

#pragma managed

#include "Wrapper.h"

using namespace System;

namespace LibSnitcher
{
    public enum class MachineType : unsigned short {
        Unknown = 0x0, // The content of this field is assumed to be applicable to any machine type.
        Alpha = 0x184, // Alpha AXP, 32-bit address space.
        Alpha64 = 0x284, // Alpha 64, 64-bit address space.
        Am33 = 0x1d3, // Matsushita AM33.
        Amd64 = 0x8664, // x64.
        Arm = 0x1c0, // ARM little endian.
        Arm64 = 0xaa64, // ARM64 little endian.
        ArmNt = 0x1c4, // ARM Thumb-2 little endian.
        Axp64 = 0x284, // AXP 64 (Same as Alpha 64).
        Ebc = 0xebc, // EFI byte code.
        I386 = 0x14c, // Intel 386 or later processors and compatible processors.
        Ia64 = 0x200, // Intel Itanium processor family.
        LoongArch32 = 0x6232, // LoongArch 32-bit processor family.
        LoongArch64 = 0x6264, // LoongArch 64-bit processor family.
        M32R = 0x9041, // Mitsubishi M32R little endian.
        Mips16 = 0x266, // MIPS16.
        MipsFpu = 0x366, // MIPS with FPU.
        MipsFpu16 = 0x466, // MIPS16 with FPU.
        PowerPc = 0x1f0, // Power PC little endian.
        PowerPcFp = 0x1f1, // Power PC with floating point support.
        R4000 = 0x166, // MIPS little endian.
        RiscV32 = 0x5032, // RISC-V 32-bit address space.
        RiscV64 = 0x5064, // RISC-V 64-bit address space.
        RiscV128 = 0x5128, // RISC-V 128-bit address space.
        Sh3 = 0x1a2, // Hitachi SH3.
        Sh3Dsp = 0x1a3, // Hitachi SH3 DSP.
        Sh4 = 0x1a6, // Hitachi SH4.
        Sh5 = 0x1a8, // Hitachi SH5.
        Thumb = 0x1c2, // Thumb.
        WceMipsV2 = 0x169 // MIPS little-endian WCE v2.
    };

    public enum class MagicNumber : unsigned short
    {
        Pe32 = IMAGE_NT_OPTIONAL_HDR32_MAGIC,
        Pe32Plus = IMAGE_NT_OPTIONAL_HDR64_MAGIC
    };

    [Flags]
    public enum class ImageCharacteristics : unsigned short
    {
        RelocsStripped = 0x0001, // Image only, Windows CE, and Microsoft Windows NT and later. This indicates that the file does not contain base relocations and must therefore be loaded at its preferred base address.
        ExecutableImage = 0x0002, // Image only. This indicates that the image file is valid and can be run. If this flag is not set, it indicates a linker error.
        LineNumsStripped = 0x0004, // COFF line numbers have been removed. This flag is deprecated and should be zero.
        LocalSymsStripped = 0x0008, // COFF symbol table entries for local symbols have been removed. This flag is deprecated and should be zero.
        AggressiveWsTrim = 0x0010, // Obsolete. Aggressively trim working set. This flag is deprecated for Windows 2000 and later and must be zero.
        LargeAddressAware = 0x0020, // Application can handle > 2-GB addresses.
        Reserved = 0x0040, // This flag is reserved for future use.
        BytesReversedLo = 0x0080, // Little endian: the least significant bit (LSB) precedes the most significant bit (MSB) in memory. This flag is deprecated and should be zero.
        ThirtyTwoBitMachine = 0x0100, // Machine is based on a 32-bit-word architecture.
        DebugStripped = 0x0200, // Debugging information is removed from the image file.
        RemovableRunFromSwap = 0x0400, // If the image is on removable media, fully load it and copy it to the swap file.
        NetRunFromSwap = 0x0800, // If the image is on network media, fully load it and copy it to the swap file.
        System = 0x1000, // The image file is a system file, not a user program.
        Dll = 0x2000, // The image file is a dynamic-link library (DLL). Such files are considered executable files for almost all purposes, although they cannot be directly run.
        UpSystemOnly = 0x4000, // The file should be run only on a uniprocessor machine.
        BytesReversedHi = 0x8000 // Big endian: the MSB precedes the LSB in memory. This flag is deprecated and should be zero.
    };

	[Flags]
	public enum class SectionCharacteristics : unsigned int
	{
		TypeReg = 0, // Reserved for future use.
		TypeDSect = 1, // Reserved for future use.
		TypeNoLoad = 2,  // Reserved for future use.
		TypeGroup = 4,  // Reserved for future use.
		TypeNoPad = 8,  // The section should not be padded to the next boundary. This flag is obsolete and is replaced by IMAGE_SCN_ALIGN_1BYTES. This is valid only for object files.
		TypeCopy = 0x10,  // Reserved for future use.
		ContainsCode = 0x20,  // The section contains executable code.
		ContainsInitializedData = 0x40,  // The section contains initialized data.
		ContainsUninitializedData = 0x80,  // The section contains uninitialized data.
		LinkerOther = 0x100,  // Reserved for future use.
		LinkerInfo = 0x200,  // The section contains comments or other information. The .drectve section has this type. This is valid for object files only.
		TypeOver = 0x400,  // Reserved for future use.
		LinkerRemove = 0x800,  // The section will not become part of the image. This is valid only for object files.
		LinkerComdat = 0x1000,  // The section contains COMDAT data. For more information, see COMDAT Sections (Object Only). This is valid only for object files.
		MemProtected = 0x4000,
		NoDeferSpecExc = 0x4000,
		GPRel = 0x8000, // The section contains data referenced through the global pointer (GP).
		MemFardata = 0x8000,
		MemSysheap = 0x10000,
		MemPurgeable = 0x20000, // Reserved for future use.
		Mem16Bit = 0x20000, // Reserved for future use.
		MemLocked = 0x40000, // Reserved for future use.
		MemPreload = 0x80000, // Reserved for future use.
		Align1Bytes = 0x100000, // Align data on a 1-byte boundary. Valid only for object files.
		Align2Bytes = 0x200000, // Align data on a 2-byte boundary. Valid only for object files.
		Align4Bytes = 0x300000, // Align data on a 4-byte boundary. Valid only for object files.
		Align8Bytes = 0x400000, // Align data on an 8-byte boundary. Valid only for object files.
		Align16Bytes = 0x500000, // Align data on a 16-byte boundary. Valid only for object files.
		Align32Bytes = 0x600000, // Align data on a 32-byte boundary. Valid only for object files.
		Align64Bytes = 0x700000, // Align data on a 64-byte boundary. Valid only for object files.
		Align128Bytes = 0x800000, // Align data on a 128-byte boundary. Valid only for object files.
		Align256Bytes = 0x900000, // Align data on a 256-byte boundary. Valid only for object files.
		Align512Bytes = 0xA00000, // Align data on a 512-byte boundary. Valid only for object files.
		Align1024Bytes = 0xB00000, // Align data on a 1024-byte boundary. Valid only for object files.
		Align2048Bytes = 0xC00000, // Align data on a 2048-byte boundary. Valid only for object files.
		Align4096Bytes = 0xD00000, // Align data on a 4096-byte boundary. Valid only for object files.
		Align8192Bytes = 0xE00000, // Align data on an 8192-byte boundary. Valid only for object files.
		AlignMask = 0xF00000,
		LinkerNRelocOvfl = 0x1000000, // The section contains extended relocations.
		MemDiscardable = 0x2000000, // The section can be discarded as needed.
		MemNotCached = 0x4000000, // The section cannot be cached.
		MemNotPaged = 0x8000000, // The section is not pageable.
		MemShared = 0x10000000, // The section can be shared in memory.
		MemExecute = 0x20000000, // The section can be executed as code.
		MemRead = 0x40000000, // The section can be read.
		MemWrite = 0x80000000 // The section can be written to.
	};

	[Flags]
	public enum class CorFlags
	{
		ILOnly = 1,
		Requires32Bit = 2,
		ILLibrary = 4,
		StrongNameSigned = 8,
		NativeEntryPoint = 0x10,
		TrackDebugData = 0x10000,
		Prefers32Bit = 0x20000
	};

	public enum class Subsystem : unsigned short {
		Unknown = 0, // An unknown subsystem.
		Native, // Device drivers and native Windows processes.
		WindowsGui, // The Windows graphical user interface (GUI) subsystem.
		WindowsCui, // The Windows character subsystem.
		Os2Cui = 5, // The OS/2 character subsystem.
		PosixCui = 7, // The Posix character subsystem.
		NativeWindows, // Native Win9x driver.
		WindowsCeGui, // Windows CE.
		EfiApplication, // An Extensible Firmware Interface (EFI) application.
		EfiBootServiceDriver, // An EFI driver with boot services.
		EfiRuntimeDriver, // An EFI driver with run-time services.
		EfiRom, // An EFI ROM image.
		Xbox, // XBOX.
		WindowsBootApplication = 16 // Windows boot application..
	};

	[Flags]
	public enum class DllCharacteristics : unsigned short {
		// 0x0001 - Reserved, must be zero.
		// 0x0001 - Reserved, must be zero.
		// 0x0004 - Reserved, must be zero.
		// 0x0008 - Reserved, must be zero.
		HighEntropyVa = 0x0020, // Image can handle a high entropy 64-bit virtual address space.
		DynamicBase = 0x0040, // DLL can be relocated at load time.
		ForceIntegrity = 0x0080, // Code Integrity checks are enforced.
		NxCompact = 0x0100, // Image is NX compatible.
		NoIsolation = 0x0200, // Isolation aware, but do not isolate the image.
		NoSeh = 0x0400, // Does not use structured exception (SE) handling. No SE handler may be called in this image.
		NoBind = 0x0800, // Do not bind the image.
		AppContainer = 0x1000, // Image must execute in an AppContainer.
		WdmDriver = 0x2000, // A WDM driver.
		GuardCf = 0x4000, // Image supports Control Flow Guard.
		TerminalServerAware = 0x8000 // Terminal Server aware.
	};

	typedef struct _LS_IMAGE_OPTIONAL_HEADER {
		// Standard fields.
		WORD Magic;
		BYTE MajorLinkerVersion;
		BYTE MinorLinkerVersion;
		DWORD SizeOfCode;
		DWORD SizeOfInitializedData;
		DWORD SizeOfUninitializedData;
		DWORD AddressOfEntryPoint;
		DWORD BaseOfCode;
		DWORD BaseOfData;

		// NT additional fields.
		ULONGLONG ImageBase;
		DWORD SectionAlignment;
		DWORD FileAlignment;
		WORD MajorOperatingSystemVersion;
		WORD MinorOperatingSystemVersion;
		WORD MajorImageVersion;
		WORD MinorImageVersion;
		WORD MajorSubsystemVersion;
		WORD MinorSubsystemVersion;
		DWORD Win32VersionValue;
		DWORD SizeOfImage;
		DWORD SizeOfHeaders;
		DWORD CheckSum;
		WORD OsSubsystem;
		WORD Characteristics;
		ULONGLONG SizeOfStackReserve;
		ULONGLONG SizeOfStackCommit;
		ULONGLONG SizeOfHeapReserve;
		ULONGLONG SizeOfHeapCommit;
		DWORD LoaderFlags;
		DWORD NumberOfRvaAndSizes;

		// Data directories.
		IMAGE_DATA_DIRECTORY DataDirectory[IMAGE_NUMBEROF_DIRECTORY_ENTRIES];

		_LS_IMAGE_OPTIONAL_HEADER(IMAGE_OPTIONAL_HEADER32& header) {
			Magic = header.Magic;
			MajorLinkerVersion = header.MajorLinkerVersion;
			MinorLinkerVersion = header.MinorLinkerVersion;
			SizeOfCode = header.SizeOfCode;
			SizeOfInitializedData = header.SizeOfInitializedData;
			SizeOfUninitializedData = header.SizeOfUninitializedData;
			AddressOfEntryPoint = header.AddressOfEntryPoint;
			BaseOfCode = header.BaseOfCode;
			BaseOfData = header.BaseOfData;
			ImageBase = header.ImageBase;
			SectionAlignment = header.SectionAlignment;
			FileAlignment = header.FileAlignment;
			MajorOperatingSystemVersion = header.MajorOperatingSystemVersion;
			MinorOperatingSystemVersion = header.MinorOperatingSystemVersion;
			MajorImageVersion = header.MajorImageVersion;
			MinorImageVersion = header.MinorImageVersion;
			MajorSubsystemVersion = header.MajorSubsystemVersion;
			MinorSubsystemVersion = header.MinorSubsystemVersion;
			Win32VersionValue = header.Win32VersionValue;
			SizeOfImage = header.SizeOfImage;
			SizeOfHeaders = header.SizeOfHeaders;
			CheckSum = header.CheckSum;
			OsSubsystem = header.Subsystem;
			Characteristics = header.DllCharacteristics;
			SizeOfStackReserve = header.SizeOfStackReserve;
			SizeOfStackCommit = header.SizeOfStackCommit;
			SizeOfHeapReserve = header.SizeOfHeapReserve;
			SizeOfHeapCommit = header.SizeOfHeapCommit;
			LoaderFlags = header.LoaderFlags;
			NumberOfRvaAndSizes = header.NumberOfRvaAndSizes;

			memcpy(DataDirectory, header.DataDirectory, sizeof(IMAGE_DATA_DIRECTORY) * IMAGE_NUMBEROF_DIRECTORY_ENTRIES);
		}

		_LS_IMAGE_OPTIONAL_HEADER(IMAGE_OPTIONAL_HEADER64& header) {
			Magic = header.Magic;
			MajorLinkerVersion = header.MajorLinkerVersion;
			MinorLinkerVersion = header.MinorLinkerVersion;
			SizeOfCode = header.SizeOfCode;
			SizeOfInitializedData = header.SizeOfInitializedData;
			SizeOfUninitializedData = header.SizeOfUninitializedData;
			AddressOfEntryPoint = header.AddressOfEntryPoint;
			BaseOfCode = header.BaseOfCode;
			BaseOfData = 0;
			ImageBase = header.ImageBase;
			SectionAlignment = header.SectionAlignment;
			FileAlignment = header.FileAlignment;
			MajorOperatingSystemVersion = header.MajorOperatingSystemVersion;
			MinorOperatingSystemVersion = header.MinorOperatingSystemVersion;
			MajorImageVersion = header.MajorImageVersion;
			MinorImageVersion = header.MinorImageVersion;
			MajorSubsystemVersion = header.MajorSubsystemVersion;
			MinorSubsystemVersion = header.MinorSubsystemVersion;
			Win32VersionValue = header.Win32VersionValue;
			SizeOfImage = header.SizeOfImage;
			SizeOfHeaders = header.SizeOfHeaders;
			CheckSum = header.CheckSum;
			OsSubsystem = header.Subsystem;
			Characteristics = header.DllCharacteristics;
			SizeOfStackReserve = header.SizeOfStackReserve;
			SizeOfStackCommit = header.SizeOfStackCommit;
			SizeOfHeapReserve = header.SizeOfHeapReserve;
			SizeOfHeapCommit = header.SizeOfHeapCommit;
			LoaderFlags = header.LoaderFlags;
			NumberOfRvaAndSizes = header.NumberOfRvaAndSizes;

			memcpy(DataDirectory, header.DataDirectory, sizeof(IMAGE_DATA_DIRECTORY) * IMAGE_NUMBEROF_DIRECTORY_ENTRIES);
		}

	} LS_IMAGE_OPTIONAL_HEADER, * PLS_IMAGE_OPTIONAL_HEADER;

	public ref class DirectoryEntry
	{
	public:
		property UInt32 VirtualAddress { UInt32 get() { return _wrapper->VirtualAddress; } }
		property UInt32 Size { UInt32 get() { return _wrapper->Size; } }

		DirectoryEntry(IMAGE_DATA_DIRECTORY& dir) {
			_wrapper = new IMAGE_DATA_DIRECTORY();
			memcpy(_wrapper, &dir, sizeof(IMAGE_DATA_DIRECTORY));
		}

		~DirectoryEntry() {
			delete _wrapper;
		}

	protected:
		!DirectoryEntry() {
			delete _wrapper;
		}

	private:
		PIMAGE_DATA_DIRECTORY _wrapper;
	};

	public ref class CoffHeader
	{
	public:
		property MachineType Machine { MachineType get() { return (MachineType)_wrapper->Machine; } }
		property UInt16 NumberOfSections { UInt16 get() { return _wrapper->NumberOfSections; } }
		property DateTime^ TimeDateStamp { DateTime^ get() { return Core::GetDateTimeFromTimeT(_wrapper->TimeDateStamp); } }
		property UInt32 PointerToSymbolTable { UInt32 get() { return _wrapper->PointerToSymbolTable; } }
		property UInt32 NumberOfSymbols { UInt32 get() { return _wrapper->NumberOfSymbols; } }
		property UInt16 SizeOfOptionalHeader { UInt16 get() { return _wrapper->SizeOfOptionalHeader; } }
		property ImageCharacteristics Characteristics { ImageCharacteristics get() { return (ImageCharacteristics)_wrapper->Characteristics; } }

		CoffHeader(IMAGE_FILE_HEADER& header) {
			_wrapper = new IMAGE_FILE_HEADER();
			memcpy(_wrapper, &header, sizeof(IMAGE_FILE_HEADER));
		}

		~CoffHeader() {
			delete _wrapper;
		}

	protected:
		!CoffHeader() {
			delete _wrapper;
		}

	private:
		PIMAGE_FILE_HEADER _wrapper;
	};

	public ref class OptionalHeader
	{
	public:
		property MagicNumber Magic { MagicNumber get() { return (MagicNumber)_wrapper->Magic; } }
		property Byte MajorLinkerVersion { Byte get() { return _wrapper->MajorLinkerVersion; } }
		property Byte MinorLinkerVersion { Byte get() { return _wrapper->MinorLinkerVersion; } }
		property Int32 SizeOfCode { Int32 get() { return _wrapper->SizeOfCode; } }
		property Int32 SizeOfInitializedData { Int32 get() { return _wrapper->SizeOfInitializedData; } }
		property Int32 SizeOfUninitializedData { Int32 get() { return _wrapper->SizeOfUninitializedData; } }
		property Int32 AddressOfEntryPoint { Int32 get() { return _wrapper->AddressOfEntryPoint; } }
		property Int32 BaseOfCode { Int32 get() { return _wrapper->BaseOfCode; } }
		property Int32 BaseOfData { Int32 get() { return _wrapper->BaseOfData; } }
		property UInt64 ImageBase { UInt64 get() { return _wrapper->ImageBase; } }
		property Int32 SectionAlignment { Int32 get() { return _wrapper->SectionAlignment; } }
		property Int32 FileAlignment { Int32 get() { return _wrapper->FileAlignment; } }
		property UInt16 MajorOperatingSystemVersion { UInt16 get() { return _wrapper->MajorOperatingSystemVersion; } }
		property UInt16 MinorOperatingSystemVersion { UInt16 get() { return _wrapper->MinorOperatingSystemVersion; } }
		property UInt16 MajorImageVersion { UInt16 get() { return _wrapper->MajorImageVersion; } }
		property UInt16 MinorImageVersion { UInt16 get() { return _wrapper->MinorImageVersion; } }
		property UInt16 MajorSubsystemVersion { UInt16 get() { return _wrapper->MajorSubsystemVersion; } }
		property UInt16 MinorSubsystemVersion { UInt16 get() { return _wrapper->MinorSubsystemVersion; } }
		property Int32 SizeOfImage { Int32 get() { return _wrapper->SizeOfImage; } }
		property Int32 SizeOfHeaders { Int32 get() { return _wrapper->SizeOfHeaders; } }
		property UInt32 CheckSum { UInt32 get() { return _wrapper->CheckSum; } }
		property Subsystem OsSubsystem { Subsystem get() { return (Subsystem)_wrapper->OsSubsystem; } }
		property DllCharacteristics Characteristics { DllCharacteristics get() { return (DllCharacteristics)_wrapper->Characteristics; } }
		property UInt64 SizeOfStackReserve { UInt64 get() { return _wrapper->SizeOfStackReserve; } }
		property UInt64 SizeOfStackCommit { UInt64 get() { return _wrapper->SizeOfStackCommit; } }
		property UInt64 SizeOfHeapReserve { UInt64 get() { return _wrapper->SizeOfHeapReserve; } }
		property UInt64 SizeOfHeapCommit { UInt64 get() { return _wrapper->SizeOfHeapCommit; } }
		property Int32 NumberOfRvaAndSizes { Int32 get() { return _wrapper->NumberOfRvaAndSizes; } }
		property DirectoryEntry^ ExportTable { DirectoryEntry^ get() { return gcnew DirectoryEntry(_wrapper->DataDirectory[IMAGE_DIRECTORY_ENTRY_EXPORT]); } }
		property DirectoryEntry^ ImportTable { DirectoryEntry^ get() { return gcnew DirectoryEntry(_wrapper->DataDirectory[IMAGE_DIRECTORY_ENTRY_IMPORT]); } }
		property DirectoryEntry^ ResourceTable { DirectoryEntry^ get() { return gcnew DirectoryEntry(_wrapper->DataDirectory[IMAGE_DIRECTORY_ENTRY_RESOURCE]); } }
		property DirectoryEntry^ ExceptionTable { DirectoryEntry^ get() { return gcnew DirectoryEntry(_wrapper->DataDirectory[IMAGE_DIRECTORY_ENTRY_EXCEPTION]); } }
		property DirectoryEntry^ SecurityTable { DirectoryEntry^ get() { return gcnew DirectoryEntry(_wrapper->DataDirectory[IMAGE_DIRECTORY_ENTRY_SECURITY]); } }
		property DirectoryEntry^ BaseRelocationTable { DirectoryEntry^ get() { return gcnew DirectoryEntry(_wrapper->DataDirectory[IMAGE_DIRECTORY_ENTRY_BASERELOC]); } }
		property DirectoryEntry^ DebugTable { DirectoryEntry^ get() { return gcnew DirectoryEntry(_wrapper->DataDirectory[IMAGE_DIRECTORY_ENTRY_DEBUG]); } }
		property DirectoryEntry^ ArchitectureTable { DirectoryEntry^ get() { return gcnew DirectoryEntry(_wrapper->DataDirectory[IMAGE_DIRECTORY_ENTRY_ARCHITECTURE]); } }
		property DirectoryEntry^ GlobalPointerTable { DirectoryEntry^ get() { return gcnew DirectoryEntry(_wrapper->DataDirectory[IMAGE_DIRECTORY_ENTRY_GLOBALPTR]); } }
		property DirectoryEntry^ ThreadLocalStorageTable { DirectoryEntry^ get() { return gcnew DirectoryEntry(_wrapper->DataDirectory[IMAGE_DIRECTORY_ENTRY_TLS]); } }
		property DirectoryEntry^ LoadConfigTable { DirectoryEntry^ get() { return gcnew DirectoryEntry(_wrapper->DataDirectory[IMAGE_DIRECTORY_ENTRY_LOAD_CONFIG]); } }
		property DirectoryEntry^ BoundImportTable { DirectoryEntry^ get() { return gcnew DirectoryEntry(_wrapper->DataDirectory[IMAGE_DIRECTORY_ENTRY_BOUND_IMPORT]); } }
		property DirectoryEntry^ ImportAddressTable { DirectoryEntry^ get() { return gcnew DirectoryEntry(_wrapper->DataDirectory[IMAGE_DIRECTORY_ENTRY_IAT]); } }
		property DirectoryEntry^ DelayImportTable { DirectoryEntry^ get() { return gcnew DirectoryEntry(_wrapper->DataDirectory[IMAGE_DIRECTORY_ENTRY_DELAY_IMPORT]); } }
		property DirectoryEntry^ CorHeaderTable { DirectoryEntry^ get() { return gcnew DirectoryEntry(_wrapper->DataDirectory[IMAGE_DIRECTORY_ENTRY_COM_DESCRIPTOR]); } }

		OptionalHeader(IMAGE_OPTIONAL_HEADER32& header) {
			_wrapper = new LS_IMAGE_OPTIONAL_HEADER(header);
		}

		OptionalHeader(IMAGE_OPTIONAL_HEADER64& header) {
			_wrapper = new LS_IMAGE_OPTIONAL_HEADER(header);
		}

		~OptionalHeader() {
			delete _wrapper;
		}

	protected:
		!OptionalHeader() {
			delete _wrapper;
		}

	private:
		PLS_IMAGE_OPTIONAL_HEADER _wrapper;
	};

	public ref class SectionHeader
	{
	public:
		property String^ Name { String^ get() { return gcnew String((char*)_wrapper->Name); } }
		property UInt32 VirtualSize { UInt32 get() { return _wrapper->Misc.VirtualSize; } }
		property UInt32 VirtualAddress { UInt32 get() { return _wrapper->VirtualAddress; } }
		property UInt32 SizeOfRawData { UInt32 get() { return _wrapper->SizeOfRawData; } }
		property UInt32 PointerToRawData { UInt32 get() { return _wrapper->PointerToRawData; } }
		property UInt32 PointerToRelocations { UInt32 get() { return _wrapper->PointerToRelocations; } }
		property UInt32 PointerToLineNumbers { UInt32 get() { return _wrapper->PointerToLinenumbers; } }
		property UInt16 NumberOfRelocations { UInt16 get() { return _wrapper->NumberOfRelocations; } }
		property UInt16 NumberOfLinenumbers { UInt16 get() { return _wrapper->NumberOfLinenumbers; } }
		property SectionCharacteristics Characteristics { SectionCharacteristics get() { return (SectionCharacteristics)_wrapper->Characteristics; } }

		SectionHeader(IMAGE_SECTION_HEADER& header) {
			_wrapper = new IMAGE_SECTION_HEADER();
			memcpy(_wrapper, &header, sizeof(IMAGE_SECTION_HEADER));
		}

		~SectionHeader() {
			delete _wrapper;
		}

	protected:
		!SectionHeader() {
			delete _wrapper;
		}

	private:
		PIMAGE_SECTION_HEADER _wrapper;
	};

	public ref class CorHeader
	{
	public:
		property UInt16 MajorRuntimeVersion { UInt16 get() { return _wrapper->MajorRuntimeVersion; } }
		property UInt16 MinorRuntimeVersion { UInt16 get() { return _wrapper->MinorRuntimeVersion; } }
		property DirectoryEntry^ MetadataDirectory { DirectoryEntry^ get() { return gcnew DirectoryEntry(_wrapper->MetaData); } }
		property CorFlags Flags { CorFlags get() { return (CorFlags)_wrapper->Flags; } }
		property Int32 EntryPointTokenOrRelativeVirtualAddress { Int32 get() { return _wrapper->EntryPointToken; } }
		property DirectoryEntry^ ResourcesDirectory { DirectoryEntry^ get() { return gcnew DirectoryEntry(_wrapper->Resources); } }
		property DirectoryEntry^ StrongNameSignatureDirectory { DirectoryEntry^ get() { return gcnew DirectoryEntry(_wrapper->StrongNameSignature); } }
		property DirectoryEntry^ CodeManagerTable { DirectoryEntry^ get() { return gcnew DirectoryEntry(_wrapper->CodeManagerTable); } }
		property DirectoryEntry^ VTableFixupsDirectory { DirectoryEntry^ get() { return gcnew DirectoryEntry(_wrapper->VTableFixups); } }
		property DirectoryEntry^ ExportAddressTableJumpsDirectory { DirectoryEntry^ get() { return gcnew DirectoryEntry(_wrapper->ExportAddressTableJumps); } }
		property DirectoryEntry^ ManagedNativeHeaderDirectory { DirectoryEntry^ get() { return gcnew DirectoryEntry(_wrapper->ManagedNativeHeader); } }

		CorHeader(IMAGE_COR20_HEADER& header) {
			_wrapper = new IMAGE_COR20_HEADER();
			memcpy(_wrapper, &header, sizeof(IMAGE_COR20_HEADER));
		}

		~CorHeader() {
			delete _wrapper;
		}

	protected:
		!CorHeader() {
			delete _wrapper;
		}

	private:
		PIMAGE_COR20_HEADER _wrapper;
	};

	public ref class PortableExecutable
	{
	public:
		property String^ Name { String^ get() { return _name; } }
		property Int32 MetadataStartOffset { Int32 get() { return _wrapper->MetadataStartOffset; } }
		property Int32 MetadataSize { Int32 get() { return _wrapper->MetadataSize; } }
		property Int32 CoffHeaderOffset { Int32 get() { return _wrapper->CoffHeaderOffset; } }
		property CoffHeader^ FileHeader { CoffHeader^ get() { return _coff_header; } }
		property Boolean IsCoffOnly { Boolean get() { return _wrapper->IsCoffOnly; } }
		property Int32 OptionalHeaderOffset { Int32 get() { return _wrapper->OptionalHeaderOffset; } }
		property OptionalHeader^ OptHeader { OptionalHeader^ get() { return _opt_headers; } }

		property array<SectionHeader^>^ SectionHeaders {
			array<SectionHeader^>^ get() {
				size_t section_count = _wrapper->SectionHeaders.size();
				if (section_count == 0)
					return gcnew array<SectionHeader^>(0);

				array<SectionHeader^>^ output = gcnew array<SectionHeader^>(static_cast<int>(section_count));
				int index = 0;
				for (IMAGE_SECTION_HEADER& header : _wrapper->SectionHeaders)
					output[index++] = gcnew SectionHeader(header);

				return output;
			}
		}

		property Int32 ClrHeaderStartOffset { Int32 get() { return _wrapper->CorHeaderOffset; } }
		property CorHeader^ ClrHeader { CorHeader^ get() { return _cor_header; } }
		property Boolean IsConsoleApplication { Boolean get() { return _is_console; } }
		property Boolean IsDll { Boolean get() { return _is_dll; } }
		property Boolean IsExe { Boolean get() { return _is_exe; } }

		PortableExecutable(String^ file_path) {
			if (String::IsNullOrEmpty(file_path))
				throw gcnew ArgumentNullException("File path cannot be null or empty.");

			WWuString wrapped_path = Core::GetWideFromManagedString(file_path);
			_wrapper = new Core::PeHelper::LS_PORTABLE_EXECUTABLE();

			Core::LSRESULT result = _pe_helper->GetPeHeaders(wrapped_path, _wrapper);
			if (result.Result != ERROR_SUCCESS)
				throw gcnew NativeException(result);

			_name = Path::GetFileName(file_path);
			if (_wrapper->IsCoffOnly) {
				_coff_header = gcnew CoffHeader(_wrapper->CoffHeader);
				_opt_headers = nullptr;
			}
			else {
				if (_wrapper->Magic == IMAGE_NT_OPTIONAL_HDR32_MAGIC) {
					_coff_header = gcnew CoffHeader(_wrapper->NtHeaders32.FileHeader);
					_opt_headers = gcnew OptionalHeader(_wrapper->NtHeaders32.OptionalHeader);
				}
				else {
					_coff_header = gcnew CoffHeader(_wrapper->NtHeaders64.FileHeader);
					_opt_headers = gcnew OptionalHeader(_wrapper->NtHeaders64.OptionalHeader);
				}
			}

			_is_dll = (_coff_header->Characteristics & ImageCharacteristics::Dll) == ImageCharacteristics::Dll;
			_is_exe = (_coff_header->Characteristics & ImageCharacteristics::Dll) != ImageCharacteristics::Dll;

			if (_opt_headers != nullptr)
				_is_console = _opt_headers->OsSubsystem == Subsystem::WindowsCui;
			else
				_is_console = false;

			if (_wrapper->CorHeaderOffset == -1)
				_cor_header = nullptr;
			else
				_cor_header = gcnew CorHeader(_wrapper->CorHeader);
		}

	private:
		String^ _name;
		CoffHeader^ _coff_header;
		OptionalHeader^ _opt_headers;
		CorHeader^ _cor_header;
		Boolean _is_console;
		Boolean _is_exe;
		Boolean _is_dll;
		Core::PeHelper::PLS_PORTABLE_EXECUTABLE _wrapper;
		Core::PeHelper* _pe_helper;
	};
}