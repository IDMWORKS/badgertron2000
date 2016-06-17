using Microsoft.AspNetCore.Mvc;
using System.IO;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System;

namespace BadgerApi.Jenkins
{
    [Route("/jenkins/build-status")]
    public class JenkinsBuildStatusController : Controller
    {
        private ILogger<JenkinsBuildStatusController> logger;
        private JenkinsApiClient apiClient;

        public JenkinsBuildStatusController(
            ILogger<JenkinsBuildStatusController> logger,
            JenkinsApiClient apiClient)
        {
            this.logger = logger;
            this.apiClient = apiClient;
        }

        [HttpGet("{projectName}/{buildId?}")]
        [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
        public async Task<IActionResult> Get(string projectName, string buildId)
        {
            // last path segment is optional
            var actualBuildId = buildId ?? "lastBuild";

            logger.LogInformation($"Serving badge for route {Request.Path} [{projectName} and {actualBuildId}]");
            
            JenkinsBuildStatus status = await apiClient.GetBuildStatus(projectName, actualBuildId);

            var badgeName = "build-failing.svg";
            if ("success".Equals(status.Result, StringComparison.OrdinalIgnoreCase))
            {
                badgeName = "build-passing.svg";
            }
            
            return new FileStreamResult(new FileStream($"images/badges/{badgeName}", FileMode.Open), "image/svg+xml");
        }
    }
}