using System;
using System.IO;
using System.Runtime.InteropServices;
using LibSnitcher.Interop;

#nullable enable

namespace LibSnitcher;

// This implementation is based on the .NET core 'System.Reflection.PortableExecutable'.
// Was modified to construct from a raw module handle.

public class PortableExecutable
{
    private readonly CoffHeader _coffHeader;

	private readonly OptionalHeaders? _peHeader;

	private readonly SectionHeader[] _sectionHeaders;

	private readonly CorHeader? _corHeader;

	private readonly bool _isLoadedImage;

	private readonly int _metadataStartOffset = -1;

	private readonly int _metadataSize;

	private readonly int _coffHeaderStartOffset = -1;

	private readonly int _corHeaderStartOffset = -1;

	private readonly int _peHeaderStartOffset = -1;

	internal const ushort DosSignature = 23117;

	internal const int PESignatureOffsetLocation = 60;

	internal const uint PESignature = 17744u;

	internal const int PESignatureSize = 4;

	public int MetadataStartOffset => _metadataStartOffset;
	public int MetadataSize => _metadataSize;
	public CoffHeader CoffHeader => _coffHeader;
	public int CoffHeaderStartOffset => _coffHeaderStartOffset;
	public bool IsCoffOnly => _peHeader == null;
	public OptionalHeaders? OptionalHeaders => _peHeader;
	public int OptionalHeadersStartOffset => _peHeaderStartOffset;
	public SectionHeader[] SectionHeaders => _sectionHeaders;
	public CorHeader? CorHeader => _corHeader;
	public int CorHeaderStartOffset => _corHeaderStartOffset;
	public bool IsConsoleApplication
	{
		get
		{
			if (_peHeader != null)
			{
				return _peHeader.Subsystem == Subsystem.WindowsCui;
			}
			return false;
		}
	}

	public bool IsDll => (_coffHeader.Characteristics & ImageCharacteristics.Dll) != 0;
	public bool IsExe => (_coffHeader.Characteristics & ImageCharacteristics.Dll) == 0;
	
    public PortableExecutable(Stream peStream)
		: this(peStream, 0)
	{ }

	public PortableExecutable(Stream peStream, int size)
		: this(peStream, size, isLoadedImage: false)
	{ }

	public PortableExecutable(Stream peStream, int size, bool isLoadedImage)
	{
		if (peStream == null)
			throw new ArgumentNullException("Stream cannot be null.");

		if (!peStream.CanRead || !peStream.CanSeek)
			throw new ArgumentException("Stream is in an invalid state.");

		_isLoadedImage = isLoadedImage;
		int andValidateSize = StreamExtensions.GetAndValidateSize(peStream, size);
		PeBinaryReader reader = new(peStream, andValidateSize);
		SkipDosHeader(ref reader, out var isCOFFOnly);
		_coffHeaderStartOffset = reader.CurrentOffset;
		_coffHeader = new CoffHeader(ref reader);
		if (!isCOFFOnly)
		{
			_peHeaderStartOffset = reader.CurrentOffset;
			_peHeader = new OptionalHeaders(ref reader);
		}
		_sectionHeaders = ReadSectionHeaders(ref reader);
		if (!isCOFFOnly && TryCalculateCorHeaderOffset(out var startOffset))
		{
			_corHeaderStartOffset = startOffset;
			reader.Seek(startOffset);
			_corHeader = new CorHeader(ref reader);
		}
		CalculateMetadataLocation(andValidateSize, out _metadataStartOffset, out _metadataSize);
	}

