using System;
using System.Runtime.Serialization;

namespace LibSnitcher;

[Serializable()]
public class NativeException : Exception
{
    private readonly int _native_error_number;

    public int NativeErrorNumber => _native_error_number;

    protected NativeException() : base() { }

    protected NativeException(SerializationInfo info, StreamingContext context) : base(info, context) { }

    public NativeException(int error_number)
        : base(Utils.GetSystemErrorText(error_number)) => _native_error_number = error_number;

    public NativeException(int error_number, string message) :
        base(message) => _native_error_number = error_number;

    public NativeException(int error_number, string message, Exception inner_exception) :
        base(message, inner_exception) => _native_error_number = error_number;
}