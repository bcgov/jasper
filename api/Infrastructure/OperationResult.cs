using System.Collections.Generic;

namespace Scv.Api.Infrastructure;

public class OperationResult<T>(bool succeeded, T payload, List<string> errors)
{
    public bool Succeeded { get; set; } = succeeded;
    public T Payload { get; set; } = payload;
    public List<string> Errors { get; set; } = errors;

    public static OperationResult<T> Success(T data) => new(true, data, []);
    public static OperationResult<T> Failure(params string[] errors) => new(false, default, [.. errors]);
}

public class OperationResult
{
    public bool Succeeded { get; }
    public List<string> Errors { get; } = [];

    private OperationResult(bool succeeded, List<string> errors)
    {
        Succeeded = succeeded;
        if (errors != null) Errors = errors;
    }

    public static OperationResult Success() => new(true, []);
    public static OperationResult Failure(params string[] errors) => new(false, [.. errors]);
}