    public PortableExecutable(SafeModuleHandle hModule)
    {
        if (hModule is null || hModule.IsClosed || hModule.IsInvalid)
            throw new ArgumentException("Module handle in invalid state.");

        IntPtr dangerousModuleHandle = hModule.DangerousGetHandle();
        IMAGE_DOS_HEADER dosHeader = (IMAGE_DOS_HEADER?)Marshal.PtrToStructure(dangerousModuleHandle, typeof(IMAGE_DOS_HEADER))
            ?? throw new ArgumentNullException("Failed to marshal DOS header.");

        bool coffOnly = false;
        if (dosHeader.e_magic != 23117)
        {
            if (dosHeader.e_magic == 0 && dosHeader.e_cblp == ushort.MaxValue)
				throw new BadImageFormatException("Unknown file format.");

            coffOnly = true;
        }
        IntPtr offset = IntPtr.Zero;
        if (!coffOnly)
        {
            uint? sigOffset = (uint?)Marshal.PtrToStructure(dangerousModuleHandle + 60, typeof(uint));
            offset = (IntPtr)((ulong)dangerousModuleHandle + sigOffset);
            uint? peSig = (uint?)Marshal.PtrToStructure(offset, typeof(uint));
            if (peSig != 17744)
                throw new BadImageFormatException("Invalid PE signature.");

            ushort? magic = (ushort?)Marshal.PtrToStructure((IntPtr)((ulong)offset + 24), typeof(ushort));

            object imageHeaders;
            switch (magic)
            {
                case 0x10b:
                    imageHeaders = (IMAGE_NT_HEADERS32?)Marshal.PtrToStructure((IntPtr)((ulong)dangerousModuleHandle + dosHeader.e_lfanew), typeof(IMAGE_NT_HEADERS32))
                        ?? throw new ArgumentNullException("Failed to marshal image headers.");
                    _peHeaderStartOffset = (int)sigOffset + 24;
                    _peHeader = new(((IMAGE_NT_HEADERS32)imageHeaders).OptionalHeaders);
					_coffHeaderStartOffset = (int)dosHeader.e_lfanew + 4;
                    _coffHeader = new(((IMAGE_NT_HEADERS32)imageHeaders).FileHeader);
                    break;

                case 0x20b:
                    imageHeaders = (IMAGE_NT_HEADERS64?)Marshal.PtrToStructure((IntPtr)((ulong)dangerousModuleHandle + dosHeader.e_lfanew), typeof(IMAGE_NT_HEADERS64))
                        ?? throw new ArgumentNullException("Failed to marshal image headers.");
                    _peHeaderStartOffset = (int)sigOffset + 24;
                    _peHeader = new(((IMAGE_NT_HEADERS64)imageHeaders).OptionalHeaders);
					_coffHeaderStartOffset = (int)dosHeader.e_lfanew + 4;
                    _coffHeader = new(((IMAGE_NT_HEADERS64)imageHeaders).FileHeader);
                    break;

                default:
                    throw new BadImageFormatException("Invalid magic number.");
            }
        }
        if (_coffHeader is null)
            throw new BadImageFormatException("Invalid COFF header.");

        IntPtr sectionOffset = (IntPtr)((ulong)offset + (uint)_coffHeader.SizeOfOptionalHeader + 24);
        _sectionHeaders = ReadSectionHeaders(sectionOffset);

        if (!coffOnly && TryCalculateCorHeaderOffset(out var corOffset))
        {
            _corHeaderStartOffset = corOffset;
            IMAGE_COR20_HEADER? nativeCorHeader = (IMAGE_COR20_HEADER?)Marshal.PtrToStructure((IntPtr)((ulong)dangerousModuleHandle + (uint)corOffset), typeof(IMAGE_COR20_HEADER));
            if (nativeCorHeader is not null)
                _corHeader = new(nativeCorHeader.Value);
        }

        if (_peHeader is not null)
            CalculateMetadataLocation(_peHeader.SizeOfImage, out _metadataStartOffset, out _metadataSize);
        
        // TODO: Figure out a safer way.
        else
            CalculateMetadataLocation(out _metadataStartOffset, out _metadataSize);
    }

	private bool TryCalculateCorHeaderOffset(out int startOffset)
	{
        if (_peHeader is not null)
        {
            if (!TryGetDirectoryOffset(_peHeader.CorHeaderTableDirectory, out startOffset, canCrossSectionBoundary: false))
		    {
		    	startOffset = -1;
		    	return false;
		    }
		    int size = (int)_peHeader.CorHeaderTableDirectory.Size;
		    if (size < 72)
		    {
		    	throw new BadImageFormatException("Invalid COR header size.");
		    }
		    return true;
        }
		
        startOffset = 0;
        return false;
	}

