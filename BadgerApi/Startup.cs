using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace BadgerApi
{
    
    public class Startup
    {
        private IConfigurationRoot config;
        
        public Startup(IHostingEnvironment env) {
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