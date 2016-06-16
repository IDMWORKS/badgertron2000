using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using BadgerApi.Jenkins;
using Microsoft.AspNetCore.Diagnostics;

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

        public void Configure(IApplicationBuilder app, ILoggerFactory loggerFactory, IHostingEnvironment env)
        {
            loggerFactory.AddConsole(LogLevel.Debug);

            app.UseStatusCodePages("text/plain", "Response, status code: {0}");

            // The UseExceptionHandler and logging seems to suppress the developer exception page, so
            // don't enable both at the same time. 
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            } 
            else
            {
                UseUnhandledExceptionLogger(app, loggerFactory);
            }
            app.UseMvc();
        }

        private void UseUnhandledExceptionLogger(IApplicationBuilder app, ILoggerFactory loggerFactory)
        {
            app.UseExceptionHandler(
                builder =>
                {
                    builder.Run(
                        async context =>
                        {
                            var error = context.Features.Get<IExceptionHandlerFeature>();
                            if (error != null)
                            {
                                ILogger logger = loggerFactory.CreateLogger<Startup>();
                                logger.LogError("An error occurred", error.Error);
                            }
                        });
                });
        }
    }
}