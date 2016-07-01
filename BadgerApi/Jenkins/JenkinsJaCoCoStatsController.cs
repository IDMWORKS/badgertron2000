using Microsoft.AspNetCore.Mvc;
using System.IO;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;

namespace BadgerApi.Jenkins
{
    [Route("/jenkins/jacoco-stats")]
    public class JenkinsJaCoCoStatsController : Controller
    {
        private ILogger<JenkinsJaCoCoStatsController> logger;
        private JenkinsApiClient apiClient;

        public JenkinsJaCoCoStatsController(
            ILogger<JenkinsJaCoCoStatsController> logger,
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
            
            var buildStatus = await apiClient.GetBuildStatus(projectName, actualBuildId, 1);

            int? coverage = ExtractCoverageFromBuildStatus(buildStatus);

            var badgeName = GetBadgeNameForCoverage(coverage);
            
            return new FileStreamResult(new FileStream($"images/badges/{badgeName}", FileMode.Open), "image/svg+xml");
        }

        private string GetBadgeNameForCoverage(int? coverage)
        {
            var badgeName = "coverage-error.svg";
            
            if (coverage.HasValue)
            {
                badgeName = $"coverage-{coverage.Value}.svg";
            }

            return badgeName;
        }

        private const string ActionsKey = "actions";
        private const string MethodCoverageKey = "methodCoverage";
        private const string PercentageKey = "percentage";

        private int? ExtractCoverageFromBuildStatus(ExpandoObject projectStatus)
        {
            var projectKvp = projectStatus.SingleOrDefault(p => ActionsKey.Equals(p.Key, StringComparison.OrdinalIgnoreCase));
            return projectKvp.Value == null ? null : ExtractCoverageFromActions(projectKvp);
        }

        private int? ExtractCoverageFromActions(KeyValuePair<string, object> actions)
        {
            int? coverage = null;

            var actionsList = (List<Object>)actions.Value;
            foreach (var action in actionsList)
            {
                var actionKvp = ((ExpandoObject)action).SingleOrDefault(a => MethodCoverageKey.Equals(a.Key, StringComparison.OrdinalIgnoreCase));
                coverage = actionKvp.Value == null ? null : ExtractCoverageFromMethodCoverageMetrics((ExpandoObject)actionKvp.Value);
                if (coverage.HasValue)
                {
                    break;
                }
            }

            return coverage;
        }

        private int? ExtractCoverageFromMethodCoverageMetrics(ExpandoObject coverageMetrics)
        {
            var coverageKvp = coverageMetrics.SingleOrDefault(cm => PercentageKey.Equals(cm.Key, StringComparison.OrdinalIgnoreCase));
            return coverageKvp.Value == null ? null : (int?)Convert.ToInt32((Int64)coverageKvp.Value);
        }
    }
}