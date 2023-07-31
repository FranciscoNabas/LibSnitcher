using System;
using System.Text;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using System.Runtime.CompilerServices;

namespace LibSnitcher.Interop;

[Flags]
internal enum FormatMessageFlags : uint
{
    AllocateBuffer = 0x00000100,
    IgnoreInserts = 0x00000200,
    FromString = 0x00000400,
    FromHModule = 0x00000800,
    FromSystem = 0x00001000,
    ArgumentArray = 0x00002000,
    MaxWidthAndMask = 0x000000FF
}

public enum MagicNumber : ushort {
    Pe32 = 0x10b,
    Pe32Plus = 0x20b
}

public enum MachineType : ushort {
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
}

[Flags]
public enum ImageCharacteristics : ushort {
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
}

[Flags]
public enum SectionCharacteristics : uint
{
	TypeReg = 0u,
	TypeDSect = 1u,
	TypeNoLoad = 2u,
	TypeGroup = 4u,
	TypeNoPad = 8u,
	TypeCopy = 0x10u,
	ContainsCode = 0x20u,
	ContainsInitializedData = 0x40u,
	ContainsUninitializedData = 0x80u,
	LinkerOther = 0x100u,
	LinkerInfo = 0x200u,
	TypeOver = 0x400u,
	LinkerRemove = 0x800u,
	LinkerComdat = 0x1000u,
	MemProtected = 0x4000u,
	NoDeferSpecExc = 0x4000u,
	GPRel = 0x8000u,
	MemFardata = 0x8000u,
	MemSysheap = 0x10000u,
	MemPurgeable = 0x20000u,
	Mem16Bit = 0x20000u,
	MemLocked = 0x40000u,
	MemPreload = 0x80000u,
	Align1Bytes = 0x100000u,
	Align2Bytes = 0x200000u,
	Align4Bytes = 0x300000u,
	Align8Bytes = 0x400000u,
	Align16Bytes = 0x500000u,
	Align32Bytes = 0x600000u,
	Align64Bytes = 0x700000u,
	Align128Bytes = 0x800000u,
	Align256Bytes = 0x900000u,
	Align512Bytes = 0xA00000u,
	Align1024Bytes = 0xB00000u,
	Align2048Bytes = 0xC00000u,
	Align4096Bytes = 0xD00000u,
	Align8192Bytes = 0xE00000u,
	AlignMask = 0xF00000u,
	LinkerNRelocOvfl = 0x1000000u,
	MemDiscardable = 0x2000000u,
	MemNotCached = 0x4000000u,
	MemNotPaged = 0x8000000u,
	MemShared = 0x10000000u,
	MemExecute = 0x20000000u,
	MemRead = 0x40000000u,
	MemWrite = 0x80000000u
}

[Flags]
public enum CorFlags
{
	ILOnly = 1,
	Requires32Bit = 2,
	ILLibrary = 4,
	StrongNameSigned = 8,
	NativeEntryPoint = 0x10,
	TrackDebugData = 0x10000,
	Prefers32Bit = 0x20000
}

public enum Subsystem : ushort {
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
}

[Flags]
public enum DllCharacteristics : ushort {
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
}

public enum DirectoryEntryType : uint {
    ExportTable,
    ImportTable,
    ResourceTable,
    ExceptionTable,
    CertificateTable,
    BaseRealocationTable,
    Debug,
    Architecture,
    GlobalPtr,
    TlsTable,
    LoadConfigTable,
    BoundImport,
    ImportAddressTable,
    DelayImportDescriptor,
    ClrRuntimeHeader
}

