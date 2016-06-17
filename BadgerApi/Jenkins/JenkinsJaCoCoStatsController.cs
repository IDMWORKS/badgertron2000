using Microsoft.AspNetCore.Mvc;
using System.IO;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Dynamic;

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
            
            var buildStatus = await apiClient.GetBuildStatus(projectName, actualBuildId, 2);

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
            int? coverage = null;

            foreach (var kvp in projectStatus)
            {
                if (ActionsKey.Equals(kvp.Key, StringComparison.OrdinalIgnoreCase))
                {
                    coverage = ExtractCoverageFromActions(kvp);
                    break;
                }
            }

            return coverage;
        }

        private int? ExtractCoverageFromActions(KeyValuePair<string, object> actions)
        {
            int? coverage = null;

            var actionsList = (List<Object>)actions.Value;
            foreach (var action in actionsList)
            {
                foreach (var element in (ExpandoObject)action)
                {
                    if (MethodCoverageKey.Equals(element.Key, StringComparison.OrdinalIgnoreCase))
                    {
                        coverage = ExtractCoverageFromMethodCoverageMetrics((ExpandoObject)element.Value);
                        break;
                    }
                }
            }

            return coverage;
        }

        private int? ExtractCoverageFromMethodCoverageMetrics(ExpandoObject coverageMetrics)
        {
            int? coverage = null;

            foreach (var coverageMetric in coverageMetrics)
            {
                if (PercentageKey.Equals(coverageMetric.Key, StringComparison.OrdinalIgnoreCase))
                {
                    coverage = Convert.ToInt32((Int64)coverageMetric.Value);
                    break;
                }
            }

            return coverage;
        }
    }
}