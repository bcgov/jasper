using JCCommon.Clients.UserService;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Serialization;
using Scv.Core.Helpers.ContractResolver;
using Scv.Core.Helpers.Extensions;
using Scv.Models.JCUserService;
using System;
using System.Threading.Tasks;

namespace Scv.Api.Services
{
    public class JCUserService
    {
        #region Variables
        private UserServiceClient UserServiceClient { get; }
        private ILogger<JCUserService> Logger { get; }
        private string SupremeAgencyId { get; }
        #endregion Variables

        #region Constructor
        public JCUserService(UserServiceClient userServiceClient, ILogger<JCUserService> logger, IConfiguration configuration)
        {
            UserServiceClient = userServiceClient;
            UserServiceClient.JsonSerializerSettings.ContractResolver = new SafeContractResolver { NamingStrategy = new CamelCaseNamingStrategy() };
            Logger = logger;
            SupremeAgencyId = configuration.GetNonEmptyValue("Request:GetUserLoginDefaultAgencyId");
        }
        #endregion

        public async Task<GetParticipantInfoResponse> GetUserInfo(UserInfoRequest userInfoRequest)
        {
            try
            {
                var response = await UserServiceClient.UserGetParticipantInfoAsync(userInfoRequest.DomainUserGuid);

                if (response.ErrorMsg == null)
                {
                    response.AgenID = string.IsNullOrEmpty(response.AgenID) ? SupremeAgencyId : response.AgenID;
                    Logger.LogDebug($"SMGOV_USERGUID: {userInfoRequest.DomainUserGuid}, UserAgencyCd: {response.AgenID}, UserPartId: {response.PartID}");
                    return response;
                }
                else
                {
                    Logger.LogError($"Return failed from getParticipantInfo with error: {response.ErrorMsg}");
                    return null;
                }
            }
            catch (Exception e)
            {
                Logger.LogError(e, e.Message);
                return null;
            }
        }
    }
}