	private static void SkipDosHeader(ref PeBinaryReader reader, out bool isCOFFOnly)
	{
		ushort num = reader.ReadUInt16();
		if (num != 23117)
		{
			if (num == 0 && reader.ReadUInt16() == ushort.MaxValue)
			{
				throw new BadImageFormatException("Unknown file format.");
			}
			isCOFFOnly = true;
			reader.Seek(0);
		}
		else
		{
			isCOFFOnly = false;
		}
		if (!isCOFFOnly)
		{
			reader.Seek(60);
			int offset = reader.ReadInt32();
			reader.Seek(offset);
			uint num2 = reader.ReadUInt32();
			if (num2 != 17744)
			{
				throw new BadImageFormatException("Invalid PE signature.");
			}
		}
	}

	private SectionHeader[] ReadSectionHeaders(ref PeBinaryReader reader)
	{
		int numberOfSections = _coffHeader.NumberOfSections;
		if (numberOfSections < 0)
		{
			throw new BadImageFormatException("Invalid number of sections.");
		}
		SectionHeader[] headers = new SectionHeader[numberOfSections];
		for (int i = 0; i < numberOfSections; i++)
            headers[i] = new SectionHeader(ref reader);

		return headers;
	}

    private SectionHeader[] ReadSectionHeaders(IntPtr sectionOffset)
	{
		int numberOfSections = _coffHeader.NumberOfSections;
		if (numberOfSections < 0)
			throw new BadImageFormatException("Invalid number of sections.");

		SectionHeader[] headers = new SectionHeader[numberOfSections];
        int sectionHeaderSize = 40;
		for (int i = 0; i < numberOfSections; i++)
        {
            IMAGE_SECTION_HEADER? nativeSecHeader = (IMAGE_SECTION_HEADER?)Marshal.PtrToStructure(sectionOffset, typeof(IMAGE_SECTION_HEADER));
            if (nativeSecHeader is not null)
                headers[i] = new SectionHeader(nativeSecHeader.Value);

            sectionOffset += sectionHeaderSize;
        }

		return headers;
	}

	public bool TryGetDirectoryOffset(DirectoryEntry directory, out int offset)
	{
		return TryGetDirectoryOffset(directory, out offset, canCrossSectionBoundary: true);
	}

	internal bool TryGetDirectoryOffset(DirectoryEntry directory, out int offset, bool canCrossSectionBoundary)
	{
		int containingSectionIndex = GetContainingSectionIndex((int)directory.VirtualAddress);
		if (containingSectionIndex < 0)
		{
			offset = -1;
			return false;
		}
		int num = (int)directory.VirtualAddress - _sectionHeaders[containingSectionIndex].VirtualAddress;
		if (!canCrossSectionBoundary && directory.Size > _sectionHeaders[containingSectionIndex].VirtualSize - num)
			throw new BadImageFormatException("Section too small.");

		offset = _isLoadedImage ? (int)directory.VirtualAddress : (_sectionHeaders[containingSectionIndex].PointerToRawData + num);
		return true;
	}

	public int GetContainingSectionIndex(int relativeVirtualAddress)
	{
		for (int i = 0; i < _sectionHeaders.Length; i++)
		{
			if (_sectionHeaders[i].VirtualAddress <= relativeVirtualAddress && relativeVirtualAddress < _sectionHeaders[i].VirtualAddress + _sectionHeaders[i].VirtualSize)
			{
				return i;
			}
		}
		return -1;
	}

	internal int IndexOfSection(string name)
	{
		for (int i = 0; i < SectionHeaders.Length; i++)
		{
			if (SectionHeaders[i].Name.Equals(name, StringComparison.Ordinal))
			{
				return i;
			}
		}
		return -1;
	}

