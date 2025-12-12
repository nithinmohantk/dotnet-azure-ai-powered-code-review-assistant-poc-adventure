using CodeReviewAssistant.Infrastructure.Persistence.HealthChecks;
using CodeReviewAssistant.WebApi.Middleware;
using CodeReviewAssistant.WebApi.Extensions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.OpenApi.Models;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .CreateLogger();

try
{
    builder.Host.UseSerilog();

    // Add services to the container.
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    
    // Configure Swagger
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo { Title = "Code Review Assistant API", Version = "v1" });
        
        // Add JWT Authentication
        c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Description = "JWT Authorization header using the Bearer scheme",
            Type = SecuritySchemeType.Http,
            Scheme = "bearer"
        });
        
        c.AddSecurityRequirement(new OpenApiSecurityRequirement
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

    // Add Application Insights
    builder.Services.AddApplicationInsightsTelemetry();

    // Add Authentication
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.Authority = $"{builder.Configuration["AzureAd:Instance"]}{builder.Configuration["AzureAd:TenantId"]}";
            options.Audience = builder.Configuration["AzureAd:ClientId"];
        });

    // Add Authorization
    builder.Services.AddAuthorization();

    // Add CORS
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowAll", builder =>
            builder.AllowAnyOrigin()
                   .AllowAnyMethod()
                   .AllowAnyHeader());
    });

    // Update Program.cs to add these lines in the services configuration:
    builder.Services.AddHealthChecks()
        .AddCheck<DatabaseHealthCheck>("database");
    // Add after AddControllers():
    builder.Services.Configure<RouteOptions>(options => 
    {
        options.LowercaseUrls = true;
        options.LowercaseQueryStrings = true;
    });

    // Register application services
    builder.Services.AddOpenTelemetryObservability(builder.Configuration);
    builder.Services.AddInfrastructureServices(builder.Configuration);
    builder.Services.AddInfrastructureMessaging(builder.Configuration);

    var app = builder.Build();

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Code Review Assistant API v1"));
    }

    app.UseHttpsRedirection();
    app.UseCors("AllowAll");
    app.UseAuthentication();
    app.UseAuthorization();

    // Add global error handling middleware
    app.UseMiddleware<ExceptionHandlingMiddleware>();

    app.MapControllers();

    Log.Information("Starting web host");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Host terminated unexpectedly");
    return 1;
}
finally
{
    Log.CloseAndFlush();
}

return 0;
