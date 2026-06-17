using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Text.Json;
using Class.Api.Auth;
using Class.Api.Middleware;
using Class.Application;
using Class.Application.Abstractions;
using Class.Application.Common;
using Class.Infrastructure;
using Class.Infrastructure.Seed;
using Class.Infrastructure.Support;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

EnvFileLoader.LoadNearest();
JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddEnvironmentVariables();

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUserAccessor>();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy
            .WithOrigins(ReadCorsOrigins(builder.Configuration))
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services
    .AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var details = context.ModelState
                .Where(x => x.Value?.Errors.Count > 0)
                .ToDictionary(
                    x => ToCamelCase(x.Key),
                    x => x.Value!.Errors.Select(error => string.IsNullOrWhiteSpace(error.ErrorMessage)
                        ? "Invalid value."
                        : error.ErrorMessage).ToArray());

            var response = ApiResponse<object>.Fail(new ApiError(ErrorCodes.ValidationError, "Validation failed.", details));
            return new BadRequestObjectResult(response);
        };
    });

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var secret = ReadRequired(builder.Configuration, "JWT_SECRET", "Jwt:Secret");
        var issuer = ReadOptional(builder.Configuration, "JWT_ISSUER", "Jwt:Issuer", "ags");
        var audience = ReadOptional(builder.Configuration, "JWT_AUDIENCE", "Jwt:Audience", "ags-api");

        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),
            ValidateIssuer = true,
            ValidIssuer = issuer,
            ValidateAudience = true,
            ValidAudience = audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1),
            RoleClaimType = "role",
            NameClaimType = "email"
        };

        options.Events = new JwtBearerEvents
        {
            OnChallenge = context =>
            {
                context.HandleResponse();
                return WriteErrorAsync(
                    context.HttpContext,
                    StatusCodes.Status401Unauthorized,
                    ErrorCodes.Unauthorized,
                    "A valid bearer token is required.");
            },
            OnForbidden = context =>
            {
                return WriteErrorAsync(
                    context.HttpContext,
                    StatusCodes.Status403Forbidden,
                    ErrorCodes.Forbidden,
                    "You do not have permission to access this resource.");
            }
        };
    });

builder.Services.AddAuthorization();

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "EvalCore Class Service",
            Version = "v1"
        });

        var securityScheme = new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Description = "JWT Bearer token. Example: Bearer {token}",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Reference = new OpenApiReference
            {
                Type = ReferenceType.SecurityScheme,
                Id = "Bearer"
            }
        };

        options.AddSecurityDefinition("Bearer", securityScheme);
        options.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            [securityScheme] = Array.Empty<string>()
        });
    });
}

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    await SeedDevelopmentDataAsync(app);
}

app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapGet("/health", () => Results.Ok(ApiResponse<object>.Ok(new
{
    status = "healthy",
    service = "evalcore-class-service",
    timestamp = DateTimeOffset.UtcNow
}))).AllowAnonymous();

await app.RunAsync();

static string ReadRequired(IConfiguration configuration, string environmentKey, string configKey)
{
    var value = configuration[environmentKey] ?? configuration[configKey];
    if (string.IsNullOrWhiteSpace(value))
    {
        throw new InvalidOperationException($"{environmentKey} must be configured.");
    }

    return value;
}

static string ReadOptional(IConfiguration configuration, string environmentKey, string configKey, string fallback)
{
    return configuration[environmentKey] ?? configuration[configKey] ?? fallback;
}

static string[] ReadCorsOrigins(IConfiguration configuration)
{
    var raw = configuration["CORS_ALLOWED_ORIGINS"];
    if (!string.IsNullOrWhiteSpace(raw))
    {
        return raw
            .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Select(origin => origin.Trim('[', ']', '(', ')'))
            .ToArray();
    }

    var configured = configuration.GetSection("Cors:AllowedOrigins")
        .GetChildren()
        .Select(section => section.Value)
        .Where(value => !string.IsNullOrWhiteSpace(value))
        .Select(value => value!)
        .ToArray();
    return configured is { Length: > 0 }
        ? configured
        : ["http://localhost:3000", "http://localhost:5173"];
}

static string ToCamelCase(string value)
{
    if (string.IsNullOrWhiteSpace(value))
    {
        return value;
    }

    var lastSegment = value.Split('.').Last();
    return char.ToLowerInvariant(lastSegment[0]) + lastSegment[1..];
}

static async Task WriteErrorAsync(HttpContext httpContext, int statusCode, string code, string message)
{
    if (httpContext.Response.HasStarted)
    {
        return;
    }

    httpContext.Response.StatusCode = statusCode;
    httpContext.Response.ContentType = "application/json";
    var response = ApiResponse<object>.Fail(new ApiError(code, message));
    await httpContext.Response.WriteAsync(JsonSerializer.Serialize(response, new JsonSerializerOptions(JsonSerializerDefaults.Web)));
}

static async Task SeedDevelopmentDataAsync(WebApplication app)
{
    try
    {
        using var scope = app.Services.CreateScope();
        var seeder = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();
        await seeder.SeedDevelopmentAsync();
    }
    catch (Exception ex)
    {
        app.Logger.LogWarning(ex, "Development seed skipped. Ensure PostgreSQL is running and migrations are applied.");
    }
}
