using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Scv.Api.Infrastructure.Options;

namespace Scv.Api.Documents.Conversion;

internal sealed class LibreOfficeConversionConcurrencyLimiter : IDisposable
{
    private readonly SemaphoreSlim _gate;

    public LibreOfficeConversionConcurrencyLimiter(IOptions<DocumentConversionOptions> options)
    {
        _gate = new SemaphoreSlim(
            options.Value.MaxConcurrentLibreOfficeConversions,
            options.Value.MaxConcurrentLibreOfficeConversions);
    }

    public Task WaitAsync(CancellationToken cancellationToken) => _gate.WaitAsync(cancellationToken);

    public void Release() => _gate.Release();

    public void Dispose() => _gate.Dispose();
}
