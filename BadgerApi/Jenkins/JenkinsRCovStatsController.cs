using Microsoft.AspNetCore.Mvc;
using System.IO;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Text.RegularExpressions;

namespace BadgerApi.Jenkins
{
    [Route("/jenkins/rcov-stats")]
    public class JenkinsRCovStatsController : Controller
    {
        private ILogger<JenkinsRCovStatsController> logger;
        private JenkinsApiClient apiClient;

        public JenkinsRCovStatsController(
            ILogger<JenkinsRCovStatsController> logger,
            JenkinsApiClient apiClient)
        {
            this.logger = logger;
            this.apiClient = apiClient;
        }

        [HttpGet("{projectName}")]
        [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
        public async Task<IActionResult> Get(string projectName)
        {
            logger.LogInformation($"Serving badge for route {Request.Path} [{projectName}]");
            
            var projectStatus = await apiClient.GetProjectStatus(projectName);

            int? coverage = ExtractCoverageFromProjectStatus(projectStatus);

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

        private const string RCovRegexPattern = @"Code coverage \d{1,3}\.\d{2}%\(([^\)]+)\)";
        private const string HealthReportKey = "healthReport";
        private const string DescriptionKey = "description";
        private const string RCovMetricPrefix = "Rcov coverage";

        private int? ExtractCoverageFromProjectStatus(ExpandoObject projectStatus)
        {
            int? coverage = null;

            foreach (var kvp in projectStatus)
            {
                if (HealthReportKey.Equals(kvp.Key, StringComparison.OrdinalIgnoreCase))
                {
                    coverage = ExtractCoverageFromHealthReport(kvp);
                    break;
                }
            }

            return coverage;
        }

        private int? ExtractCoverageFromHealthReport(KeyValuePair<string, object> healthReport)
        {
            int? coverage = null;

            var reports = (List<Object>)healthReport.Value;
            foreach (var report in reports)
            {
                foreach (var element in (ExpandoObject)report)
                {
                    if (DescriptionKey.Equals(element.Key, StringComparison.OrdinalIgnoreCase))
                    {
                        coverage = ExtractCoverageFromHealthReportDescription((string)element.Value);
                        if (coverage.HasValue)
                        {
                            break;
                        }
                    }
                }
                if (coverage.HasValue)
                {
                    break;
                }
            }

            return coverage;
        }

        private int? ExtractCoverageFromHealthReportDescription(string description)
        {
            int? coverage = null;

            if (description.StartsWith(RCovMetricPrefix, StringComparison.OrdinalIgnoreCase))
            {
                string groupValue = ExtractMatchedGroup(description, RCovRegexPattern, RegexOptions.IgnoreCase);

                if (!String.IsNullOrEmpty(groupValue))
                {
                    coverage = Convert.ToInt32(Decimal.Parse(groupValue));
                }
            }

            return coverage;
        }

        private string ExtractMatchedGroup(string content, string pattern, RegexOptions options)
        {
            string groupValue = null;

            Match match = Regex.Match(content, pattern, options);
            if (match.Success)
            {
                groupValue = match.Groups[1].Value;
            }

            return groupValue;
        }
    }
}