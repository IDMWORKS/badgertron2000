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
    public class JenkinsBuildStatusControllerTests
    {
        // Test cases

        [Theory]
        [InlineData("SUCCESS", "passing")]
        [InlineData("FAILURE", "failing")]
        [InlineData("", "running")]
        public async void ReturnBuildStatusBadgeGivenValidArgs(string successStatus, string expectedBadgeText)
        {
            // arrange

            // setup mock HTTP client
            const string projectName = "asdf";
            const string buildId = "qwer";

            string expected = "{\"actions\":[{\"causes\":[{\"shortDescription\":\"Started by user ACME Worker\",\"userId\":\"acme\",\"userName\":\"ACME Worker\"}]},{},{\"buildsByBranchName\":{\"refs/remotes/origin/develop\":{\"buildNumber\":75,\"buildResult\":null,\"marked\":{\"SHA1\":\"90e22e2f39a8ffff3488c5faaa2f1e2e801cd000\",\"branch\":[{\"SHA1\":\"90e22e2f39a8ffff3488c5faaa2f1e2e801cd000\",\"name\":\"refs/remotes/origin/develop\"}]},\"revision\":{\"SHA1\":\"90e22e2f39a8ffff3488c5faaa2f1e2e801cd000\",\"branch\":[{\"SHA1\":\"90e22e2f39a8ffff3488c5faaa2f1e2e801cd000\",\"name\":\"refs/remotes/origin/develop\"}]}}},\"lastBuiltRevision\":{\"SHA1\":\"90e22e2f39a8ffff3488c5faaa2f1e2e801cd000\",\"branch\":[{\"SHA1\":\"90e22e2f39a8ffff3488c5faaa2f1e2e801cd000\",\"name\":\"refs/remotes/origin/develop\"}]},\"remoteUrls\":[\"git@bitbucket.org:acme/myproject.git\"],\"scmName\":\"\"},{},{},{\"failCount\":0,\"skipCount\":5,\"totalCount\":292,\"urlName\":\"testReport\"},{},{},{}],\"artifacts\":[],\"building\":false,\"builtOn\":\"my_project_slave\",\"changeSet\":{\"items\":[],\"kind\":\"git\"},\"culprits\":[],\"description\":null,\"displayName\":\"#75\",\"duration\":675832,\"estimatedDuration\":666143,\"executor\":null,\"fullDisplayName\":\"My Project #75\",\"id\":\"75\",\"keepLog\":false,\"number\":75,\"queueId\":438162,\"result\":\"" + successStatus + "\",\"timestamp\":1467236801123,\"url\":\"http://ci.example.org/job/My%20Project/75/\"}";

            var settings = new JenkinsSettings(){ HostURL = "http://example.org" };

            var mockHttp = new MockHttpMessageHandler();
            mockHttp.When($"{settings.HostURL}/job/{projectName}/{buildId}/api/json?depth=0")
                .Respond("application/json", expected);

            var httpClient = new HttpClient(mockHttp);

            // setup the SUT
            var apiClient = new JenkinsApiClient(settings, httpClient);
            var logger = new LoggerFactory().CreateLogger<JenkinsBuildStatusController>();
            var controller = new JenkinsBuildStatusController(logger, apiClient);

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