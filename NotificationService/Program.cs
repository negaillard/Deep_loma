using Contracts.BindingModels.Authentication;
using Contracts.StorageContracts;
using MassTransit;
using NotificationService.Consumers;
using Storage.Storages;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddScoped<IUserStorage, UserStorage>();
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
