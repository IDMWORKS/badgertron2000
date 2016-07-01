using Microsoft.AspNetCore.Mvc;
using System.IO;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System;
using System.Linq;
using System.Dynamic;

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
            var actualBuildId = buildId ?? JenkinsApiClient.LastBuildId;

            logger.LogInformation($"Serving badge for route {Request.Path} [{projectName} and {actualBuildId}]");
            
            var buildStatus = await apiClient.GetBuildStatus(projectName, actualBuildId);

            string result = ExtractResultFromBuildStatus(buildStatus);

            var badgeName = GetBadgeNameForBuildResult(result);
            
            return new FileStreamResult(new FileStream($"images/badges/{badgeName}", FileMode.Open), "image/svg+xml");
        }

        private const string ResultKey = "result";
        private const string SuccessValue = "success";

        private string GetBadgeNameForBuildResult(string buildResult)
        {
            var badgeName = "build-failing.svg";
            
            if (SuccessValue.Equals(buildResult, StringComparison.OrdinalIgnoreCase))
            {
                badgeName = "build-passing.svg";
            }
            else if (String.IsNullOrEmpty(buildResult))
            {
                badgeName = "build-running.svg";
            }

            return badgeName;
        }

        private string ExtractResultFromBuildStatus(ExpandoObject buildStatus)
        {
            var buildKvp = buildStatus.SingleOrDefault(bs => ResultKey.Equals(bs.Key, StringComparison.OrdinalIgnoreCase));
            return (string)buildKvp.Value;
        }
    }
}