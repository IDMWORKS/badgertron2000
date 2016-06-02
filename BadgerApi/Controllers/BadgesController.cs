using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.IO;
using Microsoft.Extensions.Logging;

namespace BadgerApi
{
    [Route("/badge")]
    public class BadgesController
    {
        private JenkinsSettings jenkinsSettings;
        private ILogger<BadgesController> logger; 
        
        public BadgesController(
            ILogger<BadgesController> logger, 
            IOptions<JenkinsSettings> settings) {
            this.jenkinsSettings = settings.Value;
            this.logger = logger;
        }

        [HttpGet("{badgeType}/{projectName}")]
        public IActionResult Get(string badgeType, string projectName)
        {
            return new FileStreamResult(new FileStream("Badges/coverage-28.svg", FileMode.Open), "image/svg+xml");
        }
    }
}