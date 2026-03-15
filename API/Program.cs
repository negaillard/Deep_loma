using API.Authorization;
using API.Controllers;
using API.Seeding;
using Auth.Authentication;
using Contracts.BindingModels.Authentication;
using Contracts.LogicContracts;
using Contracts.LogicContracts.Authentication;
using Contracts.StorageContracts;
using FileStorage;
using Logic;
using Logic.Authentication;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.OpenApi.Models;
using Models;
using Storage.Storages;

AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
{
	Console.WriteLine($"[FATAL] UnhandledException: {e.ExceptionObject}");
};

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(options =>
{
	options.Limits.MaxRequestBodySize = 100_000_000; // 100 MB
});

builder.Services.Configure<FormOptions>(options =>
{
	options.MultipartBodyLengthLimit = 100_000_000; // 100 MB
});

builder.Services.Configure<FileUploadPolicy>(
	builder.Configuration.GetSection("FileUploadPolicy"));

builder.Services.Configure<AntivirusOptions>(
	builder.Configuration.GetSection("Antivirus"));

builder.Services.AddStackExchangeRedisCache(options =>
{
	options.Configuration = builder.Configuration.GetConnectionString("Redis");
	options.InstanceName = builder.Configuration["Redis:InstanceName"];
});

builder.Services.AddSwaggerGen(options =>
{
	options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
	{
		Description = "??????? ????? ? ???????: Bearer {token}",
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

builder.Services.Configure<RedisSettings>(builder.Configuration.GetSection("Redis"));

builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));

builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<ICodeVerificationLogic, CodeVerificationLogic>();
builder.Services.AddScoped<ISessionService, SessionService>();

builder.Services.AddScoped<IUserLogic, UserLogic>();
builder.Services.AddScoped<IRoleLogic, RoleLogic>();
builder.Services.AddScoped<IDocumentLogic, DocumentLogic>();
builder.Services.AddScoped<IDocumentUserLogic, DocumentUserLogic>();
builder.Services.AddScoped<ICertificateLogic, CertificateLogic>();
builder.Services.AddScoped<ISignatureLogic, SignatureLogic>();
builder.Services.AddScoped<IAntivirusService, ClamAvService>();

builder.Services.AddScoped<IUserStorage, UserStorage>();
builder.Services.AddScoped<IRoleStorage, RoleStorage>();
builder.Services.AddScoped<IDocumentStorage, DocumentStorage>();
builder.Services.AddScoped<IDocumentUserStorage, DocumentUserStorage>();
builder.Services.AddScoped<ICertificateStorage, CertificateStorage>();
builder.Services.AddScoped<ISignatureStorage, SignatureStorage>();

builder.Services.AddScoped<IFileStorage, LocalFileStorage>();

var certificateMode = builder.Configuration.GetValue<CertificateMode>("AppMode");

if (certificateMode == CertificateMode.Internal)
{
	builder.Services.AddScoped<ICertificateGeneratorLogic, SelfSignedCertificateGenerator>();
}
else
{
	//builder.Services.AddScoped<ICertificateGeneratorLogic, CryptoProCertificateImporter>();
}


builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
	app.UseSwagger();
	app.UseSwaggerUI();
}

app.Use(async (context, next) =>
{
	var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
	logger.LogWarning(">>> CANARY: {Method} {Path} ContentType={CT} ContentLength={CL}",
		context.Request.Method,
		context.Request.Path,
		context.Request.ContentType,
		context.Request.ContentLength);
	try
	{
		await next(context);
		logger.LogWarning(">>> CANARY DONE: {StatusCode}", context.Response.StatusCode);
	}
	catch (Exception ex)
	{
		logger.LogError(ex, ">>> CANARY CAUGHT EXCEPTION: {Message}", ex.Message);
		throw;
	}
});

app.UseExceptionHandler(errorApp =>
{
	errorApp.Run(async context =>
	{
		context.Response.StatusCode = 500;
		context.Response.ContentType = "application/json";
		await context.Response.WriteAsJsonAsync(new { error = "?????????? ?????? ???????" });
	});
});

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.UseMiddleware<AuthMiddleware>();
app.MapControllers();

await app.SeedInitialAdminAsync();

app.Run();

