using Xunit;
using RichardSzalay.MockHttp;
using System.Net.Http;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Text;
using Microsoft.AspNetCore.Http;
using Moq;

namespace BadgerApi.SonarQube
{
    public class SonarQubeSqaleRatingControllerTests
    {
        // Test cases

        [Theory]
        [InlineData("1.0", "A")]
        [InlineData("2.0", "B")]
        [InlineData("3.0", "C")]
        [InlineData("4.0", "D")]
        [InlineData("5.0", "E")]
        [InlineData("6.0", "error")]
        public async void ReturnSqaleBadgeGivenValidArgs(string sqaleRating, string expectedGrade)
        {
            // arrange

            // setup mock HTTP client
            const string projectKey = "asdf";
            string expected = "{'component':{'id':'AVolRB9SkCgaqSRFQon1','key':'org.example:my-project:develop','name':'My Project','qualifier':'TRK','measures':[{'metric':'sqale_rating','value':'" + sqaleRating + "','periods':[{'index':1,'value':'0.0'}]}]}}";
            const string metric = "sqale_rating";

            var settings = new SonarQubeSettings(){ HostURL = "http://example.org" };

            var mockHttp = new MockHttpMessageHandler();
            mockHttp.When($"{settings.HostURL}/api/measures/component?component={projectKey}&metricKeys={metric}")
                .Respond("application/json", expected);

            var httpClient = new HttpClient(mockHttp);

            // setup the SUT
            var apiClient = new SonarQubeApiClient(settings, httpClient);
            var logger = new LoggerFactory().CreateLogger<SonarQubeSqaleRatingController>();
            var controller = new SonarQubeSqaleRatingController(logger, apiClient);

            // make sure the controller has a non-null Request property (used for logging)
            SetupControllerContext(controller);

            // set working directory so the test finds assets
            Directory.SetCurrentDirectory("../netcoreapp2.0/");

            // act
            var response = await controller.Get(projectKey);

            // assert
            Assert.IsType<FileStreamResult>(response);
            var content = response as FileStreamResult;
            var actual = new StreamReader(content.FileStream, Encoding.UTF8).ReadToEnd();
            Assert.Matches("<text ?.+>" + expectedGrade + "<\\/text>", actual);
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