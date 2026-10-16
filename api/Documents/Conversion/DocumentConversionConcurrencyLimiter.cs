using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Scv.Api.Infrastructure.Options;

namespace Scv.Api.Documents.Conversion;

public sealed class DocumentConversionConcurrencyLimiter : IDisposable
{
    private readonly SemaphoreSlim _gate;

    public DocumentConversionConcurrencyLimiter(IOptions<DocumentConversionOptions> options)
    {
        _gate = new SemaphoreSlim(
            options.Value.MaxConcurrentConversions,
            options.Value.MaxConcurrentConversions);
    }

    public Task WaitAsync(CancellationToken cancellationToken) => _gate.WaitAsync(cancellationToken);

    public void Release() => _gate.Release();

    public void Dispose() => _gate.Dispose();
}
