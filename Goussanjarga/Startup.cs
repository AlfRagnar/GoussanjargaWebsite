using Azure.Core.Extensions;
using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using Goussanjarga.Models;
using Goussanjarga.Services;
using Goussanjarga.Services.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Management.Media;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Identity.Client;
using Microsoft.Identity.Web;
using Microsoft.Rest;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Net;
using System.Threading.Tasks;

namespace Goussanjarga
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
            Config.AzureStorageConnectionString = Configuration["GoussanStorage"];
            Config.AzureCosmosConnectionString = Configuration["GoussanCosmos"];
            Config.CosmosDBName = Configuration["CosmosDb:DatabaseName"];
            Config.AzureStorageBlob = Configuration["GoussanStorage:blob"];
            Config.AzureStorageQueue = Configuration["GoussanStorage:queue"];
            Config.AzureAppInsight = Configuration["AppInsightConString"];
            Config.CosmosVideos = Configuration["CosmosDb:Containers:Videos:containerName"];
            Config.AadClientId = Configuration["AadClientId"];
            Config.AadSecret = Configuration["AadSecret"];
            Config.AadTenantDomain = Configuration["AzureAd:AadTenantDomain"];
            Config.AadTenantId = Configuration["AadTenantId"];
            Config.AccountName = Configuration["AADAccountName"];
            Config.ResourceGroup = Configuration["AADResourceGroup"];
            Config.SubscriptionId = Configuration["AADSubscriptionId"];
            Config.ArmAadAudience = Configuration["AzureAd:ArmAadAudience"];
            Config.ArmEndpoint = Configuration["AzureAd:ArmEndpoint"];
            Config.AppName = "GoussanMedia";
            Config.AppRegion = Regions.WestEurope;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // Configure user consert for non-essential cookies
            services.Configure<CookiePolicyOptions>(options =>
            {
                options.CheckConsentNeeded = context => true;
                options.MinimumSameSitePolicy = SameSiteMode.Unspecified;
                options.HandleSameSiteCookieCompatibility();
            });

            services.AddControllersWithViews();

            services.AddSingleton<ICosmosDbService>(InitializeCosmosClientInstanceAsync().GetAwaiter().GetResult());
            services.AddSingleton<IBlobStorageService>(InitializeStorageClientInstance());
            services.AddSingleton<IAzMediaService>(InitializeMediaService().GetAwaiter().GetResult());

            services.AddRazorPages();

            //services.AddServerSideBlazor().AddMicrosoftIdentityConsentHandler();

            services.AddRazorPages()
            .AddMicrosoftIdentityUI();
            //services.AddSingleton<IBlobStorageService, BlobStorageService>();

            services.AddServerSideBlazor().AddMicrosoftIdentityConsentHandler();

            services.AddAzureClients(builder =>
            {
                builder.AddBlobServiceClient(Config.AzureStorageConnectionString, preferMsi: true);
                builder.AddQueueServiceClient(Config.AzureStorageQueue, preferMsi: true);
            });
            services.AddApplicationInsightsTelemetry(Config.AzureAppInsight);
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
            CosmosClientOptions options = new()
            {
                ApplicationName = Config.AppName,
                ApplicationRegion = Config.AppRegion
            };
            // Create the new Cosmos Database Client
            CosmosClient cosmosClient = new(Config.AzureCosmosConnectionString, options);
            // Get the predefined Database name from Config
            string databaseName = Config.CosmosDBName;
            // Initialize the client
            CosmosDbService cosmosDbService = new(cosmosClient, databaseName);
            // Check if database exists
            await cosmosDbService.CheckDatabase(databaseName);
            // Create necessary containers to store META data in
            IEnumerable<IConfiguration> containerList = Configuration.GetSection("CosmosDb").GetSection("Containers").GetChildren();
            foreach (var item in containerList)
            {
                string containerName = item.GetSection("containerName").Value;
                string paritionKeyPath = item.GetSection("paritionKeyPath").Value;
                await cosmosDbService.CheckContainer(containerName, paritionKeyPath);
            }
            return cosmosDbService;
        }

        private static BlobStorageService InitializeStorageClientInstance()
                {
            // Create the new Blob Service Client
            BlobServiceClient blobService = new(Config.AzureStorageConnectionString);
            // Get the predefined Container name from Config
            string container = Config.CosmosVideos;
            // Initialize the client
            BlobStorageService storageService = new(blobService, container);
            return storageService;
                }

        private static async Task<AzMediaService> InitializeMediaService()
        {
            // Create the new Azure Media Service Client
            ServiceClientCredentials serviceClientCredentials = await GetCredentialsAsync();
            AzureMediaServicesClient azureMediaServicesClient = new(serviceClientCredentials) { SubscriptionId = Config.SubscriptionId };
            // Initialize the client
            AzMediaService azMediaService = new(azureMediaServicesClient);

            // Ensure streaming endpoint is online
            _ = await azMediaService.EnsureStreamingEndpoint();

            // One time Task to ensure that I have the desired encoding available
            _ = await azMediaService.GetOrCreateTransformAsync();

            return azMediaService;
            }

        private static async Task<ServiceClientCredentials> GetCredentialsAsync()
        {
            // Use ConfidentialClientApplicationBuilder.AcquireTokenForClient to get a token using a service principal with symmetric key
            string TokenType = "Bearer";
            var scopes = new[] { Config.ArmAadAudience + "/.default" };

            var app = ConfidentialClientApplicationBuilder.Create(Config.AadClientId)
                .WithClientSecret(Config.AadSecret)
                .WithAuthority(AzureCloudInstance.AzurePublic, Config.AadTenantId)
                .Build();
            var authResult = await app.AcquireTokenForClient(scopes).ExecuteAsync().ConfigureAwait(false);

            var token = new TokenCredentials(authResult.AccessToken, TokenType);
            return token;
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