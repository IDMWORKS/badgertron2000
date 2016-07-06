using Xunit;
using RichardSzalay.MockHttp;
using System.Net.Http;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Moq;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace BadgerApi.Jenkins
{
    public class JenkinsJUnitStatsControllerTests
    {
        // Test cases

        [Theory]
        [InlineData("tests", 0, 5, 292, "brightgreen")]
        [InlineData("zxcv", 5, 10, 50, "green")]
        public async void ReturnJUnitStatsBadgeWithoutCachingGivenValidArgs(string caption, int failCount, int skipCount, int totalCount, string expectedColor)
        {
            // arrange

            // setup mock HTTP client
            const string projectName = "asdf";
            const string buildId = "qwer";

            string expectedJson = "{\"actions\":[{\"causes\":[{\"shortDescription\":\"Started by user ACME Worker\",\"userId\":\"acme\",\"userName\":\"ACME Worker\"}]},{},{\"buildsByBranchName\":{\"refs/remotes/origin/develop\":{\"buildNumber\":75,\"buildResult\":null,\"marked\":{\"SHA1\":\"90e22e2f39a8ffff3488c5faaa2f1e2e801cd000\",\"branch\":[{\"SHA1\":\"90e22e2f39a8ffff3488c5faaa2f1e2e801cd000\",\"name\":\"refs/remotes/origin/develop\"}]},\"revision\":{\"SHA1\":\"90e22e2f39a8ffff3488c5faaa2f1e2e801cd000\",\"branch\":[{\"SHA1\":\"90e22e2f39a8ffff3488c5faaa2f1e2e801cd000\",\"name\":\"refs/remotes/origin/develop\"}]}}},\"lastBuiltRevision\":{\"SHA1\":\"90e22e2f39a8ffff3488c5faaa2f1e2e801cd000\",\"branch\":[{\"SHA1\":\"90e22e2f39a8ffff3488c5faaa2f1e2e801cd000\",\"name\":\"refs/remotes/origin/develop\"}]},\"remoteUrls\":[\"git@bitbucket.org:acme/myproject.git\"],\"scmName\":\"\"},{},{},{\"failCount\":" + failCount.ToString() + ",\"skipCount\":" + skipCount.ToString() + ",\"totalCount\":" + totalCount.ToString() + ",\"urlName\":\"testReport\"},{},{},{}],\"artifacts\":[],\"building\":false,\"builtOn\":\"my_project_slave\",\"changeSet\":{\"items\":[],\"kind\":\"git\"},\"culprits\":[],\"description\":null,\"displayName\":\"#75\",\"duration\":675832,\"estimatedDuration\":666143,\"executor\":null,\"fullDisplayName\":\"My Project #75\",\"id\":\"75\",\"keepLog\":false,\"number\":75,\"queueId\":438162,\"result\":\"SUCCESS\",\"timestamp\":1467236801123,\"url\":\"http://ci.example.org/job/My%20Project/75/\"}";

            var settings = new JenkinsSettings(){ HostURL = "http://example.org" };

            // setup mock HTTP handler for the API client
            var mockApiHttp = new MockHttpMessageHandler();
            mockApiHttp.When($"{settings.HostURL}/job/{projectName}/{buildId}/api/json?depth=0")
                .Respond("application/json", expectedJson);
            var apiHttpClient = new HttpClient(mockApiHttp);

            // setup mock HTTP handler for the shields.io HTTP client
            const string expectedSvg = "<svg/>";
            var mockShieldHttp = new MockHttpMessageHandler();
            var realTotal = totalCount - skipCount;
            var badgeName = $"{caption}-{realTotal - failCount}%2F{realTotal}-{expectedColor}.svg";
            mockShieldHttp.When($"https://img.shields.io/badge/{badgeName}")
                .Respond("image/svg+xml", expectedSvg);
            var shieldHttpClient = new HttpClient(mockShieldHttp);

            // setup the SUT
            var apiClient = new JenkinsApiClient(settings, apiHttpClient);
            var logger = new LoggerFactory().CreateLogger<JenkinsJUnitStatsController>();
            var cachingSettings = CreateCachingSettings(false);

            var controller = new JenkinsJUnitStatsController(cachingSettings, logger, apiClient, shieldHttpClient);

            // make sure the controller has a non-null Request property (used for logging)
            SetupControllerContext(controller);

            // act
            var response = await controller.Get(projectName, buildId, caption);

            // assert
            Assert.IsType<FileContentResult>(response);
            var content = response as FileContentResult;
            var actual = System.Text.Encoding.UTF8.GetString(content.FileContents);
            Assert.Equal(expectedSvg, actual);
        }

        // Utility methods
        private void SetupControllerContext(Controller controller)
        {
            controller.ControllerContext = new ControllerContext();

            var httpContext = new Mock<HttpContext>();
            var httpRequest = new Mock<HttpRequest>();
            httpRequest.Object.Path = "/example";
            httpContext.SetupGet(r => r.Request).Returns(httpRequest.Object);

            controller.ControllerContext.HttpContext = httpContext.Object;
        }

        // see http://dotnetliberty.com/index.php/2015/11/30/learning-asp-net-5-through-experimentation-and-unit-testing/
        private IOptions<CachingSettings> CreateCachingSettings(bool cacheDynamicBadges)
        {
            const string configSection = "Caching";
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(
                    new Dictionary<string, string>
                    {
                        {$"{configSection}:CacheDynamicBadges", cacheDynamicBadges.ToString()}
                    })
                .Build();
 
            var services = new ServiceCollection();
            services.Configure<CachingSettings>(configuration.GetSection(configSection));
            services.AddOptions();
            var serviceProvider = services.BuildServiceProvider();

            return serviceProvider.GetService<IOptions<CachingSettings>>();
        }
    }
}