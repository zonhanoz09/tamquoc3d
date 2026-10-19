using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.OpenApi.Models;
using Npgsql;
using TamQuoc3D.Server.Authentication;
using TamQuoc3D.Server.Contracts;
using TamQuoc3D.Server.Endpoints;
using TamQuoc3D.Server.Middleware;
using TamQuoc3D.Server.Services;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("GameDatabase")
    ?? DatabaseConfiguration.FromEnvironment(builder.Configuration["DATABASE_URL"]);
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException("Configure ConnectionStrings:GameDatabase or DATABASE_URL.");
}

builder.Services.AddSingleton(NpgsqlDataSource.Create(connectionString));
builder.Services.AddSingleton<IAuthService, AuthService>();
builder.Services.AddSingleton<IGameService, GameService>();
builder.Services.AddAuthentication("GameSession")
    .AddScheme<AuthenticationSchemeOptions, SessionAuthenticationHandler>("GameSession", _ => { });
builder.Services.AddAuthorization();
builder.Services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
    options.SerializerOptions.DictionaryKeyPolicy = null;
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Tam Quoc 3D Game Server",
        Version = "1.0.0"
    });
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "Opaque session token",
        In = ParameterLocation.Header,
        Description = "Bearer access token returned by /api/auth/login or /api/auth/register."
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        [new OpenApiSecurityScheme
        {
            Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
        }] = Array.Empty<string>()
    });
});

var app = builder.Build();
app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseSwagger();
app.UseSwaggerUI();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", () => Results.Redirect("/docs"))
    .WithSummary("Open the API documentation.");
app.MapGet("/health", async (IGameService game, CancellationToken ct) =>
    TypedResults.Ok(await game.GetHealthAsync(ct)))
    .WithName("GetHealth")
    .WithSummary("Check PostgreSQL and Redis availability.")
    .Produces<HealthResponse>();

app.MapAuthEndpoints();
app.MapPlayerEndpoints();

app.Run();

public partial class Program;

internal static class DatabaseConfiguration
{
    public static string? FromEnvironment(string? databaseUrl)
    {
        if (string.IsNullOrWhiteSpace(databaseUrl))
        {
            return null;
        }

        if (!Uri.TryCreate(databaseUrl, UriKind.Absolute, out var uri) ||
            (uri.Scheme != "postgres" && uri.Scheme != "postgresql"))
        {
            return databaseUrl;
        }

        var userInfo = uri.UserInfo.Split(':', 2);
        if (userInfo.Length != 2)
        {
            throw new InvalidOperationException("DATABASE_URL must include a username and password.");
        }

        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.IsDefaultPort ? 5432 : uri.Port,
            Database = Uri.UnescapeDataString(uri.AbsolutePath.TrimStart('/')),
            Username = Uri.UnescapeDataString(userInfo[0]),
            Password = Uri.UnescapeDataString(userInfo[1])
        };
        return builder.ConnectionString;
    }
}
