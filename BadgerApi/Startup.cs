using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using BadgerApi.Jenkins;
using NLog.Extensions.Logging;
using BadgerApi.SonarQube;
using System.Net.Http;

namespace BadgerApi
{
    public class Startup
    {
        private IConfigurationRoot config;

        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();

            config = builder.Build();
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddOptions();

            services.AddMvcCore()
                    .AddJsonFormatters();
            
            services.Configure<JenkinsSettings>(config.GetSection("Jenkins"));
            services.Configure<SonarQubeSettings>(config.GetSection("SonarQube"));
            services.Configure<CachingSettings>(config.GetSection("Caching"));
            
            services.AddTransient<JenkinsApiClient>();
            services.AddTransient<SonarQubeApiClient>();
            services.AddTransient<HttpClient>();
        }

        public void Configure(IApplicationBuilder app, ILoggerFactory loggerFactory, IHostingEnvironment env)
        {
            // add NLog to ASP.NET Core - default logging does not log to a file
            loggerFactory.AddNLog();

            // configure logging using appsettings.json
            loggerFactory.AddConsole(config.GetSection("Logging"));

            app.UseStatusCodePages("text/plain", "Response, status code: {0}");
            
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            } 
            
            app.UseMvc();
        }
    }
}