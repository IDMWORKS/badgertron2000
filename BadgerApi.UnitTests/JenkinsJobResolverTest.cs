using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using BadgerApi;

namespace BadgerApi.UnitTests.Services
{
    public class JenkinsJobResolverTest
    {
         private readonly JenkinsJobResolver _jenkinsJobResolver;

         public JenkinsJobResolverTest()
         {
             _jenkinsJobResolver = new JenkinsJobResolver();
         }

         [Theory]
         [InlineData("Project1", "123")]
         [InlineData("Project2", "456")]
         [InlineData("blahblah", "789")]
         public void ReturnValidBuildStatus(String projectName, String jobNumber)
         {
             var status = _jenkinsJobResolver.GetBuildStatus(projectName, jobNumber);
             Assert.NotNull(status);
         }
    }
}  