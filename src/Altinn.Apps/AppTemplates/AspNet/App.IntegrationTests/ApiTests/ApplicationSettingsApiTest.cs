using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Altinn.App.IntegrationTests;

using App.IntegrationTests.Utils;
using Xunit;

namespace App.IntegrationTestsRef.ApiTests
{
    public class ApplicationSettingsApiTest: IClassFixture<CustomWebApplicationFactory<Altinn.App.Startup>>
    {
        private readonly CustomWebApplicationFactory<Altinn.App.Startup> _factory;

        public ApplicationSettingsApiTest(CustomWebApplicationFactory<Altinn.App.Startup> factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task GetApplicationSettings_WithOidcProvider()
        {
            HttpClient client = SetupUtil.GetTestClient(_factory, "ttd", "model-validation");

            string requestUri = "/ttd/model-validation/api/v1/applicationsettings";
            HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, requestUri)
            {
            };

            HttpResponseMessage response = await client.SendAsync(httpRequestMessage);

            string responseContent = await response.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }
}
