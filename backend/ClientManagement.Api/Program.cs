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




var builder = WebApplication.CreateBuilder(args);

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


builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")
    )
);

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

builder.Services.AddScoped<
    IRealtimeNotifier,
    SignalRRealtimeNotifier>();

var app = builder.Build();




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

app.MapAuthEndpoints();

app.MapClientEndpoints();
app.MapFollowUpEndpoints();
app.MapNoteEndpoints();
app.MapDashboardEndpoints();

app.MapHub<NotificationHub>("/hubs/notifications")
    .RequireAuthorization();





app.Run();


public partial class Program { }