using Azure.Core.Extensions;
using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using Goussanjarga.Services;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Goussanjarga
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
                .AddMicrosoftIdentityWebApp(Configuration.GetSection("AzureAd"));

            services.AddControllersWithViews(options =>
            {
                var policy = new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .Build();
                options.Filters.Add(new AuthorizeFilter(policy));
            });

            services.AddSingleton<ICosmosDbService>(
               InitializeCosmosClientInstanceAsync()
               .GetAwaiter()
               .GetResult()
               );

            services.AddRazorPages()
                .AddMicrosoftIdentityUI();

            services.AddAzureClients(builder =>
            {
                builder.AddBlobServiceClient(Configuration["StorageConString:blob"], preferMsi: true);
                builder.AddQueueServiceClient(Configuration["StorageConString:queue"], preferMsi: true);
            });

            services.AddApplicationInsightsTelemetry(Configuration["AppInsightConString"]);
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
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }
            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
                endpoints.MapRazorPages();
            });
        }

        private async Task<CosmosDbService> InitializeCosmosClientInstanceAsync()
        {
            // Define Azure Cosmos Db Client options like preferred operation region and Application Name
            CosmosClientOptions options = new CosmosClientOptions
            {
                ApplicationName = "GoussanMedia",
                ApplicationRegion = Regions.WestEurope
            };
            // Create the new Cosmos Db Client
            CosmosClient cosmosClient = new CosmosClient(Configuration["CosmosConString"], options);
            // Get the predefined Database name from appsettings.json
            string databaseName = Configuration["DatabaseName"];
            // Initialize the client
            CosmosDbService cosmosDbService = new CosmosDbService(cosmosClient, databaseName);
            // Check if database exists
            await cosmosDbService.CheckDatabase(databaseName);
            // Create necessary containers to store META data in
            IEnumerable<IConfiguration> containerList = Configuration.GetSection("Containers").GetChildren();
            foreach (IConfiguration item in containerList)
            {
                string containerName = item.GetSection("containerName").Value;
                string paritionKeyPath = item.GetSection("paritionKeyPath").Value;
                await cosmosDbService.CheckContainer(containerName, paritionKeyPath);
            }
            return cosmosDbService;
        }
    }

    internal static class StartupExtensions
    {
        public static IAzureClientBuilder<BlobServiceClient, BlobClientOptions> AddBlobServiceClient(this AzureClientFactoryBuilder builder, string serviceUriOrConnectionString, bool preferMsi)
        {
            if (preferMsi && Uri.TryCreate(serviceUriOrConnectionString, UriKind.Absolute, out Uri serviceUri))
            {
                return builder.AddBlobServiceClient(serviceUri);
            }
            else
            {
                return builder.AddBlobServiceClient(serviceUriOrConnectionString);
            }
        }

        public static IAzureClientBuilder<QueueServiceClient, QueueClientOptions> AddQueueServiceClient(this AzureClientFactoryBuilder builder, string serviceUriOrConnectionString, bool preferMsi)
        {
            if (preferMsi && Uri.TryCreate(serviceUriOrConnectionString, UriKind.Absolute, out Uri serviceUri))
            {
                return builder.AddQueueServiceClient(serviceUri);
            }
            else
            {
                return builder.AddQueueServiceClient(serviceUriOrConnectionString);
            }
        }
    }
}