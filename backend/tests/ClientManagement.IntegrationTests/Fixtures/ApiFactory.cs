
using ClientManagement.Api.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using ClientManagement.Api.Features.Auth.Services;
using NSubstitute;

namespace ClientManagement.IntegrationTests.Fixtures;

public class ApiFactory : WebApplicationFactory<Program>
{
    private const string TestJwtKey =
        "IntegrationTestSigningKey-OnlyForTests-2026!";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        var connectionString = Environment.GetEnvironmentVariable(
            "CLIENTMANAGEMENT_TEST_CONNECTION");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "CLIENTMANAGEMENT_TEST_CONNECTION is not configured.");
        }

        var sql = new SqlConnectionStringBuilder(connectionString);

        if (!string.Equals(
                sql.InitialCatalog,
                "ClientManagementDb_Test",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Integration tests must use ClientManagementDb_Test.");
        }

        // Override the test database registration.
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<DbContextOptions<AppDbContext>>();

            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(connectionString));

            services.RemoveAll<IEmailService>();

            services.AddSingleton(
                Substitute.For<IEmailService>()
);
        });

        
    }

    public ApiFactory()
    {
        // Program.cs reads JWT configuration before the test host
        // finishes configuring services, so provide it early.

        Environment.SetEnvironmentVariable(
            "Jwt__Key", TestJwtKey);

        Environment.SetEnvironmentVariable(
            "Jwt__Issuer", "ClientManagement.Tests");

        Environment.SetEnvironmentVariable(
            "Jwt__Audience", "ClientManagement.Tests");

        Environment.SetEnvironmentVariable(
            "Jwt__ExpiryMinutes", "60");

        Environment.SetEnvironmentVariable(
            "EmailSettings__FrontendBaseUrl",
            "http://localhost:4200");
    }
}
