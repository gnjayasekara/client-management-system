
using ClientManagement.Api.Infrastructure.Data;
using ClientManagement.Api.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

using ClientManagement.Api.Features.Clients;
using ClientManagement.Api.Features.Clients.Validators;
using FluentValidation;

using ClientManagement.Api.Features.FollowUps;

using ClientManagement.Api.Features.Notes;

using ClientManagement.Api.Features.Dashboard;

var builder = WebApplication.CreateBuilder(args);

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
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AngularApp", policy =>
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod());
});

builder.Services.AddValidatorsFromAssemblyContaining<
    CreateClientValidator>();

var app = builder.Build();

app.MapClientEndpoints();
app.MapFollowUpEndpoints();
app.MapNoteEndpoints();
app.MapDashboardEndpoints();

app.UseHttpsRedirection();
app.UseCors("AngularApp");

app.MapGet("/api/health", () =>
    Results.Ok(new
    {
        status = "Healthy",
        application = "Client Management API"
    }));

app.Run();