	private void CalculateMetadataLocation(long peImageSize, out int start, out int size)
	{
		if (IsCoffOnly)
		{
			int num = IndexOfSection(".cormeta");
			if (num == -1)
			{
				start = -1;
				size = 0;
				return;
			}
			if (_isLoadedImage)
			{
				start = SectionHeaders[num].VirtualAddress;
				size = SectionHeaders[num].VirtualSize;
			}
			else
			{
				start = SectionHeaders[num].PointerToRawData;
				size = SectionHeaders[num].SizeOfRawData;
			}
		}
		else
		{
			if (_corHeader == null)
			{
				start = 0;
				size = 0;
				return;
			}
			if (!TryGetDirectoryOffset(_corHeader.MetadataDirectory, out start, canCrossSectionBoundary: false))
			{
				throw new BadImageFormatException("COR header missing data directory.");
			}
			size = (int)_corHeader.MetadataDirectory.Size;
		}
		if (start < 0 || start >= peImageSize || size <= 0 || start > peImageSize - size)
		{
			throw new BadImageFormatException("Invalid metadata section span.");
		}
	}

    private void CalculateMetadataLocation(out int start, out int size)
	{
		if (IsCoffOnly)
		{
			int num = IndexOfSection(".cormeta");
			if (num == -1)
			{
				start = -1;
				size = 0;
				return;
			}
			if (_isLoadedImage)
			{
				start = SectionHeaders[num].VirtualAddress;
				size = SectionHeaders[num].VirtualSize;
			}
			else
			{
				start = SectionHeaders[num].PointerToRawData;
				size = SectionHeaders[num].SizeOfRawData;
			}
		}
		else
		{
			if (_corHeader == null)
			{
				start = 0;
				size = 0;
				return;
			}
			if (!TryGetDirectoryOffset(_corHeader.MetadataDirectory, out start, canCrossSectionBoundary: false))
			{
				throw new BadImageFormatException("COR header missing data directory.");
			}
			size = (int)_corHeader.MetadataDirectory.Size;
		}
	}
}

public class CoffHeader
{
    internal const int Size = 20;

	public MachineType Machine { get; }
	public short NumberOfSections { get; }
	public int TimeDateStamp { get; }
	public int PointerToSymbolTable { get; }
	public int NumberOfSymbols { get; }
	public short SizeOfOptionalHeader { get; }
	public ImageCharacteristics Characteristics { get; }

	internal CoffHeader(ref PeBinaryReader reader)
	{
		Machine = (MachineType)reader.ReadUInt16();
		NumberOfSections = reader.ReadInt16();
		TimeDateStamp = reader.ReadInt32();
		PointerToSymbolTable = reader.ReadInt32();
		NumberOfSymbols = reader.ReadInt32();
		SizeOfOptionalHeader = reader.ReadInt16();
		Characteristics = (ImageCharacteristics)reader.ReadUInt16();
	}

    internal CoffHeader(IMAGE_FILE_HEADER header)
    {
        Machine = header.Machine;
        NumberOfSections = (short)header.NUmberOfSections;
        TimeDateStamp = (int)header.TimeDateStamp;
        PointerToSymbolTable = (int)header.PointerToSymbolTable;
        NumberOfSymbols = (int)header.NumberOfSymbols;
        SizeOfOptionalHeader = (short)header.SizeOfOptionalHeader;
        Characteristics = header.Characteristics;
    }
}

public sealed class OptionalHeaders
{
	internal const int OffsetOfChecksum = 64;

	public MagicNumber Magic { get; }
	public byte MajorLinkerVersion { get; }
	public byte MinorLinkerVersion { get; }
	public int SizeOfCode { get; }
	public int SizeOfInitializedData { get; }
	public int SizeOfUninitializedData { get; }
	public int AddressOfEntryPoint { get; }
	public int BaseOfCode { get; }
	public int BaseOfData { get; }
	public ulong ImageBase { get; }
	public int SectionAlignment { get; }
	public int FileAlignment { get; }
	public ushort MajorOperatingSystemVersion { get; }
	public ushort MinorOperatingSystemVersion { get; }
	public ushort MajorImageVersion { get; }
	public ushort MinorImageVersion { get; }
	public ushort MajorSubsystemVersion { get; }
	public ushort MinorSubsystemVersion { get; }
	public int SizeOfImage { get; }
	public int SizeOfHeaders { get; }
	public uint CheckSum { get; }
	public Subsystem Subsystem { get; }
	public DllCharacteristics DllCharacteristics { get; }
	public ulong SizeOfStackReserve { get; }
	public ulong SizeOfStackCommit { get; }
	public ulong SizeOfHeapReserve { get; }
	public ulong SizeOfHeapCommit { get; }
	public int NumberOfRvaAndSizes { get; }
	public DirectoryEntry ExportTableDirectory { get; }
	public DirectoryEntry ImportTableDirectory { get; }
	public DirectoryEntry ResourceTableDirectory { get; }
	public DirectoryEntry ExceptionTableDirectory { get; }
	public DirectoryEntry CertificateTableDirectory { get; }
	public DirectoryEntry BaseRelocationTableDirectory { get; }
	public DirectoryEntry DebugTableDirectory { get; }
	public DirectoryEntry CopyrightTableDirectory { get; }
	public DirectoryEntry GlobalPointerTableDirectory { get; }
	public DirectoryEntry ThreadLocalStorageTableDirectory { get; }
	public DirectoryEntry LoadConfigTableDirectory { get; }
	public DirectoryEntry BoundImportTableDirectory { get; }
	public DirectoryEntry ImportAddressTableDirectory { get; }
	public DirectoryEntry DelayImportTableDirectory { get; }
	public DirectoryEntry CorHeaderTableDirectory { get; }

