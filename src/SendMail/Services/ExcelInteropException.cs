using System;

namespace SendMail.Services;

public sealed class ExcelInteropException : Exception
{
    public ExcelInteropException(string code, string message, Exception? innerException = null)
        : base(message, innerException)
    {
        Code = code;
    }

    public string Code { get; }
}

