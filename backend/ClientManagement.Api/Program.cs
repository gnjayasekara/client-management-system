using System.Text;
using ClientManagement.Api.Features.Auth;
using ClientManagement.Api.Features.Auth.Services;
using ClientManagement.Api.Infrastructure.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

using Microsoft.OpenApi;


using ClientManagement.Api.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

using ClientManagement.Api.Features.Clients;
using ClientManagement.Api.Features.Clients.Validators;
using FluentValidation;

using ClientManagement.Api.Features.FollowUps;

using ClientManagement.Api.Features.Notes;

using ClientManagement.Api.Features.Dashboard;

using System.Security.Claims;

using ClientManagement.Api.Infrastructure.Realtime;

using ClientManagement.Api.Infrastructure.Messaging.FollowUps;

using ClientManagement.Api.Infrastructure.Messaging.Notes;

// using ClientManagement.Api.Infrastructure.Messaging.Rpc;

using Microsoft.Extensions.Caching.Distributed;

using Serilog;

using ClientManagement.Api.Infrastructure.Caching;
using ClientManagement.Api.Features.Clients.Commands;
using ClientManagement.Api.Infrastructure.Messaging.Clients;
using ClientManagement.Api.Features.Clients.Events;

using ClientManagement.Api.Features.FollowUps.Events;
using ClientManagement.Api.Features.Notes.Events;

using ClientManagement.Api.Features.Clients.Queries;






var builder = WebApplication.CreateBuilder(args);


if (builder.Environment.IsDevelopment())
{
    builder.Services.AddSerilog((services, logger) =>
    {
        logger
            .MinimumLevel.Information()
            .MinimumLevel.Override(
                "Microsoft.AspNetCore",
                Serilog.Events.LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .WriteTo.Seq(
                builder.Configuration["Seq:ServerUrl"]
                ?? "http://localhost:5341"
            );
    });
}


builder.Services.AddEndpointsApiExplorer();


builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer(
        (document, context, cancellationToken) =>
        {
            document.Components ??= new OpenApiComponents();

            document.Components.SecuritySchemes ??=
                new Dictionary<string, IOpenApiSecurityScheme>();

            // Register JWT Bearer authentication
            document.Components.SecuritySchemes["Bearer"] =
                new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    Description = "Enter your JWT access token."
                };

            // Apply Bearer authentication to API operations
            foreach (var path in document.Paths.Values)
            {
                if (path.Operations is null)
                {
                    continue;
                }

                foreach (var operation in path.Operations.Values)
                {
                    operation.Security ??=
                        new List<OpenApiSecurityRequirement>();

                    operation.Security.Add(
                        new OpenApiSecurityRequirement
                        {
                            [new OpenApiSecuritySchemeReference(
                                "Bearer",
                                document
                            )] = new List<string>()
                        });
                }
            }

            return Task.CompletedTask;
        });
});



builder.Services.AddScoped<
    DashboardCacheInvalidationInterceptor>();

builder.Services.AddDbContext<AppDbContext>((serviceProvider, options) =>
{
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")
    );

    options.AddInterceptors(
        serviceProvider.GetRequiredService<
            DashboardCacheInvalidationInterceptor>()
    );
});


builder.Services.AddDataProtection();

builder.Services
    .AddIdentityCore<AppUser>(options =>
    {
        options.Password.RequiredLength = 8;
        options.Password.RequireDigit = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = true;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AngularApp", policy =>
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials());
});

builder.Services.AddValidatorsFromAssemblyContaining<
    CreateClientValidator>();


var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException(
        "JWT signing key not configured.");

if (Encoding.UTF8.GetByteCount(jwtKey) < 32)
{
    throw new InvalidOperationException(
        "JWT signing key must be at least 32 bytes.");
}

var jwtIssuer = builder.Configuration["Jwt:Issuer"]
    ?? throw new InvalidOperationException(
        "JWT issuer not configured.");

