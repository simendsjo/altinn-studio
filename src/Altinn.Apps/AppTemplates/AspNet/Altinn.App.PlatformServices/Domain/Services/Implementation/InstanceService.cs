using System;
using System.Collections.Generic;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;

using Altinn.App.Domain.Clients.Interface;
using Altinn.App.Domain.Domains;
using Altinn.App.Domain.Models;
using Altinn.App.Domain.Services.Interface;
using Altinn.App.PlatformServices.Helpers;
using Altinn.App.Services.Configuration;
using Altinn.Authorization.ABAC.Xacml.JsonProfile;
using Altinn.Common.AccessTokenClient.Services;
using Altinn.Common.PEP.Helpers;
using Altinn.Common.PEP.Interfaces;
using Altinn.Common.PEP.Models;
using Altinn.Platform.Storage.Interface.Models;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace Altinn.App.Domain.Services.Implementation
{
    /// <summary>
    /// Class that exposes functionality for handling instances
    /// </summary>
    public class InstanceService : IInstanceService
    {
        private readonly IInstanceClient _instanceClient;
        private readonly IEventClient _eventClient;

        private readonly ILoggedInUser _loggedInUser;
        private readonly IPDP _pdp;
        private readonly IAccessTokenGenerator _accessTokenGenerator;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<IInstanceService> _logger;
        private readonly JsonSerializerOptions _serializerOptions;
        private readonly AppSettings _settings;
        private readonly string _hostName;

        private readonly Application _application;

        /// <summary>
        /// Initializes a new instance of the <see cref="InstanceService"/> class.
        /// </summary>
        public InstanceService(
            IInstanceClient instanceClient,
            IEventClient eventClient,
            IApplicationService applicationService,
            ILoggedInUser loggedInUser,
            IPDP pdp,
            IAccessTokenGenerator accessTokenGenerator,
            IHttpContextAccessor httpContextAccessor,
            IOptionsMonitor<AppSettings> settings,
            IOptionsMonitor<GeneralSettings> generalSettings,
            ILogger<IInstanceService> logger)
        {
            _instanceClient = instanceClient;
            _eventClient = eventClient;

            _application = applicationService.GetApplication();
            _loggedInUser = loggedInUser;
            _pdp = pdp;
            _accessTokenGenerator = accessTokenGenerator;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;

            _settings = settings.CurrentValue;
            _hostName = generalSettings.CurrentValue.HostName;
            _serializerOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString
            };
        }

        ///<inheritdoc/>
        public async Task<InstanceResponse> AddCompleteConfirmation(int instanceOwnerPartyId, Guid instanceGuid)
        {
            Task<Platform.Storage.Interface.Models.Instance> task = _instanceClient.PostCompleteConfirmation(instanceOwnerPartyId, instanceGuid);
            return await HandleClientResponse(task);
        }

        ///<inheritdoc/>
        public async Task<InstanceResponse> CreateInstance(string org, string app, Platform.Storage.Interface.Models.Instance instanceTemplate)
        {
            if (_application == null)
            {
                return new InstanceResponse
                {
                    Result = new ServiceResult
                    {
                        StatusCode = HttpStatusCode.NotFound,
                        ErrorObject = $"AppId {org}/{app} was not found"
                    }
                };
            }

            Task<Platform.Storage.Interface.Models.Instance> task = _instanceClient.PostInstance(org, app, instanceTemplate);
            return await HandleClientResponse(task);
        }

        ///<inheritdoc/>
        public async Task<InstanceResponse> DeleteInstance(int instanceOwnerPartyId, Guid instanceGuid, bool hard)
        {
            Task<Platform.Storage.Interface.Models.Instance> task = _instanceClient.DeleteInstance(instanceOwnerPartyId, instanceGuid, hard);
            return await HandleClientResponse(task);
        }

        ///<inheritdoc/>
        public async Task<InstanceResponse> GetInstance(string org, string app, int instanceOwnerPartyId, Guid instanceGuid)
        {
            EnforcementResult enforcementResult = await AuthorizeAction(org, app, instanceOwnerPartyId, instanceGuid, "read");

            if (!enforcementResult.Authorized)
            {
                return Forbidden(enforcementResult);
            }

            Task<Platform.Storage.Interface.Models.Instance> task = _instanceClient.GetInstance(instanceOwnerPartyId, instanceGuid);

            InstanceResponse response = await HandleClientResponse(task);

            if (response.Result.Successful)
            {
                string userOrgClaim = _loggedInUser.GetOrg();

                if (userOrgClaim == null || !org.Equals(userOrgClaim, StringComparison.InvariantCultureIgnoreCase))
                {
                    await _instanceClient.PutReadStatus(instanceOwnerPartyId, instanceGuid, "read");
                }
            }

            return response;
        }

        /// <inheritdoc/>
        public async Task<InstanceQueryResponse> GetInstances(Dictionary<string, StringValues> queryParams)
        {
            try
            {
                List<Platform.Storage.Interface.Models.Instance> instances = await _instanceClient.GetInstances(queryParams);

                return new InstanceQueryResponse
                {
                    Instances = instances,
                    Result = new ServiceResult { Successful = true, StatusCode = HttpStatusCode.OK }
                };
            }
            catch (PlatformHttpException exception)
            {
                return new InstanceQueryResponse
                {
                    Result = new()
                    {
                        StatusCode = exception.Response.StatusCode,
                        ErrorObject = exception.Message
                    }
                };
            }
        }

        ///<inheritdoc/>
        public async Task<InstanceResponse> UpdateDataValues(int instanceOwnerPartyId, Guid instanceGuid, DataValues dataValues)
        {
            Task<Platform.Storage.Interface.Models.Instance> task = _instanceClient.PutDataValues(instanceOwnerPartyId, instanceGuid, dataValues);
            return await HandleClientResponse(task);
        }

        ///<inheritdoc/>
        public async Task<InstanceResponse> UpdatePresentationTexts(int instanceOwnerPartyId, Guid instanceGuid, PresentationTexts presentationTexts)
        {
            Task<Platform.Storage.Interface.Models.Instance> task = _instanceClient.PutPresentationTexts(instanceOwnerPartyId, instanceGuid, presentationTexts);
            return await HandleClientResponse(task);
        }

        ///<inheritdoc/>
        public async Task<InstanceResponse> UpdateProcess(int instanceOwnerPartyId, Guid instanceGuid, ProcessState process)
        {
            Task<Platform.Storage.Interface.Models.Instance> task = _instanceClient.PutProcess(instanceOwnerPartyId, instanceGuid, process);
            return await HandleClientResponse(task);
        }

        ///<inheritdoc/>
        public async Task<InstanceResponse> UpdateReadStatus(int instanceOwnerPartyId, Guid instanceGuid, string readStatus)
        {
            Task<Platform.Storage.Interface.Models.Instance> task = _instanceClient.PutReadStatus(instanceOwnerPartyId, instanceGuid, readStatus);
            return await HandleClientResponse(task);
        }

        ///<inheritdoc/>
        public async Task<InstanceResponse> UpdateSubstatus(int instanceOwnerPartyId, Guid instanceGuid, Substatus substatus)
        {
            Task<Platform.Storage.Interface.Models.Instance> task = _instanceClient.GetInstance(instanceOwnerPartyId, instanceGuid);

            InstanceResponse response = await HandleClientResponse(task);

            if (!response.Result.Successful)
            {
                return response;
            }

            Platform.Storage.Interface.Models.Instance instance = response.Instance;
            string orgClaim = _loggedInUser.GetOrg();

            if (!instance.Org.Equals(orgClaim))
            {
                return new InstanceResponse
                {
                    Result = new ServiceResult { StatusCode = HttpStatusCode.Forbidden }
                };
            }

            task = _instanceClient.PutSubstatus(instanceOwnerPartyId, instanceGuid, substatus);
            response = await HandleClientResponse(task);

            if (!response.Result.Successful)
            {
                return response;
            }

            await RegisterEvent("app.instance.substatus.changed", instance);

            return response;
        }

        private async Task<EnforcementResult> AuthorizeAction(string org, string app, int instanceOwnerPartyId, Guid? instanceGuid, string action)
        {
            EnforcementResult enforcementResult = new EnforcementResult();
            XacmlJsonRequestRoot request = DecisionHelper.CreateDecisionRequest(org, app, _loggedInUser.GetUserPrincipal(), action, instanceOwnerPartyId, instanceGuid);
            XacmlJsonResponse response = await _pdp.GetDecisionForRequest(request);

            if (response?.Response == null)
            {
                _logger.LogError(
                    $"// InstanceService failed to authorize action \"{action}\" on {instanceOwnerPartyId}/{instanceGuid}." +
                    $"Provided request: {JsonSerializer.Serialize(request, _serializerOptions)}.");
                return enforcementResult;
            }

            enforcementResult = DecisionHelper.ValidatePdpDecisionDetailed(response.Response, _loggedInUser.GetUserPrincipal());
            return enforcementResult;
        }

        private InstanceResponse Forbidden(EnforcementResult enforcementResult)
        {
            InstanceResponse response = new InstanceResponse
            {
                Result = new()
                {
                    StatusCode = HttpStatusCode.Forbidden,
                }
            };

            if (enforcementResult.FailedObligations != null && enforcementResult.FailedObligations.Count > 0)
            {
                response.Result.ErrorObject = enforcementResult;
            }

            return response;
        }

        private string GetPlatformAccessToken()
        {
            return _accessTokenGenerator.GenerateAccessToken(_application.Org, _application.Id.Split("/")[1]);
        }

        private async Task<InstanceResponse> HandleClientResponse(Task<Platform.Storage.Interface.Models.Instance> task)
        {
            try
            {
                Platform.Storage.Interface.Models.Instance instance = await task;
                SetSelfLink(instance);

                return new InstanceResponse
                {
                    Instance = instance,
                    Result = new ServiceResult { Successful = true, StatusCode = HttpStatusCode.OK }
                };
            }
            catch (PlatformHttpException exception)
            {
                return new InstanceResponse
                {
                    Result = new()
                    {
                        StatusCode = exception.Response.StatusCode,
                        ErrorObject = exception.Message
                    }
                };
            }
        }

        private async Task RegisterEvent(string eventType, Platform.Storage.Interface.Models.Instance instance)
        {
            if (_settings.RegisterEventsWithEventsComponent)
            {
                string appBaseUrl = $"https://{instance.Org}.apps.{_hostName}/{instance.AppId}";

                CloudEvent cloudEvent = EventDomain.CreateCloudEvent(eventType, appBaseUrl, instance);

                try
                {
                    await _eventClient.PostCloudEvent(GetPlatformAccessToken(), cloudEvent);
                }
                catch (Exception exception)
                {
                    _logger.LogWarning(exception, "Exception when sending event with the Events component.");
                }
            }
        }

        private void SetSelfLink(Platform.Storage.Interface.Models.Instance instance)
        {
            HttpRequest request = _httpContextAccessor.HttpContext.Request;

            // TODO: handle schema for localtest
            string host = $"https://{request.Host.ToUriComponent()}";
            string url = request.Path;

            string baseUrl = $"{host}{url}";
            Instance.SetAppSelfLinks(baseUrl, instance);
        }
    }
}
