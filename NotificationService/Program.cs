using AppLogging;
using Contracts.BindingModels.Authentication;
using MassTransit;
using NotificationService.Consumers;
using Serilog;

var builder = Host.CreateApplicationBuilder(args);
builder.AddAppLogging("NotificationService");

builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));

builder.Services.AddMassTransit(x =>
{
	x.AddConsumer<NotificationConsumer>();

	x.UsingRabbitMq((context, cfg) =>
	{
		cfg.Host(builder.Configuration["RabbitMQ:Host"] ?? "localhost", "/", h =>
		{
			h.Username(builder.Configuration["RabbitMQ:Username"] ?? "guest");
			h.Password(builder.Configuration["RabbitMQ:Password"] ?? "guest");
		});

		cfg.ConfigureEndpoints(context);
	});
});

var host = builder.Build();

try
{
	host.Run();
}
finally
{
	Log.CloseAndFlush();
}
