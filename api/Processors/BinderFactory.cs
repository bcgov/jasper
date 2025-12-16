using FluentValidation;
using JCCommon.Clients.FileServices;
using LazyCache;
using MapsterMapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Scv.Api.Documents;
using Scv.Api.Services;
using Scv.Core.Helpers.Extensions;
using Scv.Db.Contants;
using Scv.Models;
using System;
using System.Collections.Generic;
using System.Security.Claims;

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
    IValidator<BinderDto> basicValidator,
    IAppCache cache,
    IConfiguration configuration,
    IDocumentConverter documentConverter,
    IMapper mapper,
    IDarsService darsService) : IBinderFactory
{
    private readonly FileServicesClient _filesClient = filesClient;
    private readonly ClaimsPrincipal _currentUser = currentUser;
    private readonly ILogger<BinderFactory> _logger = logger;
    private readonly IValidator<BinderDto> _basicValidator = basicValidator;
    private readonly IAppCache _cache = cache;
    private readonly IConfiguration _configuration = configuration;
    private readonly IDocumentConverter _documentConverter = documentConverter;
    private readonly IMapper _mapper = mapper;
    private readonly IDarsService _darsService = darsService;

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
            throw new ArgumentException("Unable to determine which processor to load");
        }

        switch (courtClassCode)
        {
            case CourtClassCd.C:
            case CourtClassCd.F:
            case CourtClassCd.L:
            case CourtClassCd.M:
                return new JudicialBinderProcessor(
                    _filesClient,
                     _currentUser,
                     _basicValidator,
                     dto,
                     _cache,
                     _mapper,
                     _configuration,
                     _darsService);
            case CourtClassCd.A:
            case CourtClassCd.Y:
            case CourtClassCd.T:
                return new KeyDocumentsBinderProcessor(
                    _filesClient,
                    _currentUser,
                    _basicValidator,
                    dto,
                    _cache,
                    _logger,
                    _configuration,
                    _documentConverter,
                    _mapper);
            default:
                throw new NotSupportedException("Unsupported processor");
        }
    }
}