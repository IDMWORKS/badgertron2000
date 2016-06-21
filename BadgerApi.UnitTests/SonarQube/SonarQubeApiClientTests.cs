using Xunit;
using RichardSzalay.MockHttp;
using System.Net.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BadgerApi.SonarQube
{
    public class SonarQubeApiClientTests
    {
        [Fact]
        public async void ReturnProjectMetricsGivenValidArgs()
        {
            // arrange
            const string projectKey = "asdf";
            const string expected = "{'name' : 'Test McGee'}";
            const string metric = "sqale_rating";

            SonarQubeSettings settings = new SonarQubeSettings();
            settings.HostURL = "http://example.org";

            var mockHttp = new MockHttpMessageHandler();
            mockHttp.When($"{settings.HostURL}/api/resources?format=json&resource={projectKey}&metrics={metric}")
                .Respond("application/json", $"[{expected}]");

            HttpClient httpClient = new HttpClient(mockHttp);

            SonarQubeApiClient apiClient = new SonarQubeApiClient(settings, httpClient);

            // act
            var jsonObject = await apiClient.GetProjectMetrics(projectKey, metric);

            // assert
            var actual = JsonConvert.SerializeObject(jsonObject);
            
            Assert.Equal(FormatJson(expected), FormatJson(actual));
        }

        private static string FormatJson(string json)
        {
            return JObject.Parse(json).ToString();
        }
    }
}