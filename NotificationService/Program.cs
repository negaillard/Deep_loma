using Contracts.BindingModels.Authentication;
using MassTransit;
using NotificationService.Consumers;

var builder = Host.CreateApplicationBuilder(args);

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


host.Run();
