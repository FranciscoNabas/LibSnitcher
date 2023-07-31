using System;
using System.Text;
using System.Runtime.InteropServices;
using LibSnitcher.Interop;
using System.IO;

namespace LibSnitcher;

internal class Utils
{
    internal static string GetSystemErrorText(int error_code)
    {
        StringBuilder buffer = new(1024);
        int result = NativeFunctions.FormatMessage(
            FormatMessageFlags.AllocateBuffer |
            FormatMessageFlags.FromSystem |
            FormatMessageFlags.IgnoreInserts,
            IntPtr.Zero,
            error_code,
            0,
            out buffer,
            buffer.Capacity,
            IntPtr.Zero
        );
        if (result == 0)
            throw new SystemException($"Error formatting message. {Marshal.GetLastWin32Error()}");

        return buffer.ToString();
    }
}

internal static class StreamExtensions
{
	internal const int StreamCopyBufferSize = 81920;

	internal static int GetAndValidateSize(Stream stream, int size)
	{
		long num = stream.Length - stream.Position;
		if (size < 0 || size > num)
		{
			throw new ArgumentOutOfRangeException("Size is out of the stream boundaries.");
		}
		if (size != 0)
		{
			return size;
		}
		if (num > int.MaxValue)
		{
			throw new ArgumentException("Stream is to large.");
		}
		return (int)num;
	}
}

internal readonly struct PeBinaryReader
{
	private readonly long _startOffset;

	private readonly long _maxOffset;

	private readonly BinaryReader _reader;

	public int CurrentOffset => (int)(_reader.BaseStream.Position - _startOffset);

	public PeBinaryReader(Stream stream, int size)
	{
		_startOffset = stream.Position;
		_maxOffset = _startOffset + size;
		_reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true);
	}

	public void Seek(int offset)
	{
		CheckBounds(_startOffset, offset);
		_reader.BaseStream.Seek(offset, SeekOrigin.Begin);
	}

	public byte[] ReadBytes(int count)
	{
		CheckBounds(_reader.BaseStream.Position, count);
		return _reader.ReadBytes(count);
	}

	public byte ReadByte()
	{
		CheckBounds(1u);
		return _reader.ReadByte();
	}

	public short ReadInt16()
	{
		CheckBounds(2u);
		return _reader.ReadInt16();
	}

	public ushort ReadUInt16()
	{
		CheckBounds(2u);
		return _reader.ReadUInt16();
	}

	public int ReadInt32()
	{
		CheckBounds(4u);
		return _reader.ReadInt32();
	}

	public uint ReadUInt32()
	{
		CheckBounds(4u);
		return _reader.ReadUInt32();
	}

	public ulong ReadUInt64()
	{
		CheckBounds(8u);
		return _reader.ReadUInt64();
	}

	public string ReadNullPaddedUTF8(int byteCount)
	{
		byte[] array = ReadBytes(byteCount);
		int count = 0;
		for (int num = array.Length; num > 0; num--)
		{
			if (array[num - 1] != 0)
			{
				count = num;
				break;
			}
		}
		return Encoding.UTF8.GetString(array, 0, count);
	}

	private void CheckBounds(uint count)
	{
		if ((ulong)(_reader.BaseStream.Position + count) > (ulong)_maxOffset)
		{
			throw new BadImageFormatException("Image is too small.");
		}
	}

	private void CheckBounds(long startPosition, int count)
	{
		if ((ulong)(startPosition + (uint)count) > (ulong)_maxOffset)
		{
			throw new BadImageFormatException("Image too small or contains invalid offset or count.");
		}
	}
}