var jwtAudience = builder.Configuration["Jwt:Audience"]
    ?? throw new InvalidOperationException(
        "JWT audience not configured.");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;

        options.TokenValidationParameters =
            new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = jwtIssuer,

                ValidateAudience = true,
                ValidAudience = jwtAudience,

                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(jwtKey)),

                ValidateLifetime = true,
                RequireExpirationTime = true,
                RequireSignedTokens = true,

                ClockSkew = TimeSpan.Zero
            };
        
        options.Events ??= new JwtBearerEvents();

        var existingOnMessageReceived =
            options.Events.OnMessageReceived;

        options.Events.OnMessageReceived = async context =>
        {
            // Preserve any existing token handling
            if (existingOnMessageReceived is not null)
            {
                await existingOnMessageReceived(context);
            }

            if (!string.IsNullOrEmpty(context.Token))
            {
                return;
            }

            var accessToken =
                context.Request.Query["access_token"];

            var path = context.HttpContext.Request.Path;

            // Only accept query-string JWTs for the SignalR Hub
            if (!string.IsNullOrEmpty(accessToken) &&
                path.StartsWithSegments("/hubs/notifications"))
            {
                context.Token = accessToken;
            }
        };

    });

builder.Services.AddAuthorization();


builder.Services.AddScoped<JwtTokenService>();

builder.Services.AddScoped<IEmailService, SmtpEmailService>();

builder.Services.AddSignalR();

// builder.Services.AddScoped<GetClientsService>();

builder.Services.AddScoped<
    IRealtimeNotifier,
    SignalRRealtimeNotifier>();



// builder.Services.AddScoped<ClientSearchCacheVersion>();





var rabbitMqEnabled =
    builder.Configuration.GetValue<bool>("RabbitMq:Enabled")
    && !builder.Environment.IsEnvironment("Testing");

if (rabbitMqEnabled)
{
    // Client events
    builder.Services.AddScoped<
        IClientEventPublisher,
        RabbitMqClientEventPublisher>();

    builder.Services.AddHostedService<ClientCreatedConsumer>();

    
    builder.Services.AddHostedService(sp =>
        new ClientEventConsumer<ClientUpdatedEvent>(
            "cms.clients.updated",
            sp.GetRequiredService<IConfiguration>(),
            sp.GetRequiredService<
                ILogger<ClientEventConsumer<ClientUpdatedEvent>>>()));

    builder.Services.AddHostedService(sp =>
        new ClientEventConsumer<ClientDeletedEvent>(
            "cms.clients.deleted",
            sp.GetRequiredService<IConfiguration>(),
            sp.GetRequiredService<
            ILogger<ClientEventConsumer<ClientDeletedEvent>>>()));


    // FollowUp events
    builder.Services.AddScoped<
        IFollowUpEventPublisher,
        RabbitMqFollowUpEventPublisher>();

    builder.Services.AddHostedService<FollowUpCreatedConsumer>();

    
    builder.Services.AddHostedService(sp =>
        new ClientEventConsumer<FollowUpUpdatedEvent>(
            "cms.followups.updated",
            sp.GetRequiredService<IConfiguration>(),
            sp.GetRequiredService<
                ILogger<ClientEventConsumer<FollowUpUpdatedEvent>>>()));

    builder.Services.AddHostedService(sp =>
        new ClientEventConsumer<FollowUpCompletedEvent>(
            "cms.followups.completed",
            sp.GetRequiredService<IConfiguration>(),
            sp.GetRequiredService<
                ILogger<ClientEventConsumer<FollowUpCompletedEvent>>>()));

    builder.Services.AddHostedService(sp =>
        new ClientEventConsumer<FollowUpDeletedEvent>(
            "cms.followups.deleted",
            sp.GetRequiredService<IConfiguration>(),
            sp.GetRequiredService<
                ILogger<ClientEventConsumer<FollowUpDeletedEvent>>>()));


    // Note events
    builder.Services.AddScoped<
        INoteEventPublisher,
        RabbitMqNoteEventPublisher>();

    builder.Services.AddHostedService<NoteCreatedConsumer>();

    builder.Services.AddHostedService(sp =>
        new ClientEventConsumer<NoteDeletedEvent>(
            "cms.notes.deleted",
            sp.GetRequiredService<IConfiguration>(),
            sp.GetRequiredService<
                ILogger<ClientEventConsumer<NoteDeletedEvent>>>()));
}
else
{
    // Client events
    builder.Services.AddScoped<
        IClientEventPublisher,
        NoOpClientEventPublisher>();

    // FollowUp events
    builder.Services.AddScoped<
        IFollowUpEventPublisher,
        NoOpFollowUpEventPublisher>();

    // Note events
    builder.Services.AddScoped<
        INoteEventPublisher,
        NoOpNoteEventPublisher>();
}




