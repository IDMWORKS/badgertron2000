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

        private static Dictionary<string, string> SqaleGradeMap = new Dictionary<string, string>
        {
            { "1.0", "A" },
            { "2.0", "B" },
            { "3.0", "C" },
            { "4.0", "D" },
            { "5.0", "E" }
        };

        private string GetBadgeContentForSqualeRating(string sqaleRating)
        {
            var badgeSuffix = "error";

            if (!String.IsNullOrEmpty(sqaleRating))
            {
                // sqale is an 'A-to-E rating based on the technical debt ratio'
                if (SqaleGradeMap.ContainsKey(sqaleRating))
                {
                    badgeSuffix = SqaleGradeMap[sqaleRating];
                }
                else
                {
                    logger.LogWarning($"Unknown value '{sqaleRating}' for SQALE rating");
                }
            }

            return $"sqale-{badgeSuffix}.svg";
        }

        private const string MetricKey = "metric";
        private const string ValueKey = "value";
        private const string MeasuresKey = "measures";
        private const string SqaleRatingValue = "sqale_rating";

        private string ExtractSqaleFromMetrics(ExpandoObject projectMetrics)
        {
            var projectKvp = projectMetrics.SingleOrDefault();
            return projectKvp.Value == null ? null : ExtractSqaleFromMeasures((ExpandoObject)projectKvp.Value);
        }

        private string ExtractSqaleFromMeasures(ExpandoObject componentDetails)
        {
            string sqaleRating = null;

            var componentMeasures = componentDetails.SingleOrDefault(cd => MeasuresKey.Equals(cd.Key, StringComparison.OrdinalIgnoreCase));
            if (componentMeasures.Value == null)
            {
                logger.LogWarning($"Key '{MeasuresKey}' not found in SonarQube Web API response");
            }
            else
            {
                var measuresList = (List<Object>)componentMeasures.Value;
                foreach (ExpandoObject measure in measuresList)
                {
                    sqaleRating = ExtractSqaleFromMeasure(measure);
                    if (sqaleRating != null)
                    {
                        break;
                    }
                }
                if (sqaleRating == null)
                {
                    logger.LogWarning($"Value '{SqaleRatingValue}' not found for key '{MetricKey}' in SonarQube Web API response");
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
                if (MetricKey.Equals(measureValue.Key, StringComparison.OrdinalIgnoreCase))
                {
                    if (SqaleRatingValue.Equals((string)measureValue.Value, StringComparison.OrdinalIgnoreCase))
                    {
                        isSqale = true;
                    }
                }
                else if (isSqale && ValueKey.Equals((string)measureValue.Key, StringComparison.OrdinalIgnoreCase))
                {
                    sqaleRating = (string)measureValue.Value;
                    break;
                }
            }

            return sqaleRating;
        }
    }
}