	internal static int Size(bool is32Bit)
        => 72 + 4 * (is32Bit ? 4 : 8) + 4 + 4 + 128;

	internal OptionalHeaders(ref PeBinaryReader reader)
	{
		MagicNumber pEMagic = (MagicNumber)reader.ReadUInt16();
		if (pEMagic != MagicNumber.Pe32 && pEMagic != MagicNumber.Pe32Plus)
			throw new BadImageFormatException("Unknown PE magic number.");

		Magic = pEMagic;
		MajorLinkerVersion = reader.ReadByte();
		MinorLinkerVersion = reader.ReadByte();
		SizeOfCode = reader.ReadInt32();
		SizeOfInitializedData = reader.ReadInt32();
		SizeOfUninitializedData = reader.ReadInt32();
		AddressOfEntryPoint = reader.ReadInt32();
		BaseOfCode = reader.ReadInt32();
		if (pEMagic == MagicNumber.Pe32Plus)
			BaseOfData = 0;
		else
			BaseOfData = reader.ReadInt32();

		if (pEMagic == MagicNumber.Pe32Plus)
			ImageBase = reader.ReadUInt64();
		else
			ImageBase = reader.ReadUInt32();

		SectionAlignment = reader.ReadInt32();
		FileAlignment = reader.ReadInt32();
		MajorOperatingSystemVersion = reader.ReadUInt16();
		MinorOperatingSystemVersion = reader.ReadUInt16();
		MajorImageVersion = reader.ReadUInt16();
		MinorImageVersion = reader.ReadUInt16();
		MajorSubsystemVersion = reader.ReadUInt16();
		MinorSubsystemVersion = reader.ReadUInt16();
		reader.ReadUInt32();
		SizeOfImage = reader.ReadInt32();
		SizeOfHeaders = reader.ReadInt32();
		CheckSum = reader.ReadUInt32();
		Subsystem = (Subsystem)reader.ReadUInt16();
		DllCharacteristics = (DllCharacteristics)reader.ReadUInt16();
		if (pEMagic == MagicNumber.Pe32Plus)
		{
			SizeOfStackReserve = reader.ReadUInt64();
			SizeOfStackCommit = reader.ReadUInt64();
			SizeOfHeapReserve = reader.ReadUInt64();
			SizeOfHeapCommit = reader.ReadUInt64();
		}
		else
		{
			SizeOfStackReserve = reader.ReadUInt32();
			SizeOfStackCommit = reader.ReadUInt32();
			SizeOfHeapReserve = reader.ReadUInt32();
			SizeOfHeapCommit = reader.ReadUInt32();
		}
		reader.ReadUInt32();
		NumberOfRvaAndSizes = reader.ReadInt32();
		ExportTableDirectory = new DirectoryEntry(ref reader);
		ImportTableDirectory = new DirectoryEntry(ref reader);
		ResourceTableDirectory = new DirectoryEntry(ref reader);
		ExceptionTableDirectory = new DirectoryEntry(ref reader);
		CertificateTableDirectory = new DirectoryEntry(ref reader);
		BaseRelocationTableDirectory = new DirectoryEntry(ref reader);
		DebugTableDirectory = new DirectoryEntry(ref reader);
		CopyrightTableDirectory = new DirectoryEntry(ref reader);
		GlobalPointerTableDirectory = new DirectoryEntry(ref reader);
		ThreadLocalStorageTableDirectory = new DirectoryEntry(ref reader);
		LoadConfigTableDirectory = new DirectoryEntry(ref reader);
		BoundImportTableDirectory = new DirectoryEntry(ref reader);
		ImportAddressTableDirectory = new DirectoryEntry(ref reader);
		DelayImportTableDirectory = new DirectoryEntry(ref reader);
		CorHeaderTableDirectory = new DirectoryEntry(ref reader);
		new DirectoryEntry(ref reader);
	}

