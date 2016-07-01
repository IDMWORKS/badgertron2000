using Xunit;
using RichardSzalay.MockHttp;
using System.Net.Http;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Moq;

namespace BadgerApi.Jenkins
{
    public class JenkinsAssetsControllerTests
    {
        // Test cases

        [Fact]
        public async void ReturnAssetGivenValidArgs()
        {
            // arrange

            // setup mock HTTP client
            const string projectName = "asdf";
            const string buildId = "qwer";
            const string asset = "hjkl.png";

            const string expected = "uiop";

            var settings = new JenkinsSettings(){ HostURL = "http://example.org" };

            var mockHttp = new MockHttpMessageHandler();
            mockHttp.When($"{settings.HostURL}/job/{projectName}/{buildId}/{asset}")
                .Respond("image/png", expected);

            var httpClient = new HttpClient(mockHttp);

            // setup the SUT
            var apiClient = new JenkinsApiClient(settings, httpClient);
            var logger = new LoggerFactory().CreateLogger<JenkinsAssetsController>();
            var controller = new JenkinsAssetsController(logger, apiClient);

            // make sure the controller has a non-null Request property (used for logging)
            SetupControllerContext(controller);

            // act
            var response = await controller.Get(projectName, buildId, asset);

            // assert
            Assert.IsType<FileContentResult>(response);
            var content = response as FileContentResult;
            var actual = System.Text.Encoding.UTF8.GetString(content.FileContents);
            Assert.Equal(expected, actual);
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