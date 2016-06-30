using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Net.Http;

namespace BadgerApi.Jenkins
{
    [Route("/jenkins/junit-stats")]
    public class JenkinsJUnitStatsController : Controller
    {
        private ILogger<JenkinsJUnitStatsController> logger;
        private JenkinsApiClient apiClient;

        public JenkinsJUnitStatsController(
            ILogger<JenkinsJUnitStatsController> logger,
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

            Tuple<int, int> testResults = ExtractTestResultsFromBuildStatus(buildStatus);

            byte[] content = await GetBadgeContentForTestResults(testResults);

            return File(content, "image/svg+xml");
        }

        private async Task<byte[]> GetBadgeContentForTestResults(Tuple<int, int> testResults)
        {
            var badgeUrl = $"https://img.shields.io/badge/tests-error-blue.svg";

            // no test results found in the build status
            if (testResults != null)
            {
                string[] badgeColors = { "red", "orange", "yellow", "yellowgreen", "green", "brightgreen" };

                double ratio = (double)testResults.Item1 / (double)testResults.Item2;
                int index = Convert.ToInt32(ratio * (badgeColors.Length - 1));
                
                if ((index >= 0) && (index < badgeColors.Length))
                {
                    var badgeColor = badgeColors[index];
                    badgeUrl = $"https://img.shields.io/badge/tests-{testResults.Item1}%2F{testResults.Item2}-{badgeColor}.svg";
                }
            }

            var client = new HttpClient();
            return await client.GetByteArrayAsync(badgeUrl);
        }

        private const string ActionsKey = "actions";
        private const string FailCountKey = "failCount";
        private const string SkipCountKey = "skipCount";
        private const string TotalCountKey = "totalCount";

        private Tuple<int, int> ExtractTestResultsFromBuildStatus(ExpandoObject projectStatus)
        {
            Tuple<int, int> testResults = null;

            foreach (var kvp in projectStatus)
            {
                if (ActionsKey.Equals(kvp.Key, StringComparison.OrdinalIgnoreCase))
                {
                    testResults = ExtractTestResultsFromActions(kvp);
                    break;
                }
            }

            return testResults;
        }

        private Tuple<int, int> ExtractTestResultsFromActions(KeyValuePair<string, object> actions)
        {
            Tuple<int, int> testResults = null;

            var actionsList = (List<Object>)actions.Value;
            foreach (var action in actionsList)
            {
                var failCount = 0;
                var skipCount = 0;
                var totalCount = 0;

                foreach (var element in (ExpandoObject)action)
                {
                    if (FailCountKey.Equals(element.Key, StringComparison.OrdinalIgnoreCase))
                    {
                        failCount = Convert.ToInt32((Int64)element.Value);
                    }
                    else if (SkipCountKey.Equals(element.Key, StringComparison.OrdinalIgnoreCase))
                    {
                        skipCount = Convert.ToInt32((Int64)element.Value);
                    }
                    else if (TotalCountKey.Equals(element.Key, StringComparison.OrdinalIgnoreCase))
                    {
                        totalCount = Convert.ToInt32((Int64)element.Value);
                    }
                }

                if (totalCount > 0)
                {
                    var totalRan = totalCount - skipCount;
                    var passing = totalRan - failCount;
                    testResults = new Tuple<int, int>(passing, totalRan);
                }
            }

            return testResults;
        }
    }
}