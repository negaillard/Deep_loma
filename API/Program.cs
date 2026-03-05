using API.Authorization;
using API.Seeding;
using Auth.Authentication;
using Contracts.BindingModels.Authentication;
using Contracts.LogicContracts;
using Contracts.LogicContracts.Authentication;
using Contracts.StorageContracts;
using FileStorage;
using Logic;
using Logic.Authentication;
using Microsoft.OpenApi.Models;
using Models;
using Storage.Storages;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddStackExchangeRedisCache(options =>
{
	options.Configuration = builder.Configuration.GetConnectionString("Redis");
	options.InstanceName = builder.Configuration["Redis:InstanceName"];
});

builder.Services.AddSwaggerGen(options =>
{
	options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
	{
		Description = "¬ведите токен в формате: Bearer {token}",
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
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
	app.UseSwagger();
	app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.UseMiddleware<AuthMiddleware>();
app.MapControllers();

await app.SeedInitialAdminAsync();

app.Run();

