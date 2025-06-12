using System;
using JCCommon.Clients.FileServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Scv.Api.Helpers.Extensions;
using Scv.Api.Models;
using Scv.Db.Contants;

namespace Scv.Api.Processors;

public interface IBinderFactory
{
    IBinderProcessor Generate(BinderDto dto);
}

public class BinderFactory(IServiceProvider serviceProvider, ILogger<BinderFactory> logger) : IBinderFactory
{
    private readonly IServiceProvider _serviceProvider = serviceProvider;
    private readonly ILogger<BinderFactory> _logger = logger;

    public IBinderProcessor Generate(BinderDto dto)
    {
        var courtClass = dto.Labels.GetValue(LabelConstants.COURT_CLASS_CD);
        var isValid = Enum.TryParse(courtClass, ignoreCase: true, out CourtClassCd courtClassCode);

        if (!isValid)
        {
            _logger.LogError("Court Class: {courtClass} is invalid.", courtClass);
            throw new ArgumentException("Unable to determine which BinderProcessor to load");
        }

        switch (courtClassCode)
        {
            case CourtClassCd.C:
            case CourtClassCd.F:
            case CourtClassCd.L:
            case CourtClassCd.M:
                return _serviceProvider.GetRequiredService<JudicialBinderProcessor>();
            //case CourtClassCd.A:
            //case CourtClassCd.Y:
            //case CourtClassCd.T:
            default:
                throw new NotSupportedException("Unsupported processor");
        }
    }
}