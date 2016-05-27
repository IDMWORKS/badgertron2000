using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.IO;

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
        public FileStreamResult Get(string badgeType, string projectName)
        {
            return new FileStreamResult(new FileStream("Badges/coverage-28.svg", FileMode.Open), "image/svg");
        }


        
        
    }
}