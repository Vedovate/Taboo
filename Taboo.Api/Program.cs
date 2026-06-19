using Taboo.Api.Database;
using Taboo.Api.Hubs;
using Taboo.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Adiciona SignalR ao contêiner de serviços.
builder.Services.AddSignalR();

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

// Registrar o GameManager como um serviço Singleton.
// Isso garante que haja apenas uma instância do GameManager gerenciando o estado do jogo.
builder.Services.AddSingleton<IGameManager, GameManager>();

var app = builder.Build();
var connectionString = app.Configuration.GetConnectionString("SqliteConnection");

if (!string.IsNullOrEmpty(connectionString))
{
    if (connectionString.StartsWith("Data Source=") && !connectionString.Contains('/') && !connectionString.Contains('\\'))
    {
        var fileName = connectionString.Replace("Data Source=", "");
        connectionString = $"Data Source={Path.Combine(app.Environment.ContentRootPath + "/Database", fileName)}";
    }

    DbInitializer.Initialize(connectionString);
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors(); // Usa a política CORS padrão definida
app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

// Mapeia o GameHub para a rota "/gamehub".
// É aqui que o frontend Angular se conectará.
app.MapHub<GameHub>("/gamehub");

app.Run();

