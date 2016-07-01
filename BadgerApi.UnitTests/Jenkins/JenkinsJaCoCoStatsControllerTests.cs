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
    public class JenkinsJaCoCoStatsControllerTests
    {
        // Test cases

        [Theory]
        [InlineData("10", "10%")]
        [InlineData("100", "100%")]
        public async void ReturnJaCoCoStatsBadgeGivenValidArgs(string percentageValue, string expectedBadgeText)
        {
            // arrange

            // setup mock HTTP client
            const string projectName = "asdf";
            const string buildId = "qwer";

            string expected = "{\"actions\":[{\"causes\":[{\"shortDescription\":\"Started by user ACME Worker\",\"userId\":\"acme\",\"userName\":\"ACME Worker\"}]},{},{\"buildsByBranchName\":{\"refs/remotes/origin/develop\":{\"buildNumber\":75,\"buildResult\":null,\"marked\":{\"SHA1\":\"aaa22e2f39a8bbb83488c5f74eee1e2e801cffff\",\"branch\":[{\"SHA1\":\"aaa22e2f39a8bbb83488c5f74eee1e2e801cffff\",\"name\":\"refs/remotes/origin/develop\"}]},\"revision\":{\"SHA1\":\"aaa22e2f39a8bbb83488c5f74eee1e2e801cffff\",\"branch\":[{\"SHA1\":\"aaa22e2f39a8bbb83488c5f74eee1e2e801cffff\",\"name\":\"refs/remotes/origin/develop\"}]}}},\"lastBuiltRevision\":{\"SHA1\":\"aaa22e2f39a8bbb83488c5f74eee1e2e801cffff\",\"branch\":[{\"SHA1\":\"aaa22e2f39a8bbb83488c5f74eee1e2e801cffff\",\"name\":\"refs/remotes/origin/develop\"}]},\"remoteUrls\":[\"git@bitbucket.org:acme/myproject.git\"],\"scmName\":\"\"},{\"tags\":[]},{},{\"failCount\":0,\"skipCount\":5,\"totalCount\":292,\"urlName\":\"testReport\"},{\"branchCoverage\":{\"covered\":3109,\"missed\":72598,\"percentage\":4,\"percentageFloat\":4.1066213,\"total\":75707},\"classCoverage\":{\"covered\":393,\"missed\":896,\"percentage\":30,\"percentageFloat\":30.48875,\"total\":1289},\"complexityScore\":{\"covered\":2447,\"missed\":46340,\"percentage\":5,\"percentageFloat\":5.015681,\"total\":48787},\"instructionCoverage\":{\"covered\":83384,\"missed\":705189,\"percentage\":11,\"percentageFloat\":10.574037,\"total\":788573},\"lineCoverage\":{\"covered\":14409,\"missed\":137728,\"percentage\":9,\"percentageFloat\":9.471068,\"total\":152137},\"methodCoverage\":{\"covered\":1763,\"missed\":8913,\"percentage\":" + percentageValue + ",\"percentageFloat\":16.513676,\"total\":10676},\"previousResult\":{}},{},{}],\"artifacts\":[],\"building\":false,\"builtOn\":\"forge_6_slave\",\"changeSet\":{\"items\":[],\"kind\":\"git\"},\"culprits\":[],\"description\":null,\"displayName\":\"#75\",\"duration\":675832,\"estimatedDuration\":666143,\"executor\":null,\"fullDisplayName\":\"My Project #75\",\"id\":\"75\",\"keepLog\":false,\"number\":75,\"queueId\":438162,\"result\":\"SUCCESS\",\"timestamp\":1467236801123,\"url\":\"http://ci.dev.admin.net/job/My%20Project/75/\"}";

            var settings = new JenkinsSettings(){ HostURL = "http://example.org" };

            var mockHttp = new MockHttpMessageHandler();
            mockHttp.When($"{settings.HostURL}/job/{projectName}/{buildId}/api/json?depth=1")
                .Respond("application/json", expected);

            var httpClient = new HttpClient(mockHttp);

            // setup the SUT
            var apiClient = new JenkinsApiClient(settings, httpClient);
            var logger = new LoggerFactory().CreateLogger<JenkinsJaCoCoStatsController>();
            var controller = new JenkinsJaCoCoStatsController(logger, apiClient);

            // make sure the controller has a non-null Request property (used for logging)
            SetupControllerContext(controller);

            // set working directory so the test finds assets
            Directory.SetCurrentDirectory("../BadgerApi/");

            // act
            var response = await controller.Get(projectName, buildId);

            // assert
            Assert.IsType<FileStreamResult>(response);
            var content = response as FileStreamResult;
            var actual = new StreamReader(content.FileStream, Encoding.UTF8).ReadToEnd();
            Assert.Matches("<text ?.+>" + expectedBadgeText + "<\\/text>", actual);
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