[StructLayout(LayoutKind.Sequential)]
internal struct IMAGE_DOS_HEADER {
    internal ushort e_magic;            // Magic number
    internal ushort e_cblp;             // Bytes on last page of file
    internal ushort e_cp;               // Pages in file
    internal ushort e_crlc;             // Relocations
    internal ushort e_cparhdr;          // Size of header in paragraphs
    internal ushort e_minalloc;         // Minimum extra paragraphs needed
    internal ushort e_maxalloc;         // Maximum extra paragraphs needed
    internal ushort e_ss;               // Initial (relative) SS value
    internal ushort e_sp;               // Initial SP value
    internal ushort e_csum;             // Checksum
    internal ushort e_ip;               // Initial IP value
    internal ushort e_cs;               // Initial (relative) CS value
    internal ushort e_lfarlc;           // File address of relocation table
    internal ushort e_ovno;             // Overlay number

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
    internal ushort[] e_res;            // Reserved words
    internal ushort e_oemid;            // OEM identifier (for e_oeminfo)
    internal ushort e_oeminfo;          // OEM information; e_oemid specific

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 10)]
    internal ushort[] e_res2;           // Reserved words
    internal uint   e_lfanew;           // File address of new exe header
}

[StructLayout(LayoutKind.Sequential)]
internal struct IMAGE_FILE_HEADER {
    internal MachineType Machine;
    internal ushort NUmberOfSections;
    internal uint TimeDateStamp;
    internal uint PointerToSymbolTable;
    internal uint NumberOfSymbols;
    internal ushort SizeOfOptionalHeader;
    internal ImageCharacteristics Characteristics;
}

[StructLayout(LayoutKind.Sequential)]
internal struct IMAGE_OPTIONAL_HEADER64 {

    // Standard fields.
    internal MagicNumber Magic;
    internal byte MajorLinkerVersion;
    internal byte MinorLinkerVersion;
    internal uint SizeOfCode;
    internal uint SizeOfInitializedData;
    internal uint SizeOfUninitializedData;
    internal uint AddressOfEntryPoint;
    internal uint BaseOfCode;

    // NT additional fields.
    internal ulong ImageBase;
    internal uint SectionAlignment;
    internal uint FileAlignment;
    internal ushort MajorOperatingSystemVersion;
    internal ushort MinorOperatingSystemVersion;
    internal ushort MajorImageVersion;
    internal ushort MinorImageVersion;
    internal ushort MajorSubsystemVersion;
    internal ushort MinorSubsystemVersion;
    internal readonly uint Win32VersionValue;
    internal uint SizeOfImage;
    internal uint SizeOfHeaders;
    internal uint CheckSum;
    internal Subsystem Subsystem;
    internal DllCharacteristics DllCharacteristics;
    internal ulong SizeOfStackReserve;
    internal ulong SizeOfStackCommit;
    internal ulong SizeOfHeapReserve;
    internal ulong SizeOfHeapCommit;
    internal readonly uint LoaderFlags;
    internal uint NumberOfRvaAndSizes;
    
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
    internal DirectoryEntry[] DataDirectory;
}

[StructLayout(LayoutKind.Sequential)]
internal struct IMAGE_OPTIONAL_HEADER32 {
    internal MagicNumber Magic;
    internal byte MajorLinkerVersion;
    internal byte MinorLinkerVersion;
    internal uint SizeOfCode;
    internal uint SizeOfInitializedData;
    internal uint SizeOfUninitializedData;
    internal uint AddressOfEntryPoint;
    internal uint BaseOfCode;
    internal uint BaseOfData;
    internal uint ImageBase;
    internal uint SectionAlignment;
    internal uint FileAlignment;
    internal ushort MajorOperatingSystemVersion;
    internal ushort MinorOperatingSystemVersion;
    internal ushort MajorImageVersion;
    internal ushort MinorImageVersion;
    internal ushort MajorSubsystemVersion;
    internal ushort MinorSubsystemVersion;
    internal readonly uint Win32VersionValue;
    internal uint SizeOfImage;
    internal uint SizeOfHeaders;
    internal uint CheckSum;
    internal Subsystem Subsystem;
    internal DllCharacteristics DllCharacteristics;
    internal uint SizeOfStackReserve;
    internal uint SizeOfStackCommit;
    internal uint SizeOfHeapReserve;
    internal uint SizeOfHeapCommit;
    internal readonly uint LoaderFlags;
    internal uint NumberOfRvaAndSizes;
    internal DirectoryEntry[] DataDirectory;
}

[StructLayout(LayoutKind.Sequential)]
internal struct IMAGE_NT_HEADERS64 {
    internal uint Signature;
    internal IMAGE_FILE_HEADER FileHeader;
    internal IMAGE_OPTIONAL_HEADER64 OptionalHeaders;
}

