using Contracts.LogicContracts;
using Contracts.StorageContracts;
using FileStorage;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Models;
using SigningService.Consumers;
using SigningService.Signing;
using Storage;
using Storage.Storages;

var builder = Host.CreateApplicationBuilder(args);

var storageConnectionString = builder.Configuration.GetConnectionString("DefaultConnection")
	?? builder.Configuration.GetConnectionString("Storage")
	?? throw new InvalidOperationException("Connection string 'DefaultConnection' or 'Storage' is not configured.");

builder.Services.AddDbContext<StorageContext>(options =>
	options.UseSqlServer(storageConnectionString));

builder.Services.AddScoped<IDocumentStorage, DocumentStorage>();
builder.Services.AddScoped<ICertificateStorage, CertificateStorage>();
builder.Services.AddScoped<IDocumentUserStorage, DocumentUserStorage>();
builder.Services.AddScoped<ISignatureStorage, SignatureStorage>();
builder.Services.AddScoped<IUserStorage, UserStorage>();
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
