using Microsoft.AspNetCore.Mvc;
using System.IO;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System;
using System.Dynamic;
using System.Net.Http;
using System.Collections.Generic;

namespace BadgerApi.SonarQube
{
    [Route("/sonarqube/sqale-rating")]
    public class SonarQubeSqaleRatingController : Controller
    {
        private ILogger<SonarQubeSqaleRatingController> logger;
        private SonarQubeApiClient apiClient;

        public SonarQubeSqaleRatingController(
            ILogger<SonarQubeSqaleRatingController> logger,
            SonarQubeApiClient apiClient)
        {
            this.logger = logger;
            this.apiClient = apiClient;
        }

        [HttpGet("{projectKey}")]
        [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
        public async Task<IActionResult> Get(string projectKey)
        {
            logger.LogInformation($"Serving badge for route {Request.Path} [{projectKey}]");
            
            var projectMetrics = await apiClient.GetProjectMetrics(projectKey, "sqale_rating");

            string sqaleRating = ExtractSqaleFromMetrics(projectMetrics);

            byte[] content = await GetBadgeContentForTestResults(sqaleRating);

            return File(content, "image/svg+xml");
        }

        private async Task<byte[]> GetBadgeContentForTestResults(string sqaleRating)
        {
            var badgeUrl = $"https://img.shields.io/badge/sqale-error-blue.svg";

            // no test results found in the build status
            if (!String.IsNullOrEmpty(sqaleRating))
            {
                string[] badgeColors = { "green", "yellowgreen", "yellow", "orange", "red" };

                char sqaleChar = sqaleRating.ToUpper()[0];
                int offset = (int)'A';
                int index = (int)sqaleChar - offset;
                
                if ((index >= 0) && (index < badgeColors.Length))
                {
                    var badgeColor = badgeColors[index];
                    badgeUrl = $"https://img.shields.io/badge/sqale-{sqaleRating}-{badgeColor}.svg";
                }
            }

            var client = new HttpClient();
            return await client.GetByteArrayAsync(badgeUrl);
        }

        private const string MeauresKey = "msr";
        private const string KeyKey = "key";
        private const string DataKey = "data";
        private const string SqaleRatingValue = "sqale_rating";

        private string ExtractSqaleFromMetrics(ExpandoObject projectMetrics)
        {
            string sqaleRating = null;

            foreach (var kvp in projectMetrics)
            {
                if (MeauresKey.Equals(kvp.Key, StringComparison.OrdinalIgnoreCase))
                {
                    sqaleRating = ExtractSqaleFromMeasures(kvp);
                    break;
                }
            }

            return sqaleRating;
        }

        private string ExtractSqaleFromMeasures(KeyValuePair<string, object> projectMeasures)
        {
            string sqaleRating = null;

            var measuresList = (List<Object>)projectMeasures.Value;
            foreach (ExpandoObject measure in measuresList)
            {
                sqaleRating = ExtractSqaleFromMeasure(measure);

                if (sqaleRating != null)
                {
                    break;
                }
            }

            return sqaleRating;
        }

        private string ExtractSqaleFromMeasure(ExpandoObject projectMeasure)
        {
            string sqaleRating = null;

            bool isSqale = false;
            foreach (var measureValue in projectMeasure)
            {
                if (KeyKey.Equals(measureValue.Key, StringComparison.OrdinalIgnoreCase))
                {
                    if (SqaleRatingValue.Equals((string)measureValue.Value, StringComparison.OrdinalIgnoreCase))
                    {
                        isSqale = true;
                    } 
                }
                else if (isSqale && DataKey.Equals((string)measureValue.Key, StringComparison.OrdinalIgnoreCase))
                {
                    sqaleRating = (string)measureValue.Value;
                    break;
                }
            }

            return sqaleRating;
        }
    }
}