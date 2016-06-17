using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Microsoft.AspNetCore.StaticFiles;

namespace BadgerApi.Jenkins
{
    [Route("/jenkins/assets")]
    public class JenkinsAssetsController : Controller
    {
        private ILogger<JenkinsBuildStatusController> logger;
        private JenkinsApiClient apiClient;

        public JenkinsAssetsController(
            ILogger<JenkinsBuildStatusController> logger,
            JenkinsApiClient apiClient)
        {
            this.logger = logger;
            this.apiClient = apiClient;
        }

        [HttpGet("{projectName}/{buildId}/{*asset}")]
        [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
        public async Task<IActionResult> Get(string projectName, string buildId, string asset)
        {
            logger.LogInformation($"Serving asset {asset} for route {Request.Path} [{projectName} and {buildId}]");
            
            byte[] content = await apiClient.GetAsset(projectName, buildId, asset);

            return File(content, GetContentType(asset));
        }

        private string GetContentType(string fileName, string defaultContentType = "application/octet-stream")
        {
            string contentType;
            new FileExtensionContentTypeProvider().TryGetContentType(fileName, out contentType);
            return contentType ?? defaultContentType;
        }
    }
}