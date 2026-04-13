using Serilog;
using Serilog.Sinks.OpenTelemetry;

namespace SmartInventory.Inventories.WebApi.Extensions;

public static class HostExtensions
{
    public static IHostBuilder AddSerilogLogging(this IHostBuilder hostBuilder)
    {
        return hostBuilder.UseSerilog((context, configBuilder) =>
        {
            configBuilder.ReadFrom.Configuration(context.Configuration);

            configBuilder.WriteTo.OpenTelemetry(options =>
            {
                options.Endpoint = context.Configuration["OpenTelemetry:OtlpEndpoint"];
                options.Protocol = OtlpProtocol.HttpProtobuf;

                options.ResourceAttributes = new Dictionary<string, object>
                {
                    ["service.name"] = "InventoryService",
                    ["deployment.environment"] = context.HostingEnvironment.EnvironmentName
                };
            });
        });
    }
}
