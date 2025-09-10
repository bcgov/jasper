using System.Security.Claims;
using System.Threading.Tasks;
using FluentValidation;
using JCCommon.Clients.FileServices;
using Newtonsoft.Json.Serialization;
using Scv.Api.Helpers.ContractResolver;
using Scv.Api.Models;

namespace Scv.Api.Processors;

public class KeyDocumentsBinderProcessor : BinderProcessorBase
{
    private readonly FileServicesClient _filesClient;

    public KeyDocumentsBinderProcessor(
        FileServicesClient filesClient,
        ClaimsPrincipal currentUser,
        IValidator<BinderDto> basicValidator,
        BinderDto dto) : base(currentUser, dto, basicValidator)
    {
        _filesClient = filesClient;
        _filesClient.JsonSerializerSettings.ContractResolver = new SafeContractResolver { NamingStrategy = new CamelCaseNamingStrategy() };
    }

    public override Task PreProcessAsync()
    {
        // Key Documents Binder are generated in the backend so we have full control
        // which data are saved. Overriding this method so Labels aren't cleared.
        return Task.CompletedTask;
    }
}
