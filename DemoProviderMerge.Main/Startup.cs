using System.Text.Json;
using System.Text.Json.Serialization;

using DemoProviderMerge.Main.Cache;
using DemoProviderMerge.Main.Providers;
using DemoProviderMerge.Main.RouteComparers;
using DemoProviderMerge.Main.Services;

using TestTask;

using Route = TestTask.Route;

namespace DemoProviderMerge.Main;

public class Startup
{
    public Startup(IConfiguration configuration)
    {
        this.Configuration = configuration;
    }

    public IConfiguration Configuration { get; }

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddOptions<CacheOptions>()
            .Bind(this.Configuration.GetRequiredSection(nameof(CacheOptions)))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<ProviderOneOptions>()
            .Bind(this.Configuration.GetRequiredSection(nameof(ProviderOneOptions)))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<ProviderTwoOptions>()
            .Bind(this.Configuration.GetRequiredSection(nameof(ProviderTwoOptions)))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // Multiple DI for using both as hosted service and interface.
        services.AddSingleton<RouteCache>();
        services.AddSingleton<IRouteCache>(x => x.GetRequiredService<RouteCache>());
        services.AddHostedService<RouteCache>(x => x.GetRequiredService<RouteCache>());
        services.AddHttpClient();
        services.AddSingleton<IProviderOneClient, ProviderOneClient>();
        services.AddSingleton<IProviderTwoClient, ProviderTwoClient>();
        services.AddSingleton<IEqualityComparer<Route?>, RouteIgnoreIdComparer>();
        services.AddScoped<ISearchService, SearchService>();
        services.AddControllers().AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        });
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        app.UseStatusCodePages();

        app.UseRouting();

        app.UseEndpoints(
            endpoints =>
            {
                endpoints.MapControllers();

                if (env.IsDevelopment())
                {
                    endpoints.MapGet(
                        "/",
                        async context =>
                        {
                            await context.Response.WriteAsync("Test Task API is running!");
                        }
                    );
                }
            }
        );
    }
}