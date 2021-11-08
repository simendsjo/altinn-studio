using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

using Altinn.App.Domain.Clients.Interface;
using Altinn.App.Domain.Services.Interface;
using Altinn.App.PlatformServices.Extensions;
using Altinn.App.PlatformServices.Helpers;
using Altinn.App.Services.Configuration;
using Altinn.App.Services.Constants;
using Altinn.App.Services.Models;
using Altinn.Platform.Storage.Interface.Models;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace Altinn.App.PlatformServices.Clients
{
    /// <summary>
    /// A client for retrieving and writing instance data to Altinn Platform
    /// </summary>
    public class InstanceClient : IInstanceClient
    {
        private readonly ILogger _logger;
        private readonly HttpClient _client;
        private readonly JsonSerializerOptions _serializerOptions;
        private readonly ILoggedInUser _loggedInUser;

        /// <summary>
        /// Initializes a new instance of the <see cref="InstanceClient"/> class.
        /// </summary>
        /// <param name="platformSettings">the platform settings</param>
        /// <param name="logger">the logger</param>
        /// <param name="httpClient">A HttpClient that can be used to perform HTTP requests against the platform.</param>
        /// <param name="loggedInUser">The logged in user service.</param>
        public InstanceClient(
            IOptions<PlatformSettings> platformSettings,
            ILogger<InstanceClient> logger,
            HttpClient httpClient,
            ILoggedInUser loggedInUser)
        {
            _logger = logger;
            httpClient.BaseAddress = new Uri(platformSettings.Value.ApiStorageEndpoint);
            httpClient.DefaultRequestHeaders.Add(General.SubscriptionKeyHeaderName, platformSettings.Value.SubscriptionKey);
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/xml"));
            _client = httpClient;
            _loggedInUser = loggedInUser;
            _serializerOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString
            };
        }

        /// <inheritdoc/>
        public async Task<Instance> DeleteInstance(int instanceOwnerPartyId, Guid instanceGuid, bool hard)
        {
            string apiUrl = $"instances/{instanceOwnerPartyId}/{instanceGuid}?hard={hard}";
            HttpResponseMessage response = await _client.DeleteAsync(_loggedInUser.GetToken(), apiUrl);

            try
            {
                return await HandleInstanceResponse(response);
            }
            catch (PlatformHttpException)
            {
                _logger.LogError($"// InstanceClient was unsuccessful in deleting instance {instanceOwnerPartyId}/{instanceGuid}. Failed with status code {response.StatusCode}");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<Instance> GetInstance(int instanceOwnerPartyId, Guid instanceGuid)
        {
            string apiUrl = $"instances/{instanceOwnerPartyId}/{instanceGuid}";
            HttpResponseMessage response = await _client.GetAsync(_loggedInUser.GetToken(), apiUrl);

            try
            {
                return await HandleInstanceResponse(response);
            }
            catch (PlatformHttpException)
            {
                _logger.LogError($"// InstanceClient was unsuccessful in retrieving instance {instanceOwnerPartyId}/{instanceGuid}. Failed with status code {response.StatusCode}");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<List<Instance>> GetInstances(Dictionary<string, StringValues> queryParams)
        {
            StringBuilder apiUrl = new($"instances?");
            foreach (var queryParameter in queryParams)
            {
                foreach (string value in queryParameter.Value)
                {
                    apiUrl.Append($"&{queryParameter.Key}={value}");
                }
            }

            QueryResponse<Instance> queryResponse = await QueryInstances(apiUrl.ToString());

            if (queryResponse.Count == 0)
            {
                return new List<Instance>();
            }

            List<Instance> instances = new();

            instances.AddRange(queryResponse.Instances);

            while (!string.IsNullOrEmpty(queryResponse.Next))
            {
                queryResponse = await QueryInstances(queryResponse.Next);
                instances.AddRange(queryResponse.Instances);
            }

            return instances;
        }

        /// <inheritdoc/>
        public async Task<Instance> PostCompleteConfirmation(int instanceOwnerPartyId, Guid instanceGuid)
        {
            string apiUrl = $"instances/{instanceOwnerPartyId}/{instanceGuid}/complete";
            HttpResponseMessage response = await _client.PostAsync(_loggedInUser.GetToken(), apiUrl, new StringContent(string.Empty));

            try
            {
                return await HandleInstanceResponse(response);
            }
            catch (PlatformHttpException)
            {
                _logger.LogError($"// InstanceClient was unsuccessful in posting complete confirmation to instance {instanceOwnerPartyId}/{instanceGuid}. Failed with status code {response.StatusCode}");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<Instance> PostInstance(string org, string app, Instance instanceTemplate)
        {
            string apiUrl = $"instances?appId={org}/{app}";

            var content = JsonContent.Create(instanceTemplate, new MediaTypeWithQualityHeaderValue("application/json"), _serializerOptions);

            HttpResponseMessage response = await _client.PostAsync(_loggedInUser.GetToken(), apiUrl, content);

            try
            {
                return await HandleInstanceResponse(response);
            }
            catch (PlatformHttpException)
            {
                _logger.LogError($"// InstanceClient was unsuccessful in creating new instance. Failed with status code {response.StatusCode}");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<Instance> PutDataValues(int instanceOwnerPartyId, Guid instanceGuid, DataValues dataValues)
        {
            string apiUrl = $"instances/{instanceOwnerPartyId}/{instanceGuid}/datavalues";
            var content = JsonContent.Create(dataValues, new MediaTypeWithQualityHeaderValue("application/json"), _serializerOptions);

            HttpResponseMessage response = await _client.PutAsync(_loggedInUser.GetToken(), apiUrl, content);

            try
            {
                return await HandleInstanceResponse(response);
            }
            catch (PlatformHttpException)
            {
                _logger.LogError($"/ InstanceClient could not update data values for instance {instanceOwnerPartyId}/{instanceGuid}. Request failed with status code {response.StatusCode}");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<Instance> PutPresentationTexts(int instanceOwnerPartyId, Guid instanceGuid, PresentationTexts presentationTexts)
        {
            string apiUrl = $"instances/{instanceOwnerPartyId}/{instanceGuid}/presentationtexts";
            var content = JsonContent.Create(presentationTexts, new MediaTypeWithQualityHeaderValue("application/json"), _serializerOptions);

            HttpResponseMessage response = await _client.PutAsync(_loggedInUser.GetToken(), apiUrl, content);
            try
            {
                return await HandleInstanceResponse(response);
            }
            catch (PlatformHttpException)
            {
                _logger.LogError($"// InstanceClient could not update presentation texts for instance {instanceOwnerPartyId}/{instanceGuid}. Request failed with status code {response.StatusCode}");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<Instance> PutProcess(int instanceOwnerPartyId, Guid instanceGuid, ProcessState processState)
        {
            string apiUrl = $"instances/{instanceOwnerPartyId}/{instanceGuid}/process";
            var content = JsonContent.Create(processState, new MediaTypeWithQualityHeaderValue("application/json"), _serializerOptions);

            HttpResponseMessage response = await _client.PutAsync(_loggedInUser.GetToken(), apiUrl, content);

            try
            {
                return await HandleInstanceResponse(response);
            }
            catch (PlatformHttpException)
            {
                _logger.LogError($"// InstanceClient could not update process for instance {instanceOwnerPartyId}/{instanceGuid}. Request failed with status code {response.StatusCode}");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<Instance> PutReadStatus(int instanceOwnerPartyId, Guid instanceGuid, string readStatus)
        {
            string apiUrl = $"instances/{instanceOwnerPartyId}/{instanceGuid}/readstatus?status={readStatus}";

            HttpResponseMessage response = await _client.PutAsync(_loggedInUser.GetToken(), apiUrl, new StringContent(string.Empty));

            if (response.StatusCode == HttpStatusCode.OK)
            {
                string instanceData = await response.Content.ReadAsStringAsync();
                Instance instance = JsonSerializer.Deserialize<Instance>(instanceData, _serializerOptions);
                return instance;
            }

            _logger.LogError($"Could not update read status for instance {instanceOwnerPartyId}/{instanceGuid}. Request failed with status code {response.StatusCode}");
            return null;
        }

        /// <inheritdoc/>
        public async Task<Instance> PutSubstatus(int instanceOwnerPartyId, Guid instanceGuid, Substatus substatus)
        {
            string apiUrl = $"instances/{instanceOwnerPartyId}/{instanceGuid}/substatus";
            var content = JsonContent.Create(substatus, new MediaTypeWithQualityHeaderValue("application/json"), _serializerOptions);

            HttpResponseMessage response = await _client.PutAsync(_loggedInUser.GetToken(), apiUrl, content);

            try
            {
                return await HandleInstanceResponse(response);
            }
            catch (PlatformHttpException)
            {
                _logger.LogError($"// InstanceClient was unsuccessful in updating sub status for instance {instanceOwnerPartyId}/{instanceGuid}. Failed with status code {response.StatusCode}");
                throw;
            }
        }

        private async Task<QueryResponse<Instance>> QueryInstances(string url)
        {
            HttpResponseMessage response = await _client.GetAsync(_loggedInUser.GetToken(), url);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                string responseString = await response.Content.ReadAsStringAsync();
                QueryResponse<Instance> queryResponse = JsonSerializer.Deserialize<QueryResponse<Instance>>(responseString);
                return queryResponse;
            }

            _logger.LogError("InstanceClient was unsuccessful in querying instances from Platform Storage");
            throw await PlatformHttpException.CreateAsync(response);
        }

        private async Task<Instance> HandleInstanceResponse(HttpResponseMessage response)
        {
            if (response.IsSuccessStatusCode)
            {
                string instanceData = await response.Content.ReadAsStringAsync();
                Instance instance = JsonSerializer.Deserialize<Instance>(instanceData, _serializerOptions);
                return instance;
            }

            throw await PlatformHttpException.CreateAsync(response);
        }
    }
}
