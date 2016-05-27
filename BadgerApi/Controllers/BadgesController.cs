using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace BadgerApi
{
    [Route("/badge")]
    public class BadgesController
    {
       
        private JenkinsSettings JenkinsSettings { get; set; }
        
        public BadgesController(IOptions<JenkinsSettings> settings) {
            JenkinsSettings = settings.Value;
        }

        [HttpGet("{badgeType}/{projectName}")]
        public IActionResult Get(string badgeType, string projectName)
        {
            string result = "BadgeType=" + badgeType + " ProjectName=" + projectName;
            
            result += " username= " + JenkinsSettings.Username + " token=" + JenkinsSettings.Token;

            return new OkObjectResult(result);
        }


        
        
    }
}