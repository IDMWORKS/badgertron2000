using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.IO;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System;

namespace BadgerApi.Jenkins
{
    [Route("/jenkins/build-status")]
    public class JenkinsBuildStatusController : Controller
    {
        private JenkinsSettings jenkinsSettings;
        private ILogger<JenkinsBuildStatusController> logger;

        public JenkinsBuildStatusController(
            ILogger<JenkinsBuildStatusController> logger,
            IOptions<JenkinsSettings> settings)
        {
            this.jenkinsSettings = settings.Value;
            this.logger = logger;
        }

        [HttpGet("{projectName}/{buildId?}")]
        [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
        public async Task<IActionResult> Get(string projectName, string buildId)
        {
            // last path segment is optional
            var actualBuildId = buildId ?? "lastBuild";

            logger.LogInformation($"Serving badge for route {Request.Path} [{projectName} and {actualBuildId}]");

            JenkinsJobResolver resolver = new JenkinsJobResolver(jenkinsSettings, projectName, actualBuildId);
            JenkinsBuildStatus status = await resolver.GetBuildStatus();

            var badgeName = "build-failing.svg";
            if ("success".Equals(status.Result, StringComparison.OrdinalIgnoreCase))
            {
                badgeName = "build-passing.svg";
            }
            
            return new FileStreamResult(new FileStream($"images/badges/{badgeName}", FileMode.Open), "image/svg+xml");
        }
    }
}