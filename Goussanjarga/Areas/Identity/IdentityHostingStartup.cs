using Goussanjarga.Areas.Identity;
using Goussanjarga.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

[assembly: HostingStartup(typeof(IdentityHostingStartup))]

namespace Goussanjarga.Areas.Identity
{
    public class IdentityHostingStartup : IHostingStartup
    {
        public void Configure(IWebHostBuilder builder)
        {
            builder.ConfigureServices((context, services) =>
            {
                services.AddDbContext<GoussanjargaContext>(options =>
                    options.UseCosmos(
                        context.Configuration.GetSection("CosmosDb").GetSection("Account").Value,
                        context.Configuration.GetSection("CosmosDb").GetSection("DatabaseName").Value));

                services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true)
                    .AddEntityFrameworkStores<GoussanjargaContext>();
            });
        }
    }
}