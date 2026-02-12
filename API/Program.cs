using API.Authorization;
using Auth.Authentication;
using Contracts.BindingModels.Authentication;
using Contracts.LogicContracts;
using Contracts.LogicContracts.Authentication;
using Contracts.StorageContracts;
using Logic;
using Logic.Authentication;
using Storage.Storages;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddStackExchangeRedisCache(options =>
{
	options.Configuration = builder.Configuration.GetConnectionString("Redis");
	options.InstanceName = builder.Configuration["Redis:InstanceName"];
});
builder.Services.Configure<RedisSettings>(builder.Configuration.GetSection("Redis"));

builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));

builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<ICodeVerificationLogic, CodeVerificationLogic>();
builder.Services.AddScoped<ISessionService, SessionService>();

builder.Services.AddScoped<IUserLogic, UserLogic>();
builder.Services.AddScoped<IRoleLogic, RoleLogic>();

builder.Services.AddScoped<IUserStorage, UserStorage>();
builder.Services.AddScoped<IRoleStorage, RoleStorage>();

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
app.UseMiddleware<AuthMiddleware>();


app.Run();

