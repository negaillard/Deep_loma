using Contracts.LogicContracts;
using Contracts.StorageContracts;
using FileStorage;
using MassTransit;
using Models;
using SigningService.Consumers;
using SigningService.Signing;
using Storage.Storages;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddScoped<IDocumentStorage, DocumentStorage>();
builder.Services.AddScoped<ICertificateStorage, CertificateStorage>();
builder.Services.AddScoped<IDocumentUserStorage, DocumentUserStorage>();
builder.Services.AddScoped<ISignatureStorage, SignatureStorage>();
builder.Services.AddScoped<IFileStorage, LocalFileStorage>();


// В режиме Local подписание происходит на клиенте, SigningService не используется
var internalCrypto = builder.Configuration["InternalCryptoAlgorithm"] ?? "Rsa";
if (string.Equals(internalCrypto, "Gost", StringComparison.OrdinalIgnoreCase))
	builder.Services.AddScoped<IDocumentSigner, InternalDocumentSignerGost>();
else
	builder.Services.AddScoped<IDocumentSigner, InternalDocumentSigner>();

builder.Services.AddMassTransit(x =>
{
	x.AddConsumer<SignDocumentConsumer>();

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
