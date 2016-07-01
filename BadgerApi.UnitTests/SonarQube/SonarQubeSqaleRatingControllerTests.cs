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
        [InlineData("A")]
        [InlineData("B")]
        [InlineData("C")]
        [InlineData("D")]
        [InlineData("E")]
        public async void ReturnSqaleBadgeGivenValidArgs(string sqaleRating)
        {
            // arrange

            // setup mock HTTP client
            const string projectKey = "asdf";
            string expected = "[{'id':1,'uuid':'a456b05b-0000-0000-0000-be8328cffdcd','key':'org.example:my-project:develop','name':'My Project','scope':'PRJ','qualifier':'TRK','date':'2016-06-18T05:10:08+0000','creationDate':'2015-10-01T19:35:12+0000','lname':'My Project','version':'1.0','branch':'develop','description':'','msr':[{'key':'sqale_rating','val':3.0,'frmt_val':'" + sqaleRating + "','data':'" + sqaleRating + "'}]}]";
            const string metric = "sqale_rating";

            var settings = new SonarQubeSettings(){ HostURL = "http://example.org" };

            var mockHttp = new MockHttpMessageHandler();
            mockHttp.When($"{settings.HostURL}/api/resources?format=json&resource={projectKey}&metrics={metric}")
                .Respond("application/json", expected);

            var httpClient = new HttpClient(mockHttp);

            // setup the SUT
            var apiClient = new SonarQubeApiClient(settings, httpClient);
            var logger = new LoggerFactory().CreateLogger<SonarQubeSqaleRatingController>();
            var controller = new SonarQubeSqaleRatingController(logger, apiClient);

            // make sure the controller has a non-null Request property (used for logging)
            SetupControllerContext(controller);

            // set working directory so the test finds assets
            Directory.SetCurrentDirectory("../BadgerApi/");

            // act
            var response = await controller.Get(projectKey);

            // assert
            Assert.IsType<FileStreamResult>(response);
            var content = response as FileStreamResult;
            var actual = new StreamReader(content.FileStream, Encoding.UTF8).ReadToEnd();
            Assert.Matches("<text ?.+>" + sqaleRating + "<\\/text>", actual);
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