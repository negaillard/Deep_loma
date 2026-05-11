using API;
using API.Authorization;
using API.Controllers;
using API.Seeding;
using Auth;
using Contracts.BindingModels.Authentication;
using Contracts.LogicContracts;
using Contracts.LogicContracts.Authentication;
using Contracts.StorageContracts;
using FileStorage;
using Logic;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;
using Models;
using Storage;
using Storage.Storages;

AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
{
	Console.WriteLine($"[FATAL] UnhandledException: {e.ExceptionObject}");
};

var builder = WebApplication.CreateBuilder(args);

/// опционально. конфигурация веб-сервера на прием запросов с такими данными объема
builder.WebHost.ConfigureKestrel(options =>
{
	options.Limits.MaxRequestBodySize = 100_000_000; // 100 MB
});

builder.Services.Configure<FormOptions>(options =>
{
	options.MultipartBodyLengthLimit = 100_000_000; // 100 MB
});

/// настройка файловой политики
builder.Services.Configure<FileUploadPolicy>(
	builder.Configuration.GetSection("FileUploadPolicy"));

/// конфигурация антивируса (подключение к серверу)
builder.Services.Configure<AntivirusOptions>(
	builder.Configuration.GetSection("Antivirus"));

/// Конфигурация редиса (подключение к серверу и подключение в самой БД)
builder.Services.AddStackExchangeRedisCache(options =>
{
	options.Configuration = builder.Configuration.GetConnectionString("Redis");
	options.InstanceName = builder.Configuration["Redis:InstanceName"];
});

/// авториазация в сваггере
builder.Services.AddSwaggerGen(options =>
{
	options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
	{
		Description = "Введите токен сессии в формате: Bearer {token}",
		Name = "Authorization",
		In = ParameterLocation.Header,
		Type = SecuritySchemeType.ApiKey,
		Scheme = "Bearer"
	});

	options.AddSecurityRequirement(new OpenApiSecurityRequirement
	{
		{
			new OpenApiSecurityScheme
			{
				Reference = new OpenApiReference
				{
					Type = ReferenceType.SecurityScheme,
					Id = "Bearer"
				}
			},
			Array.Empty<string>()
		}
	});

});

/// конфигурация из appsettings
builder.Services.Configure<RedisSettings>(builder.Configuration.GetSection("Redis"));
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));

/// регистрация сервисов для авторизации
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<ICodeVerificationLogic, CodeVerificationLogic>();
builder.Services.AddScoped<ISessionService, SessionService>();

var storageConnectionString = builder.Configuration.GetConnectionString("DefaultConnection")
	?? builder.Configuration.GetConnectionString("Storage")
	?? throw new InvalidOperationException("Connection string 'DefaultConnection' or 'Storage' is not configured.");

builder.Services.AddDbContext<StorageContext>(options =>
	options.UseSqlServer(storageConnectionString));

/// регистрация сервисов
builder.Services.AddScoped<IUserLogic, UserLogic>();
builder.Services.AddScoped<IRoleLogic, RoleLogic>();
builder.Services.AddScoped<IDocumentLogic, DocumentLogic>();
builder.Services.AddScoped<IDocumentUserLogic, DocumentUserLogic>();
builder.Services.AddScoped<ICertificateLogic, CertificateLogic>();
builder.Services.AddScoped<ISignatureLogic, SignatureLogic>();
builder.Services.AddScoped<IAntivirusService, ClamAvService>();

/// регистрация репозиториев
builder.Services.AddScoped<IUserStorage, UserStorage>();
builder.Services.AddScoped<IRoleStorage, RoleStorage>();
builder.Services.AddScoped<IDocumentStorage, DocumentStorage>();
builder.Services.AddScoped<IDocumentUserStorage, DocumentUserStorage>();
builder.Services.AddScoped<ICertificateStorage, CertificateStorage>();
builder.Services.AddScoped<ISignatureStorage, SignatureStorage>();

/// ����������� ��������� ���������
builder.Services.AddScoped<IFileStorage, LocalFileStorage>();

/// конфигурация масс транзита
/// MASS TRANSIT ��� ���������� ��������� � �������
/// ������������� ����� � exchange �� �������� ���������
builder.Services.AddMassTransit(x =>
{
	x.UsingRabbitMq((context, cfg) =>
	{
		// подключение к рэббиту
		cfg.Host(builder.Configuration["RabbitMQ:Host"] ?? "localhost", "/", h =>
		{
			h.Username(builder.Configuration["RabbitMQ:Username"] ?? "guest");
			h.Password(builder.Configuration["RabbitMQ:Password"] ?? "guest");
		});
	});
});

/// Режим подписи: Internal (сервер) / Local (клиент). AppMode в приоритете, иначе CertificateMode.
var appModeSection = builder.Configuration["AppMode"] ?? builder.Configuration["CertificateMode"];
var certificateMode = Enum.TryParse<CertificateMode>(appModeSection, true, out var parsedMode)
	? parsedMode
	: CertificateMode.Internal;

if (certificateMode == CertificateMode.Internal)
{
	var internalCrypto = builder.Configuration["InternalCryptoAlgorithm"] ?? "Rsa";
	if (string.Equals(internalCrypto, "Gost", StringComparison.OrdinalIgnoreCase))
		builder.Services.AddScoped<ICertificateGeneratorLogic, SelfSignedCertificateGeneratorGost>();
	else
		builder.Services.AddScoped<ICertificateGeneratorLogic, SelfSignedCertificateGenerator>();
}
else
{
	builder.Services.AddScoped<ICertificateGeneratorLogic, LocalModeCertificateGeneratorStub>();
}

builder.Services.AddControllers();

// не поднимаем контроллер сертификата при локальной подписи
if (certificateMode == CertificateMode.Local)
{
	builder.Services.Configure<MvcOptions>(options =>
	{
		options.Conventions.Add(new ExcludeControllerConvention(typeof(CertificatesController)));
	});
}

builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
	app.UseSwagger();
	app.UseSwaggerUI();
}

app.UseExceptionHandler(errorApp =>
{
	errorApp.Run(async context =>
	{
		context.Response.StatusCode = 500;
		context.Response.ContentType = "application/json";
		await context.Response.WriteAsJsonAsync(new { error = "?????????? ?????? ???????" });
	});
});

// Редирект HTTP→HTTPS: в Docker без TLS на Kestrel ломает клиентов. Отключение: DisableHttpsRedirection=true (env или конфиг).
if (!builder.Configuration.GetValue("DisableHttpsRedirection", false))
	app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.UseMiddleware<AuthMiddleware>();
app.MapControllers();

await app.SeedInitialAdminAsync();

app.Run();
