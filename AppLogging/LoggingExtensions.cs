using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace AppLogging;

public static class LoggingExtensions
{
	public const string OutputTemplate =
		"{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}";

	public static void AddAppLogging(this IHostApplicationBuilder builder, string applicationName)
	{
		Log.Logger = new LoggerConfiguration()
			.Enrich.WithProperty("Application", applicationName)
			.WriteTo.Console(outputTemplate: OutputTemplate)
			.WriteTo.File(
				path: $"Logs/{applicationName}-.txt",
				rollingInterval: RollingInterval.Day,
				outputTemplate: OutputTemplate)
			.CreateBootstrapLogger();

		builder.Services.AddSerilog((services, configuration) => configuration
			.ReadFrom.Configuration(builder.Configuration)
			.ReadFrom.Services(services)
			.Enrich.WithProperty("Application", applicationName)
			.Enrich.FromLogContext()
			.WriteTo.Console(outputTemplate: OutputTemplate)
			.WriteTo.File(
				path: $"Logs/{applicationName}-.txt",
				rollingInterval: RollingInterval.Day,
				outputTemplate: OutputTemplate));
	}

	public static void UseAppLogging(this IApplicationBuilder app)
	{
		app.UseSerilogRequestLogging();
	}
}