    internal OptionalHeaders(IMAGE_OPTIONAL_HEADER64 header)
    {
        Magic = header.Magic;
        MajorLinkerVersion = header.MajorLinkerVersion;
        MinorLinkerVersion = header.MinorLinkerVersion;
        SizeOfCode = (int)header.SizeOfCode;
        SizeOfInitializedData = (int)header.SizeOfInitializedData;
        SizeOfUninitializedData = (int)header.SizeOfUninitializedData;
        AddressOfEntryPoint = (int)header.AddressOfEntryPoint;
        BaseOfCode = (int)header.BaseOfCode;
        BaseOfData = 0;
        ImageBase = header.ImageBase;
        SectionAlignment = (int)header.SectionAlignment;
        FileAlignment = (int)header.FileAlignment;
        MajorOperatingSystemVersion = header.MajorOperatingSystemVersion;
        MinorOperatingSystemVersion = header.MinorOperatingSystemVersion;
        MajorImageVersion = header.MajorImageVersion;
        MinorImageVersion = header.MinorImageVersion;
        MajorSubsystemVersion = header.MajorSubsystemVersion;
        MinorSubsystemVersion = header.MinorSubsystemVersion;
        SizeOfImage = (int)header.SizeOfImage;
        SizeOfHeaders = (int)header.SizeOfHeaders;
        CheckSum = header.CheckSum;
        Subsystem = header.Subsystem;
        DllCharacteristics = header.DllCharacteristics;
        SizeOfStackReserve = header.SizeOfStackCommit;
        SizeOfStackCommit = header.SizeOfStackCommit;
        SizeOfHeapReserve = header.SizeOfHeapReserve;
        SizeOfHeapCommit = header.SizeOfStackCommit;
        NumberOfRvaAndSizes = (int)header.NumberOfRvaAndSizes;
        ExportTableDirectory = header.DataDirectory[(int)DirectoryEntryType.ExportTable];
        ImportTableDirectory = header.DataDirectory[(int)DirectoryEntryType.ImportTable];
        ResourceTableDirectory = header.DataDirectory[(int)DirectoryEntryType.ResourceTable];
        ExceptionTableDirectory = header.DataDirectory[(int)DirectoryEntryType.ExceptionTable];
        CertificateTableDirectory = header.DataDirectory[(int)DirectoryEntryType.CertificateTable];
        BaseRelocationTableDirectory = header.DataDirectory[(int)DirectoryEntryType.BaseRealocationTable];
        DebugTableDirectory = header.DataDirectory[(int)DirectoryEntryType.Debug];
        CopyrightTableDirectory = header.DataDirectory[(int)DirectoryEntryType.Architecture];
        GlobalPointerTableDirectory = header.DataDirectory[(int)DirectoryEntryType.GlobalPtr];
        ThreadLocalStorageTableDirectory = header.DataDirectory[(int)DirectoryEntryType.TlsTable];
        LoadConfigTableDirectory = header.DataDirectory[(int)DirectoryEntryType.LoadConfigTable];
        BoundImportTableDirectory = header.DataDirectory[(int)DirectoryEntryType.BoundImport];
        ImportAddressTableDirectory = header.DataDirectory[(int)DirectoryEntryType.ImportAddressTable];
        DelayImportTableDirectory = header.DataDirectory[(int)DirectoryEntryType.DelayImportDescriptor];
        CorHeaderTableDirectory = header.DataDirectory[(int)DirectoryEntryType.ClrRuntimeHeader];
    }

