using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;

using Altinn.App.Domain.Clients.Interface;
using Altinn.App.Domain.Models;
using Altinn.App.Domain.Services.Interface;
using Altinn.App.PlatformServices.Extensions;
using Altinn.App.PlatformServices.Helpers;
using Altinn.App.Services.Configuration;
using Altinn.App.Services.Constants;
using Altinn.Common.AccessTokenClient.Services;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Altinn.App.PlatformServices.Clients
{
    /// <summary>
    /// A client for writing cloud eventsto Altinn Platform
    /// </summary>
    public class EventClient : IEventClient
    {
        private readonly ILogger _logger;
        private readonly HttpClient _client;
        private readonly JsonSerializerOptions _serializerOptions;
        private readonly ILoggedInUser _loggedInUser;
        private readonly IAccessTokenGenerator _accessTokenGenerator;

        /// <summary>
        /// Initializes a new instance of the <see cref="EventClient"/> class.
        /// </summary>
        /// <param name="platformSettings">The platform settings.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="httpClient">The http client.</param>
        /// <param name="loggedInUser">The logged in user service.</param>
        /// <param name="accessTokenGenerator">The access token generatior.</param>
        public EventClient(
            IOptions<PlatformSettings> platformSettings,
            ILogger<IEventClient> logger,
            HttpClient httpClient,
            ILoggedInUser loggedInUser,
            IAccessTokenGenerator accessTokenGenerator)

        {
            _logger = logger;

            httpClient.BaseAddress = new Uri(platformSettings.Value.ApiEventsEndpoint);
            httpClient.DefaultRequestHeaders.Add(General.SubscriptionKeyHeaderName, platformSettings.Value.SubscriptionKey);
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _client = httpClient;
            _loggedInUser = loggedInUser;
            _accessTokenGenerator = accessTokenGenerator;
        }

        /// <inheritdoc/>
        public async Task<string> PostCloudEvent(string accessToken, CloudEvent cloudEvent)
        {
            var content = JsonContent.Create(cloudEvent, new MediaTypeWithQualityHeaderValue("application/json"), _serializerOptions);

            HttpResponseMessage response = await _client.PostAsync(_loggedInUser.GetToken(), "app", content, accessToken);

            if (response.IsSuccessStatusCode)
            {
                string eventId = await response.Content.ReadAsStringAsync();
                return eventId;
            }

            _logger.LogError($"/ EventClient failed to post cloudEvent to Altinn Platform. Request failed with status code {response.StatusCode}");
            throw await PlatformHttpException.CreateAsync(response);
        }
    }
}