if (builder.Environment.IsDevelopment())
{
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration =
            builder.Configuration["Redis:ConnectionString"]
            ?? "localhost:6379";

        options.InstanceName = "cms:";
    });
}
else
{
    builder.Services.AddDistributedMemoryCache();
}

var app = builder.Build();

app.Logger.LogInformation(
    "Client Management API started. Logging integration: {Provider}",
    "Seq"
);




app.UseHttpsRedirection();
app.UseCors("AngularApp");



app.UseAuthentication();

// Enforce mandatory first-login password change

app.Use(async (context, next) =>
{
    var path = context.Request.Path;

    // Only inspect authenticated requests
    if (context.User.Identity?.IsAuthenticated != true)
    {
        await next(context);
        return;
    }

    var isApi = path.StartsWithSegments("/api");
    var isHub = path.StartsWithSegments("/hubs/notifications");

    // Ignore requests outside application APIs and SignalR
    if (!isApi && !isHub)
    {
        await next(context);
        return;
    }

    // Public health check should remain accessible
    if (path.Equals(
        "/api/health",
        StringComparison.OrdinalIgnoreCase))
    {
        await next(context);
        return;
    }

    // Users must be able to change their passwords
    if (path.Equals(
        "/api/auth/change-password",
        StringComparison.OrdinalIgnoreCase))
    {
        await next(context);
        return;
    }

    var userId =
        context.User.FindFirst("sub")?.Value
        ?? context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    if (string.IsNullOrWhiteSpace(userId))
    {
        context.Response.StatusCode =
            StatusCodes.Status401Unauthorized;
        return;
    }

    var userManager = context.RequestServices
        .GetRequiredService<UserManager<AppUser>>();

    var user = await userManager.FindByIdAsync(userId);

    if (user is null)
    {
        context.Response.StatusCode =
            StatusCodes.Status401Unauthorized;
        return;
    }

    if (!await userManager.IsEmailConfirmedAsync(user))
    {
        context.Response.StatusCode =
            StatusCodes.Status403Forbidden;

        await context.Response.WriteAsJsonAsync(new
        {
            code = "EMAIL_NOT_VERIFIED",
            message = "Please verify your email."
        });

        return;
    }

    if (user.MustChangePassword)
    {
        context.Response.StatusCode =
            StatusCodes.Status403Forbidden;

        await context.Response.WriteAsJsonAsync(new
        {
            code = "PASSWORD_CHANGE_REQUIRED",
            message = "Please change your password before accessing the application."
        });

        return;
    }

    await next(context);
});


app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    // In Development, keep it public and easy to test with Swagger UI
    app.MapOpenApi(); 
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "Client Management API V1");
    });
}
else
{
    // In Production, secure the OpenAPI document endpoint behind authorization
    app.MapOpenApi().RequireAuthorization();
}

if (app.Environment.IsDevelopment())
{
    await AdminSeeder.SeedAsync(
        app.Services,
        app.Configuration);
}

app.MapGet("/api/health", () =>
    Results.Ok(new
    {
        status = "Healthy",
        application = "Client Management API"
    }));


app.MapGet("/api/demo/cache", async (
    IDistributedCache cache,
    AppDbContext db,
    ILogger<Program> logger,
    CancellationToken ct) =>
{
    const string key = "demo:client-count";

    var cachedValue = await cache.GetStringAsync(key, ct);

    if (cachedValue is not null)
    {
        logger.LogInformation(
            "Redis cache HIT for {CacheKey}", key);

        return Results.Ok(new
        {
            source = "redis",
            clientCount = int.Parse(cachedValue)
        });
    }

    var clientCount = await db.Clients.CountAsync(ct);

    await cache.SetStringAsync(
        key,
        clientCount.ToString(),
        new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow =
                TimeSpan.FromMinutes(5)
        },
        ct);

    logger.LogInformation(
        "Redis cache MISS for {CacheKey}", key);

    return Results.Ok(new
    {
        source = "database",
        clientCount
    });
}).RequireAuthorization();


app.MapAuthEndpoints();

app.MapClientEndpoints();
app.MapFollowUpEndpoints();
app.MapNoteEndpoints();
app.MapDashboardEndpoints();

app.MapHub<NotificationHub>("/hubs/notifications")
    .RequireAuthorization();





app.Run();


public partial class Program { }