    internal OptionalHeaders(IMAGE_OPTIONAL_HEADER32 header)
    {
        Magic = header.Magic;
        MajorLinkerVersion = header.MajorLinkerVersion;
        MinorLinkerVersion = header.MinorLinkerVersion;
        SizeOfCode = (int)header.SizeOfCode;
        SizeOfInitializedData = (int)header.SizeOfInitializedData;
        SizeOfUninitializedData = (int)header.SizeOfUninitializedData;
        AddressOfEntryPoint = (int)header.AddressOfEntryPoint;
        BaseOfCode = (int)header.BaseOfCode;
        BaseOfData = 0;
        ImageBase = header.ImageBase;
        SectionAlignment = (int)header.SectionAlignment;
        FileAlignment = (int)header.FileAlignment;
        MajorOperatingSystemVersion = header.MajorOperatingSystemVersion;
        MinorOperatingSystemVersion = header.MinorOperatingSystemVersion;
        MajorImageVersion = header.MajorImageVersion;
        MinorImageVersion = header.MinorImageVersion;
        MajorSubsystemVersion = header.MajorSubsystemVersion;
        MinorSubsystemVersion = header.MinorSubsystemVersion;
        SizeOfImage = (int)header.SizeOfImage;
        SizeOfHeaders = (int)header.SizeOfHeaders;
        CheckSum = header.CheckSum;
        Subsystem = header.Subsystem;
        DllCharacteristics = header.DllCharacteristics;
        SizeOfStackReserve = header.SizeOfStackCommit;
        SizeOfStackCommit = header.SizeOfStackCommit;
        SizeOfHeapReserve = header.SizeOfHeapReserve;
        SizeOfHeapCommit = header.SizeOfStackCommit;
        NumberOfRvaAndSizes = (int)header.NumberOfRvaAndSizes;
        ExportTableDirectory = header.DataDirectory[(int)DirectoryEntryType.ExportTable];
        ImportTableDirectory = header.DataDirectory[(int)DirectoryEntryType.ImportTable];
        ResourceTableDirectory = header.DataDirectory[(int)DirectoryEntryType.ResourceTable];
        ExceptionTableDirectory = header.DataDirectory[(int)DirectoryEntryType.ExceptionTable];
        CertificateTableDirectory = header.DataDirectory[(int)DirectoryEntryType.CertificateTable];
        BaseRelocationTableDirectory = header.DataDirectory[(int)DirectoryEntryType.BaseRealocationTable];
        DebugTableDirectory = header.DataDirectory[(int)DirectoryEntryType.Debug];
        CopyrightTableDirectory = header.DataDirectory[(int)DirectoryEntryType.Architecture];
        GlobalPointerTableDirectory = header.DataDirectory[(int)DirectoryEntryType.GlobalPtr];
        ThreadLocalStorageTableDirectory = header.DataDirectory[(int)DirectoryEntryType.TlsTable];
        LoadConfigTableDirectory = header.DataDirectory[(int)DirectoryEntryType.LoadConfigTable];
        BoundImportTableDirectory = header.DataDirectory[(int)DirectoryEntryType.BoundImport];
        ImportAddressTableDirectory = header.DataDirectory[(int)DirectoryEntryType.ImportAddressTable];
        DelayImportTableDirectory = header.DataDirectory[(int)DirectoryEntryType.DelayImportDescriptor];
        CorHeaderTableDirectory = header.DataDirectory[(int)DirectoryEntryType.ClrRuntimeHeader];
    }
}

