using System.Runtime.Serialization;

namespace BadgerApi.Jenkins
{
    [DataContract]
    public class JenkinsBuildStatus
    {
        [DataMember(Name="result")]
        public string Result { get; set; }
    }
}