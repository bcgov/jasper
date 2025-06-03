using System.Collections.Generic;
using System.Linq;

namespace Scv.Api.Infrastructure;

public class OperationResult
{
    public bool Succeeded { get; }
    public IReadOnlyList<string> Errors { get; }

    protected OperationResult(bool succeeded, IEnumerable<string> errors)
    {
        Succeeded = succeeded;
        Errors = (errors ?? []).ToList().AsReadOnly();
    }

    public static OperationResult Success() => new(true, null);
    public static OperationResult Failure(params string[] errors) => new(false, errors);
}

public class OperationResult<T> : OperationResult
{
    public T Payload { get; }

    private OperationResult(bool succeeded, T payload, IEnumerable<string> errors)
        : base(succeeded, errors)
    {
        Payload = payload;
    }

    public static OperationResult<T> Success(T data) => new(true, data, null);
    public static new OperationResult<T> Failure(params string[] errors) => new(false, default, errors);
}