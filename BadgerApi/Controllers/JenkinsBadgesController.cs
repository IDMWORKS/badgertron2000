using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.IO;
using Microsoft.Extensions.Logging;

namespace BadgerApi
{
    [Route("/jenkins/build-status")]
    public class JenkinsBadgesController : Controller
    {
        private JenkinsSettings jenkinsSettings;
        private ILogger<JenkinsBadgesController> logger;

        public JenkinsBadgesController(
            ILogger<JenkinsBadgesController> logger,
            IOptions<JenkinsSettings> settings)
        {
            this.jenkinsSettings = settings.Value;
            this.logger = logger;
        }

        [HttpGet("{projectName}/{jobNumber}")]
        [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Get(string projectName, string jobNumber)
        {
            logger.LogInformation($"Serving badge for route {Request.Path} [{projectName} and {jobNumber}]");
            return new FileStreamResult(new FileStream("Badges/coverage-28.svg", FileMode.Open), "image/svg+xml");
        }
    }
}