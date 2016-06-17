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

        public JenkinsApiClient(JenkinsSettings settings)
        {
            this.settings = settings;
        }

        public JenkinsApiClient(IOptions<JenkinsSettings> settings)
            : this(settings.Value) { }
                
        public async Task<ExpandoObject> GetBuildStatus(string projectName, string buildId, int depth = 1)
        {
            var url = $"http://{settings.Host}/job/{projectName}/{buildId}/api/json?depth={depth}";

            return await GetApiData(url);
        }

        private async Task<ExpandoObject> GetApiData(string url)
        {
            var client = new HttpClient();

            SetRequestHeaders(client);
            
            var json = await client.GetStringAsync(url);

            // because the Jenkins REST API returns an array of disparate objects in the "actions" property,
            // and because we need to make use of those objects, we need to parse the JSON as a simple
            // list of Key-Value-Pairs
            return JsonConvert.DeserializeObject<ExpandoObject>(json, new ExpandoObjectConverter());
        }

        public async Task<byte[]> GetAsset(string projectName, string buildId, string asset)
        {
            var url = $"http://{settings.Host}/job/{projectName}/{buildId}/{asset}";
            
            var client = new HttpClient();

            SetRequestHeaders(client);

            return await client.GetByteArrayAsync(url);
        }

        private void SetRequestHeaders(HttpClient client)
        {
            client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));

            var byteArray = Encoding.ASCII.GetBytes($"{settings.Username}:{settings.Token}");
            client.DefaultRequestHeaders.Authorization = 
                new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));
        }
    }
}
