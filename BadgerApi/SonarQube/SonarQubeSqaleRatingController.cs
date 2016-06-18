using Microsoft.AspNetCore.Mvc;
using System.IO;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System;
using System.Dynamic;
using System.Collections.Generic;
using System.Linq;

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

            var badgeName = GetBadgeContentForSqualeRating(sqaleRating);
            
            return new FileStreamResult(new FileStream($"images/badges/{badgeName}", FileMode.Open), "image/svg+xml");
        }

        private string GetBadgeContentForSqualeRating(string sqaleRating)
        {
            var badgeName = $"sqale-error.svg";

            // no test results found in the build status
            if (!String.IsNullOrEmpty(sqaleRating))
            {
                char sqaleChar = sqaleRating.ToUpper()[0];
                char[] sqaleRatings = { 'A', 'B', 'C', 'D', 'E' };
                
                if (sqaleRatings.Contains(sqaleChar))
                {
                    badgeName = $"sqale-{sqaleRating}.svg";
                }
            }

            return badgeName;
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