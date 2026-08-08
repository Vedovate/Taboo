using Microsoft.AspNetCore.SignalR;
using Tipoo.Api.Database;
using Tipoo.Api.Data;
using Tipoo.Api.Filters;
using Tipoo.Api.Hubs;
using Tipoo.Api.Infrastructure;
using Tipoo.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Adiciona SignalR ao contêiner de serviços com filtro global de erros.
builder.Services.AddSignalR(hubOptions =>
{
    hubOptions.AddFilter(new GameHubFilter());
});

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(builder =>
    {
        builder.WithOrigins("http://localhost:4200") // URL do seu frontend Angular
               .AllowAnyHeader()
               .AllowAnyMethod()
               .AllowCredentials(); // Essencial para SignalR
    });
});

var configuredConnectionString = builder.Configuration.GetConnectionString("SqliteConnection") ?? string.Empty;
var connectionStringProvider = new ConnectionStringProvider(configuredConnectionString, builder.Environment.ContentRootPath);
builder.Services.AddSingleton(connectionStringProvider);
builder.Services.AddSingleton<IGameDataStore, GameDataStore>();
builder.Services.AddSingleton<IGameManager, GameManager>();

var app = builder.Build();

if (!string.IsNullOrEmpty(configuredConnectionString))
{
    DbInitializer.Initialize(connectionStringProvider.ConnectionString);
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors(); // Usa a política CORS padrão definida

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseAuthorization();

app.MapControllers();

app.MapHub<GameHub>("/gamehub");

app.Run();

