using Common.Configuration;
using Common.Keystore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.OpenApi.Models;
using Usi.WebApi.Auth;
using UsiClient;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "USI Web API",
        Version = "v1",
        Description = "REST API wrapper for Australian Government USI (Unique Student Identifier) verification services",
        Contact = new()
        {
            Name = "USI Support",
            Email = "it@usi.gov.au"
        }
    });

    // Include XML comments if available
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});

// Add USI Client services (reuses existing extension method)
builder.Services.AddUsiClient(builder.Configuration, out var clientMode);

// Configure CORS for NextJS and other web applications
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowWebApps", policy =>
    {
        var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>()
            ?? new[] { "http://localhost:3000" }; // Default to NextJS dev server

        policy.WithOrigins(allowedOrigins)
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

// API key authentication
builder.Services.AddAuthentication(ApiKeyAuthenticationOptions.DefaultScheme)
    .AddScheme<ApiKeyAuthenticationOptions, ApiKeyAuthenticationHandler>(ApiKeyAuthenticationOptions.DefaultScheme, _ => { });
builder.Services.AddAuthorization();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "USI Web API v1");
        options.RoutePrefix = "swagger";
    });
}

app.UseHttpsRedirection();
app.UseCors("AllowWebApps");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Log startup information
var logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("USI Web API starting with client mode: {ClientMode}", clientMode);
logger.LogInformation("Environment: {Environment}", app.Environment.EnvironmentName);

app.Run();
