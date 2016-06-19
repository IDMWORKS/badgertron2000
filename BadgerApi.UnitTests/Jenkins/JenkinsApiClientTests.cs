using Xunit;
using RichardSzalay.MockHttp;
using System.Net.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BadgerApi.Jenkins
{
    public class JenkinsApiClientTests
    {
        [Fact]
        public async void ReturnBuildStatusGivenValidArgs()
        {
            // arrange
            const string projectName = "asdf";
            const string buildId = "qwer";
            const string expected = "{'name' : 'Test McGee'}";

            JenkinsSettings settings = new JenkinsSettings();
            settings.HostURL = "http://example.org";

            var mockHttp = new MockHttpMessageHandler();
            mockHttp.When($"{settings.HostURL}/job/{projectName}/{buildId}/api/json?depth=1")
                .Respond("application/json", expected);

            HttpClient httpClient = new HttpClient(mockHttp);

            JenkinsApiClient jenkinsApiClient = new JenkinsApiClient(settings, httpClient);

            // act
            var jsonObject = await jenkinsApiClient.GetBuildStatus(projectName, buildId);

            // assert
            var actual = JsonConvert.SerializeObject(jsonObject);
            
            Assert.Equal(FormatJson(expected), FormatJson(actual));
        }

        [Fact]
        public async void ReturnProjectStatusGivenValidArgs()
        {
            // arrange
            const string projectName = "asdf";
            const string expected = "{'name' : 'Test McGee'}";

            JenkinsSettings settings = new JenkinsSettings();
            settings.HostURL = "http://example.org";

            var mockHttp = new MockHttpMessageHandler();
            mockHttp.When($"{settings.HostURL}/job/{projectName}/api/json?depth=1")
                .Respond("application/json", expected);

            HttpClient httpClient = new HttpClient(mockHttp);

            JenkinsApiClient jenkinsApiClient = new JenkinsApiClient(settings, httpClient);

            // act
            var jsonObject = await jenkinsApiClient.GetProjectStatus(projectName);

            // assert
            var actual = JsonConvert.SerializeObject(jsonObject);
            
            Assert.Equal(FormatJson(expected), FormatJson(actual));
        }

        [Fact]
        public async void ReturnAssetGivenValidArgs()
        {
            // arrange
            const string projectName = "asdf";
            const string buildId = "qwer";
            const string asset = "zxcv";
            const string expected = "hjkl";

            JenkinsSettings settings = new JenkinsSettings();
            settings.HostURL = "http://example.org";

            var mockHttp = new MockHttpMessageHandler();
            mockHttp.When($"{settings.HostURL}/job/{projectName}/{buildId}/{asset}")
                .Respond("application/octet-stream", expected);

            HttpClient httpClient = new HttpClient(mockHttp);

            JenkinsApiClient jenkinsApiClient = new JenkinsApiClient(settings, httpClient);

            // act
            var assetContent = await jenkinsApiClient.GetAsset(projectName, buildId, asset);

            // assert
            var actual = System.Text.Encoding.UTF8.GetString(assetContent);
            
            Assert.Equal(expected, actual);
        }

        private static string FormatJson(string json)
        {
            return JObject.Parse(json).ToString();
        }
    }
}