[StructLayout(LayoutKind.Sequential)]
internal struct IMAGE_NT_HEADERS32 {
    internal uint Signature;
    internal IMAGE_FILE_HEADER FileHeader;
    internal IMAGE_OPTIONAL_HEADER64 OptionalHeaders;
}

[StructLayout(LayoutKind.Explicit, CharSet = CharSet.Ansi)]
internal struct IMAGE_IMPORT_DESCRIPTOR {
    [FieldOffset(0)] internal uint Characteristics;
    [FieldOffset(0)] internal uint OriginalFirstThunk;
    [FieldOffset(4)] internal uint TimeDateStamp;
    [FieldOffset(8)] internal uint ForwarderChain;
    [FieldOffset(12)] internal uint Name;
    [FieldOffset(16)] internal uint FirstThunk;
}

[StructLayout(LayoutKind.Explicit)]
internal struct IMAGE_SECTION_HEADER {
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
    [FieldOffset(0)] internal char[] Name;
    [FieldOffset(8)] internal uint PhysicalAddress;
    [FieldOffset(8)] internal uint VirtualSize;
    [FieldOffset(12)] internal uint VirtualAddress;
    [FieldOffset(16)] internal uint SizeOfRawData;
    [FieldOffset(20)] internal uint PointerToRawData;
    [FieldOffset(24)] internal uint PointerToRealocations;
    [FieldOffset(28)] internal uint PointerToLinenumbers;
    [FieldOffset(32)] internal ushort NumberOfRealocations;
    [FieldOffset(34)] internal ushort NumberOfLinenumbers;
    [FieldOffset(38)] internal SectionCharacteristics Characteristics;
}

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
internal struct IMAGE_DELAYLOAD_DESCRIPTOR {
    internal readonly uint Attributes;
    internal uint Name;
    internal uint ModuleHandle;
    internal uint DelayImportAddressTable;
    internal uint DelayImportNameTable;
    internal uint BoundDelayImportTable;
    internal uint UnloadDelayImportTable;
    internal uint TimeStamp;
}

[StructLayout(LayoutKind.Sequential)]
internal struct IMAGE_COR20_HEADER
{
    internal uint cb;
    internal ushort MajorRuntimeVersion;
    internal ushort MinorRuntimeVersion;
    internal DirectoryEntry MetaData;
    internal CorFlags Flags;
    internal uint EntryPointTokenOrRelativeVirtualAddress;
    internal uint EntryPointRVA;
    internal DirectoryEntry Resources;
    internal DirectoryEntry StrongNameSignature;
    internal DirectoryEntry CodeManagerTable;
    internal DirectoryEntry VTableFixups;
    internal DirectoryEntry ExportAddressTableJumps;
    internal DirectoryEntry ManagedNativeHeader;
}

internal static class NativeConstants
{
    internal const int ERROR_INSUFFICIENT_BUFFER = 122;
    internal const int ERROR_MOD_NOT_FOUND = 126;
}

internal sealed class NativeFunctions
{
    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    internal static extern int FormatMessage(
        FormatMessageFlags dwFlags,
        IntPtr lpSource,
        int dwMessageId,
        uint dwLanguageId,
        out StringBuilder msgOut,
        int nSize,
        IntPtr Arguments
    );
    
    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode, EntryPoint = "GetModuleFileNameW")]
    internal static extern int GetModuleFileName(
        SafeModuleHandle hModule,
        StringBuilder lpFileName,
        int nSize
    );

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode, EntryPoint = "GetModuleHandleW")]
    internal static extern IntPtr GetModuleHandle(string lpModuleName);

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode, EntryPoint = "LoadLibraryW")]
    internal static extern SafeModuleHandle LoadLibrary(string lpModuleName);

    [DllImport("kernel32.dll", SetLastError = true)]
    internal static extern bool FreeLibrary(IntPtr hLibModule);
}

public class SafeModuleHandle : SafeHandleZeroOrMinusOneIsInvalid
{
    public SafeModuleHandle()
        : base(true) { }

    protected override bool ReleaseHandle()
        => NativeFunctions.FreeLibrary(handle);
}