using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Scv.Core.Exceptions;

namespace Scv.Api.Documents.Conversion;

internal sealed class BoundedMemoryStream : MemoryStream
{
    private readonly long _maximumLength;
    private readonly string _errorMessage;

    public BoundedMemoryStream(long maximumLength, string errorMessage)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maximumLength);
        if (maximumLength > int.MaxValue)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maximumLength),
                "MemoryStream cannot address more than Int32.MaxValue bytes.");
        }
        _maximumLength = maximumLength;
        _errorMessage = errorMessage;
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        EnsureCapacityForWrite(count);
        base.Write(buffer, offset, count);
    }

    public override void Write(ReadOnlySpan<byte> buffer)
    {
        EnsureCapacityForWrite(buffer.Length);
        base.Write(buffer);
    }

    public override ValueTask WriteAsync(
        ReadOnlyMemory<byte> buffer,
        CancellationToken cancellationToken = default)
    {
        EnsureCapacityForWrite(buffer.Length);
        return base.WriteAsync(buffer, cancellationToken);
    }

    public override Task WriteAsync(
        byte[] buffer,
        int offset,
        int count,
        CancellationToken cancellationToken)
    {
        EnsureCapacityForWrite(count);
        return base.WriteAsync(buffer, offset, count, cancellationToken);
    }

    public override void WriteByte(byte value)
    {
        EnsureCapacityForWrite(1);
        base.WriteByte(value);
    }

    public override void SetLength(long value)
    {
        if (value > _maximumLength)
        {
            throw new DocumentSizeLimitExceededException(_errorMessage);
        }
        EnsureCapacityForLength(value);
        base.SetLength(value);
    }

    public override int Capacity
    {
        get => base.Capacity;
        set
        {
            if (value > _maximumLength)
            {
                throw new DocumentSizeLimitExceededException(_errorMessage);
            }
            base.Capacity = value;
        }
    }

    private void EnsureCapacityForWrite(int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        if (Position > _maximumLength - count)
        {
            throw new DocumentSizeLimitExceededException(_errorMessage);
        }

        EnsureCapacityForLength(Position + count);
    }

    private void EnsureCapacityForLength(long requiredLength)
    {
        if (requiredLength <= Capacity)
        {
            return;
        }

        var nextCapacity = Capacity == 0
            ? Math.Min(256L, _maximumLength)
            : Capacity;
        while (nextCapacity < requiredLength)
        {
            nextCapacity = Math.Min(
                _maximumLength,
                Math.Max(requiredLength, nextCapacity * 2));
        }

        Capacity = checked((int)nextCapacity);
    }
}
