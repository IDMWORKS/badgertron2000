using Xunit;
using RichardSzalay.MockHttp;
using System.Net.Http;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Text;
using Microsoft.AspNetCore.Http;
using Moq;

namespace BadgerApi.Jenkins
{
    public class JenkinsRCovStatsControllerTests
    {
        // Test cases

        [Fact]
        public async void ReturnRCovStatsAmongMultipleHealthReports()
        {
            // arrange

            // setup mock HTTP client
            const string projectName = "asdf";
            string expected = "{\"actions\":[{},{},{}],\"description\":\"\",\"displayName\":\"My Project\",\"displayNameOrNull\":null,\"name\":\"My Project\",\"url\":\"http://ci.example.org/job/My%20Project/\",\"buildable\":true,\"builds\":[{\"number\":124,\"url\":\"http://ci.example.org/job/My%20Project/124/\"},{\"number\":123,\"url\":\"http://ci.example.org/job/My%20Project/123/\"},{\"number\":122,\"url\":\"http://ci.example.org/job/My%20Project/122/\"},{\"number\":121,\"url\":\"http://ci.example.org/job/My%20Project/121/\"},{\"number\":120,\"url\":\"http://ci.example.org/job/My%20Project/120/\"},{\"number\":119,\"url\":\"http://ci.example.org/job/My%20Project/119/\"},{\"number\":118,\"url\":\"http://ci.example.org/job/My%20Project/118/\"},{\"number\":117,\"url\":\"http://ci.example.org/job/My%20Project/117/\"},{\"number\":116,\"url\":\"http://ci.example.org/job/My%20Project/116/\"},{\"number\":115,\"url\":\"http://ci.example.org/job/My%20Project/115/\"}],\"color\":\"blue\",\"firstBuild\":{\"number\":115,\"url\":\"http://ci.example.org/job/My%20Project/115/\"},\"healthReport\":[{\"description\":\"Rcov coverage: Code coverage 92.66%(92.66)\",\"iconClassName\":\"icon-health-80plus\",\"iconUrl\":\"health-80plus.png\",\"score\":100},{\"description\":\"Build stability: No recent builds failed.\",\"iconClassName\":\"icon-health-80plus\",\"iconUrl\":\"health-80plus.png\",\"score\":100}],\"inQueue\":false,\"keepDependencies\":false,\"lastBuild\":{\"number\":124,\"url\":\"http://ci.example.org/job/My%20Project/124/\"},\"lastCompletedBuild\":{\"number\":124,\"url\":\"http://ci.example.org/job/My%20Project/124/\"},\"lastFailedBuild\":{\"number\":119,\"url\":\"http://ci.example.org/job/My%20Project/119/\"},\"lastStableBuild\":{\"number\":124,\"url\":\"http://ci.example.org/job/My%20Project/124/\"},\"lastSuccessfulBuild\":{\"number\":124,\"url\":\"http://ci.example.org/job/My%20Project/124/\"},\"lastUnstableBuild\":null,\"lastUnsuccessfulBuild\":{\"number\":119,\"url\":\"http://ci.example.org/job/My%20Project/119/\"},\"nextBuildNumber\":125,\"property\":[{},{\"stageName\":\"CI\",\"taskName\":\"Rspec Testing\"},{\"customMessage\":\"\",\"includeCustomMessage\":false,\"includeTestSummary\":false,\"notifyAborted\":false,\"notifyBackToNormal\":false,\"notifyFailure\":false,\"notifyNotBuilt\":false,\"notifyRepeatedFailure\":false,\"notifySuccess\":false,\"notifyUnstable\":false,\"room\":\"\",\"showCommitList\":false,\"startNotification\":false,\"teamDomain\":\"\",\"token\":\"\"}],\"queueItem\":null,\"concurrentBuild\":false,\"downstreamProjects\":[{\"name\":\"My Project - Static Analysis\",\"url\":\"http://ci.example.org/job/My%20Project%20-%20Static%20Analysis/\",\"color\":\"blue\"}],\"scm\":{},\"upstreamProjects\":[]}";

            var settings = new JenkinsSettings(){ HostURL = "http://example.org" };

            var mockHttp = new MockHttpMessageHandler();
            mockHttp.When($"{settings.HostURL}/job/{projectName}/api/json?depth=0")
                .Respond("application/json", expected);

            var httpClient = new HttpClient(mockHttp);

            // setup the SUT
            var apiClient = new JenkinsApiClient(settings, httpClient);
            var logger = new LoggerFactory().CreateLogger<JenkinsRCovStatsController>();            
            var controller = new JenkinsRCovStatsController(logger, apiClient);

            // make sure the controller has a non-null Request property (used for logging)
            SetupControllerContext(controller);

            // set working directory so the test finds assets
            Directory.SetCurrentDirectory("../BadgerApi/");

            // act
            var actual = await controller.Get(projectName);

            // assert            
            Assert.IsType<FileStreamResult>(actual);
            var result = actual as FileStreamResult;
            var content = new StreamReader(result.FileStream, Encoding.UTF8).ReadToEnd();
            Assert.DoesNotMatch("<text ?.+>error<\\/text>", content);
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
    }
}