public readonly struct SectionHeader
{
	internal const int NameSize = 8;
	internal const int Size = 40;

	public string Name { get; }
	public int VirtualSize { get; }
	public int VirtualAddress { get; }
	public int SizeOfRawData { get; }
	public int PointerToRawData { get; }
	public int PointerToRelocations { get; }
	public int PointerToLineNumbers { get; }
	public ushort NumberOfRelocations { get; }
	public ushort NumberOfLineNumbers { get; }

	public SectionCharacteristics SectionCharacteristics { get; }

	internal SectionHeader(ref PeBinaryReader reader)
	{
		Name = reader.ReadNullPaddedUTF8(8);
		VirtualSize = reader.ReadInt32();
		VirtualAddress = reader.ReadInt32();
		SizeOfRawData = reader.ReadInt32();
		PointerToRawData = reader.ReadInt32();
		PointerToRelocations = reader.ReadInt32();
		PointerToLineNumbers = reader.ReadInt32();
		NumberOfRelocations = reader.ReadUInt16();
		NumberOfLineNumbers = reader.ReadUInt16();
		SectionCharacteristics = (SectionCharacteristics)reader.ReadUInt32();
	}

    internal SectionHeader(IMAGE_SECTION_HEADER header)
    {
        Name = new string(header.Name);
        VirtualSize = (int)header.VirtualSize;
        VirtualAddress = (int)header.VirtualAddress;
        SizeOfRawData = (int)header.SizeOfRawData;
        PointerToRawData = (int)header.PointerToRawData;
        PointerToRelocations = (int)header.PointerToRealocations;
        PointerToLineNumbers = (int)header.PointerToLinenumbers;
        NumberOfRelocations = header.NumberOfRealocations;
        NumberOfLineNumbers = header.NumberOfLinenumbers;
    }
}

public sealed class CorHeader
{
	public ushort MajorRuntimeVersion { get; }
	public ushort MinorRuntimeVersion { get; }
	public DirectoryEntry MetadataDirectory { get; }
	public CorFlags Flags { get; }
	public int EntryPointTokenOrRelativeVirtualAddress { get; }
	public DirectoryEntry ResourcesDirectory { get; }
	public DirectoryEntry StrongNameSignatureDirectory { get; }
	public DirectoryEntry CodeManagerTableDirectory { get; }
	public DirectoryEntry VtableFixupsDirectory { get; }
	public DirectoryEntry ExportAddressTableJumpsDirectory { get; }
	public DirectoryEntry ManagedNativeHeaderDirectory { get; }
	
    internal CorHeader(ref PeBinaryReader reader)
	{
		reader.ReadInt32();
		MajorRuntimeVersion = reader.ReadUInt16();
		MinorRuntimeVersion = reader.ReadUInt16();
		MetadataDirectory = new DirectoryEntry(ref reader);
		Flags = (CorFlags)reader.ReadUInt32();
		EntryPointTokenOrRelativeVirtualAddress = reader.ReadInt32();
		ResourcesDirectory = new DirectoryEntry(ref reader);
		StrongNameSignatureDirectory = new DirectoryEntry(ref reader);
		CodeManagerTableDirectory = new DirectoryEntry(ref reader);
		VtableFixupsDirectory = new DirectoryEntry(ref reader);
		ExportAddressTableJumpsDirectory = new DirectoryEntry(ref reader);
		ManagedNativeHeaderDirectory = new DirectoryEntry(ref reader);
	}

    internal CorHeader(IMAGE_COR20_HEADER header)
    {
        MajorRuntimeVersion = header.MajorRuntimeVersion;
        MinorRuntimeVersion = header.MinorRuntimeVersion;
        MetadataDirectory = header.MetaData;
        Flags = header.Flags;
        EntryPointTokenOrRelativeVirtualAddress = (int)header.EntryPointTokenOrRelativeVirtualAddress;
        ResourcesDirectory = header.Resources;
        StrongNameSignatureDirectory = header.StrongNameSignature;
        CodeManagerTableDirectory = header.CodeManagerTable;
        VtableFixupsDirectory = header.VTableFixups;
        ExportAddressTableJumpsDirectory = header.ExportAddressTableJumps;
        ManagedNativeHeaderDirectory = header.ManagedNativeHeader;
    }
}

[StructLayout(LayoutKind.Sequential)]
public readonly struct DirectoryEntry
{
    public readonly uint VirtualAddress;
    public readonly uint Size;

    internal DirectoryEntry(uint rva, uint size)
        => (VirtualAddress, Size) = (rva, size);

    internal DirectoryEntry(ref PeBinaryReader reader)
        => (VirtualAddress, Size) = (reader.ReadUInt32(), reader.ReadUInt32());
}