// ------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------------------------------------------------------------------------------

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using CodeSnippetsReflection;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using GraphWebApi.Models;
using GraphExplorerPermissionsService.Interfaces;
using GraphExplorerPermissionsService;
using FileService.Interfaces;
using FileService.Services;
using GraphExplorerSamplesService.Interfaces;
using GraphExplorerSamplesService.Services;
using Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.QuickPulse;
using Serilog;

namespace GraphWebApi
{
    public class Startup
    {
        public Startup(IConfiguration configuration, IHostingEnvironment hostingEnvironment)
        {
            Configuration = configuration;
            _env = hostingEnvironment;
        }

        public IConfiguration Configuration { get; }

        private readonly IHostingEnvironment _env;

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddAuthentication(option =>
            {
                option.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            })
                   .AddJwtBearer(option =>
                   {
                       option.Authority = string.Format("{0}{1}", Configuration["AzureAd:Instance"], Configuration["AzureAd:TenantId"]);
                       option.TokenValidationParameters = new TokenValidationParameters
                       {
                           ValidAudience = Configuration["AzureAd:Audience"],
                           ValidIssuer = Configuration["AzureAd:Issuer"]
                       };
                   });

            services.AddHttpClient<GithubBlobStorageUtility>();
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);
            services.AddSingleton<ISnippetsGenerator, SnippetsGenerator>();
            services.AddSingleton<IFileUtility, AzureBlobStorageUtility>();
            services.AddSingleton<IPermissionsStore, PermissionsStore>();
            services.AddSingleton<ISamplesStore, SamplesStore>();
            services.Configure<SamplesAdministrators>(Configuration);

            #region AppInsights

            services.AddApplicationInsightsTelemetry(options =>
            {
               // options.InstrumentationKey = Configuration["ApplicationInsights:InstrumentationKey"];
                options.RequestCollectionOptions.InjectResponseHeaders = false;
                options.RequestCollectionOptions.TrackExceptions = false;
                options.EnableAuthenticationTrackingJavaScript = false;
                options.EnableHeartbeat = false;
                options.EnableAdaptiveSampling = false;
                options.EnableQuickPulseMetricStream = false;
                options.EnableDebugLogger = false;
            });

            if (!_env.IsDevelopment())
            {
                services.ConfigureTelemetryModule<QuickPulseTelemetryModule>((module, o) =>
                    module.AuthenticationApiKey = Configuration["ApplicationInsights:AppInsightsApiKey"]);
            }

            #endregion
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseHsts();
            }
            app.UseSerilogRequestLogging();
            app.UseStaticFiles(new StaticFileOptions
            {
                DefaultContentType = "text/plain",
                ServeUnknownFileTypes = true
            });
            app.UseHttpsRedirection();
            app.UseAuthentication();
            app.UseMvc();
        }
    }
}
