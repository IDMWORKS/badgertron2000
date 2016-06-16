using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;

namespace BadgerApi.Jenkins
{
    public class JenkinsJobResolver 
    {
        private JenkinsSettings settings;
        private string projectName;
        private string jobNumber;

        public JenkinsJobResolver(JenkinsSettings settings, string projectName, string jobNumber)
        {
            this.settings = settings;
            this.projectName = projectName;
            this.jobNumber = jobNumber;
        }

        public JenkinsJobResolver(JenkinsSettings settings, string projectName) 
            : this(settings, projectName, "lastBuild") { }

        public async Task<JenkinsBuildStatus> GetBuildStatus()
        {
            var url = $"http://{settings.Host}/job/{projectName}/{jobNumber}/api/json";
            
            var client = new HttpClient();

            SetRequestHeaders(client);

            var streamTask = client.GetStreamAsync(url);
            
            var serializer = new DataContractJsonSerializer(typeof(JenkinsBuildStatus));
            var status = serializer.ReadObject(await streamTask) as JenkinsBuildStatus;

            return status;
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
