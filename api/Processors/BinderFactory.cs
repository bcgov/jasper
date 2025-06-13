using System;
using System.Collections.Generic;
using System.Security.Claims;
using FluentValidation;
using JCCommon.Clients.FileServices;
using Microsoft.Extensions.Logging;
using Scv.Api.Helpers.Extensions;
using Scv.Api.Models;
using Scv.Db.Contants;

namespace Scv.Api.Processors;

public interface IBinderFactory
{
    IBinderProcessor Create(BinderDto dto);
    IBinderProcessor Create(Dictionary<string, string> labels);
}

public class BinderFactory(
    FileServicesClient filesClient,
    ClaimsPrincipal currentUser,
    ILogger<BinderFactory> logger,
    IValidator<BinderDto> basicValidator
    ) : IBinderFactory
{
    private readonly FileServicesClient _filesClient = filesClient;
    private readonly ClaimsPrincipal _currentUser = currentUser;
    private readonly ILogger<BinderFactory> _logger = logger;
    private readonly IValidator<BinderDto> _basicValidator = basicValidator;

    public IBinderProcessor Create(Dictionary<string, string> labels)
    {
        return this.Create(new BinderDto { Labels = labels });
    }

    public IBinderProcessor Create(BinderDto dto)
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
                return new JudicialBinderProcessor(_filesClient, _currentUser, _basicValidator, dto);
            //case CourtClassCd.A:
            //case CourtClassCd.Y:
            //case CourtClassCd.T:
            default:
                throw new NotSupportedException("Unsupported processor");
        }
    }
}