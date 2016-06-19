using Microsoft.Extensions.Options;
using System;
using System.Dynamic;
using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace BadgerApi.SonarQube
{
    public class SonarQubeApiClient 
    {
        private SonarQubeSettings settings;
        private HttpClient httpClient;

        public SonarQubeApiClient(SonarQubeSettings settings, HttpClient httpClient)
        {
            this.settings = settings;
            this.httpClient = httpClient;
        }

        public SonarQubeApiClient(IOptions<SonarQubeSettings> settings, HttpClient httpClient)
            : this(settings.Value, httpClient) { }

        public async Task<ExpandoObject> GetProjectMetrics(string projectKey, params string[] metrics)
        {
            var url = $"{settings.HostURL}/api/resources?format=json&resource={projectKey}&metrics={String.Join(",", metrics)}";

            return await GetApiData(url);
        }

        private async Task<ExpandoObject> GetApiData(string url)
        {
            SetRequestHeaders(httpClient);
            
            var json = await httpClient.GetStringAsync(url);

            // because the SonarQube REST API returns an array of disparate objects in the "actions" property,
            // and because we need to make use of those objects, we need to parse the JSON as a simple
            // list of Key-Value-Pairs
            return JsonConvert.DeserializeObject<List<ExpandoObject>>(json, new ExpandoObjectConverter()).FirstOrDefault();
        }

        private void SetRequestHeaders(HttpClient client)
        {
            client.DefaultRequestHeaders.Clear();
            
            client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));

            var byteArray = Encoding.ASCII.GetBytes($"{settings.Username}:{settings.Password}");
            client.DefaultRequestHeaders.Authorization = 
                new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));
        }
    }
}
