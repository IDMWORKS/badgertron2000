using Microsoft.Extensions.Options;
using System;
using System.Dynamic;
using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Text;
using System.Threading.Tasks;

namespace BadgerApi.Jenkins
{
    public class JenkinsApiClient 
    {
        public const string LastBuildId = "lastBuild";

        private JenkinsSettings settings;
        private HttpClient httpClient;

        public JenkinsApiClient(JenkinsSettings settings, HttpClient httpClient)
        {
            this.settings = settings;
            this.httpClient = httpClient;
        }

        public JenkinsApiClient(IOptions<JenkinsSettings> settings, HttpClient httpClient)
            : this(settings.Value, httpClient) { }
               
        public async Task<ExpandoObject> GetBuildStatus(string projectName, string buildId, int depth = 1)
        {
            var url = $"{settings.HostURL}/job/{projectName}/{buildId}/api/json?depth={depth}";

            return await GetApiData(url);
        }

        public async Task<ExpandoObject> GetProjectStatus(string projectName, int depth = 1)
        {
            var url = $"{settings.HostURL}/job/{projectName}/api/json?depth={depth}";

            return await GetApiData(url);
        }

        private async Task<ExpandoObject> GetApiData(string url)
        {
            SetRequestHeaders(httpClient);
            
            var json = await httpClient.GetStringAsync(url);

            // because the Jenkins REST API returns an array of disparate objects in the "actions" property,
            // and because we need to make use of those objects, we need to parse the JSON as a simple
            // list of Key-Value-Pairs
            return JsonConvert.DeserializeObject<ExpandoObject>(json, new ExpandoObjectConverter());
        }

        public async Task<byte[]> GetAsset(string projectName, string buildId, string asset)
        {
            var url = $"{settings.HostURL}/job/{projectName}/{buildId}/{asset}";
            
            SetRequestHeaders(httpClient);

            return await httpClient.GetByteArrayAsync(url);
        }

        private void SetRequestHeaders(HttpClient client)
        {
            client.DefaultRequestHeaders.Clear();
            
            client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));

            var byteArray = Encoding.ASCII.GetBytes($"{settings.Username}:{settings.Token}");
            client.DefaultRequestHeaders.Authorization = 
                new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));
        }
    }
}
