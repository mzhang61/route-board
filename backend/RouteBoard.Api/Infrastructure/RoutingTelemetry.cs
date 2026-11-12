using System.Diagnostics;
using System.Diagnostics.Metrics;
using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace RouteBoard.Api.Infrastructure;

public static class RoutingTelemetry
{
    private const string ServiceName = "routeboard-api";
    private const string Category = "RouteBoard.Routing";
    private static readonly Meter Meter = new(Category);
    private static readonly Counter<long> Estimates = Meter.CreateCounter<long>("routeboard.routing.estimates");

    public static void Configure(WebApplicationBuilder builder)
    {
        builder.Services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(ServiceName))
            .WithTracing(tracing => tracing
                .AddAspNetCoreInstrumentation(options => options.Filter = context =>
                    context.Request.Path.StartsWithSegments("/api/routes"))
                .AddProcessor(new RouteTagFilter())
                .AddConsoleExporter())
            .WithMetrics(metrics => metrics.AddMeter(Category).AddConsoleExporter());

        builder.Logging.AddOpenTelemetry(options =>
        {
            options.SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(ServiceName));
            options.AddConsoleExporter();
        });
        builder.Logging.AddFilter<OpenTelemetryLoggerProvider>((category, level) =>
            category == Category && level >= LogLevel.Information);
    }

    public static void Record(string outcome, ILoggerFactory logs)
    {
        Estimates.Add(1, new KeyValuePair<string, object?>("routing.outcome", outcome));
        Activity.Current?.SetTag("routing.outcome", outcome);
        logs.CreateLogger(Category).LogInformation("Route estimate returned {Outcome}", outcome);
    }

    private sealed class RouteTagFilter : BaseProcessor<Activity>
    {
        public override void OnEnd(Activity activity)
        {
            // Retain the route template and bounded outcomes. URLs and user attributes stay out of exports.
            foreach (var tag in activity.TagObjects.ToArray())
                if (tag.Key is not ("http.route" or "http.request.method" or "http.response.status_code" or "routing.outcome"))
                    activity.SetTag(tag.Key, null);
        }
    }
}
