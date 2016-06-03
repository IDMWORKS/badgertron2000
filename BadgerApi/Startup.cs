using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace BadgerApi
{

    /*
    Vision for routing badges:
    
    localhost/jenkins/build-stats/My%20Job/#XY/
    localhost/jenkins/junit-stats/My%20Job/#XY/
    localhost/jenkins/jacoco-stats/My%20Job/#XY/
    localhost/jenkins/rcov-stats/My%20Job/#XY/

    localhost/sonar/sqale-metric/My%20Project%20Key/#XY/
    */
    public class Startup
    {
        private IConfigurationRoot config;

        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables();

            config = builder.Build();
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddOptions();

            services.AddMvcCore()
                    .AddJsonFormatters();

            services.Configure<JenkinsSettings>(config.GetSection("JenkinsSettings"));
        }

        public void Configure(IApplicationBuilder app, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole(LogLevel.Debug);

            app.UseMvc();
        }
    }
}