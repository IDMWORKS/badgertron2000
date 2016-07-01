using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System;
using System.IO;
using System.Collections.Generic;
using System.Dynamic;
using System.Net.Http;
using Microsoft.Extensions.Options;
using System.Linq;

namespace BadgerApi.Jenkins
{
    [Route("/jenkins/junit-stats")]
    public class JenkinsJUnitStatsController : Controller
    {
        private ILogger<JenkinsJUnitStatsController> logger;
        private JenkinsApiClient apiClient;
        private CachingSettings settings;
        private HttpClient httpClient;

        public JenkinsJUnitStatsController(
            IOptions<CachingSettings> settings,
            ILogger<JenkinsJUnitStatsController> logger,
            JenkinsApiClient apiClient,
            HttpClient httpClient)
        { 
            this.settings = settings.Value;
            this.logger = logger;
            this.apiClient = apiClient;
            this.httpClient = httpClient;
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
            var badgeName = "tests-error-blue.svg";

            // no test results found in the build status
            if (testResults != null)
            {
                string[] badgeColors = { "red", "orange", "yellow", "yellowgreen", "green", "brightgreen" };

                double ratio = (double)testResults.Item1 / (double)testResults.Item2;
                int index = Convert.ToInt32(ratio * (badgeColors.Length - 1));
                
                if ((index >= 0) && (index < badgeColors.Length))
                {
                    var badgeColor = badgeColors[index];
                    badgeName = $"tests-{testResults.Item1}%2F{testResults.Item2}-{badgeColor}.svg";
                }
            }

            var cachedContent = GetCachedBadge(badgeName);
            if (cachedContent != null)
            {
                return cachedContent;
            }

            var badgeContent = await httpClient.GetByteArrayAsync($"https://img.shields.io/badge/{badgeName}");
            AddBadgeToCache(badgeName, badgeContent);
            return badgeContent;
        }

        private byte[] GetCachedBadge(string badgeName)
        {
            // check if caching enabled
            if (!settings.CacheDynamicBadges)
            {
                return null;
            }

            // check if badge exists in cache directory
            var path = Path.Combine(settings.CacheDirectory, badgeName);
            if (System.IO.File.Exists(path))
            {
                logger.LogDebug($"Using cached badge for {badgeName}");
                return System.IO.File.ReadAllBytes(path);
            }

            // not cached
            return null;
        }

        private void AddBadgeToCache(string badgeName, byte[] badgeContent)
        {
            // check if caching enabled
            if (!settings.CacheDynamicBadges)
            {
                return;
            }

            // create badge cache dir if it doesn't exists
            if (!System.IO.Directory.Exists(settings.CacheDirectory))
            {
                System.IO.Directory.CreateDirectory(settings.CacheDirectory);
            }

            // copy badge data to cache            
            var path = Path.Combine(settings.CacheDirectory, badgeName);
            System.IO.File.WriteAllBytes(path, badgeContent);
            logger.LogDebug($"Added badge {badgeName} to cache");
        }

        private const string ActionsKey = "actions";
        private const string FailCountKey = "failCount";
        private const string SkipCountKey = "skipCount";
        private const string TotalCountKey = "totalCount";

        private Tuple<int, int> ExtractTestResultsFromBuildStatus(ExpandoObject projectStatus)
        {
            var projectKvp = projectStatus.SingleOrDefault(kvp => ActionsKey.Equals(kvp.Key, StringComparison.OrdinalIgnoreCase));
            return projectKvp.Value == null ? null : ExtractTestResultsFromActions(projectKvp);
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