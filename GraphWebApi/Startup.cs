// ------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------------------------------------------------------------------------------

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using FileService.Interfaces;
using FileService.Services;
using Serilog;
using Microsoft.Extensions.Hosting;
using Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.QuickPulse;
using System.Globalization;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Options;
using TelemetrySanitizerService;
using OpenAPIService.Interfaces;
using OpenAPIService;
using KnownIssuesService.Interfaces;
using GraphWebApi.Middleware;
using CodeSnippetsReflection.OData;
using CodeSnippetsReflection.OpenAPI;
using PermissionsService.Interfaces;
using SamplesService.Interfaces;
using PermissionsService;
using SamplesService.Services;
using System.Text.Json;

namespace GraphWebApi
{
    public class Startup(IConfiguration configuration, IWebHostEnvironment hostingEnvironment)
    {
        public IConfiguration Configuration { get; } = configuration;

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            #region AppInsights

            services.AddApplicationInsightsTelemetry(options =>
            {
                options.InstrumentationKey = Configuration["ApplicationInsights:InstrumentationKey"];
                options.RequestCollectionOptions.InjectResponseHeaders = true;
                options.RequestCollectionOptions.TrackExceptions = true;
                options.EnableAuthenticationTrackingJavaScript = false;
                options.EnableHeartbeat = true;
                options.EnableAdaptiveSampling = true;    // Control volume of telemetry sent to AppInsights
                options.EnableQuickPulseMetricStream = true;   // Enable Live Metrics stream
                options.EnableDebugLogger = true;

            });
            services.AddApplicationInsightsTelemetryProcessor<CustomPIIFilter>();

            if (!hostingEnvironment.IsDevelopment())
            {
                services.ConfigureTelemetryModule<QuickPulseTelemetryModule>((module, o) =>
                    module.AuthenticationApiKey = Configuration["ApplicationInsights:AppInsightsApiKey"]);
            }

            #endregion

            services.AddMemoryCache();
            services.AddSingleton<IODataSnippetsGenerator, ODataSnippetsGenerator>();
            services.AddSingleton<IOpenApiSnippetsGenerator, OpenApiSnippetsGenerator>();
            services.AddSingleton<IFileUtility, AzureBlobStorageUtility>();
            services.AddSingleton<IPermissionsStore, PermissionsStore>();
            services.AddSingleton<ISamplesStore, SamplesStore>();
            services.AddSingleton<IOpenApiService, OpenApiService>();
            services.AddSingleton<IKnownIssuesService, KnownIssuesService.Services.KnownIssuesService>();
            services.AddHttpClient<IHttpClientUtility, HttpClientUtility>();
            services.AddControllers().AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.WriteIndented = true;
                options.JsonSerializerOptions.AllowTrailingCommas = true;
                options.JsonSerializerOptions.ReadCommentHandling = JsonCommentHandling.Skip;
            });

            // Localization
            services.Configure<RequestLocalizationOptions>(options =>
            {
                var supportedCultures = new[]
                {
                    new CultureInfo("en"),
                    new CultureInfo("de"),
                    new CultureInfo("fr"),
                    new CultureInfo("es"),
                    new CultureInfo("ru"),
                    new CultureInfo("ja"),
                    new CultureInfo("pt"),
                    new CultureInfo("zh")
                };
                options.DefaultRequestCulture = new RequestCulture("en");
                options.SupportedCultures = supportedCultures;
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseHsts();
            }
            app.UseGlobalExceptionMiddleware();
            app.UseStaticFiles(new StaticFileOptions
            {
                DefaultContentType = "text/plain",
                ServeUnknownFileTypes = true
            });
            app.UseSerilogRequestLogging();
            app.UseHttpsRedirection();
            app.UseRouting();

            // Localization
            var localizationOptions = app.ApplicationServices.GetService<IOptions<RequestLocalizationOptions>>().Value;
            app.UseRequestLocalization(localizationOptions);

            app.ApplicationServices.GetRequiredService<IOpenApiService>();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
