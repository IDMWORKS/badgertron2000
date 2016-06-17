using Microsoft.Extensions.Options;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;

namespace BadgerApi.Jenkins
{
    public class JenkinsApiClient 
    {
        private JenkinsSettings settings;

        public JenkinsApiClient(JenkinsSettings settings)
        {
            this.settings = settings;
        }

        public JenkinsApiClient(IOptions<JenkinsSettings> settings)
            : this(settings.Value) { }
        
        public async Task<JenkinsBuildStatus> GetBuildStatus(string projectName, string buildId)
        {
            var url = $"http://{settings.Host}/job/{projectName}/{buildId}/api/json";
            
            var client = new HttpClient();

            SetRequestHeaders(client);

            var streamTask = client.GetStreamAsync(url);
            
            var serializer = new DataContractJsonSerializer(typeof(JenkinsBuildStatus));
            var status = serializer.ReadObject(await streamTask) as JenkinsBuildStatus;

            return status;
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
