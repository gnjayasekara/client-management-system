
using ClientManagement.Api.Features.Clients.DTOs;
using ClientManagement.Api.Features.Clients.Validators;
using FluentValidation.TestHelper;
using Xunit;

namespace ClientManagement.UnitTests.Clients;

public class CreateClientValidatorTests
{
    private readonly CreateClientValidator _validator = new();

    // Reusable valid request for tests
    private static CreateClientRequest ValidRequest() => new(
        Name: "John Silva",
        Company: "ABC Solutions",
        Email: "john@example.com",
        Phone: "0771234567",
        Address: "Colombo",
        IsActive: true
    );

    // TEST 1: Valid client
    [Fact]
    public void ValidClient_ShouldPassValidation()
    {
        // Arrange
        var request = ValidRequest();

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    // TEST 2: Empty name
    [Fact]
    public void Name_WhenEmpty_ShouldHaveValidationError()
    {
        var request = ValidRequest() with { Name = "" };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    // TEST 3: Whitespace-only name
    [Fact]
    public void Name_WhenWhitespace_ShouldHaveValidationError()
    {
        var request = ValidRequest() with { Name = "   " };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    // TEST 4: Name exceeding 150 characters
    [Fact]
    public void Name_WhenLongerThan150_ShouldHaveValidationError()
    {
        var request = ValidRequest() with
        {
            Name = new string('A', 151)
        };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    // TEST 5: Name exactly 150 characters
    [Fact]
    public void Name_WhenExactly150_ShouldPassValidation()
    {
        var request = ValidRequest() with
        {
            Name = new string('A', 150)
        };

        var result = _validator.TestValidate(request);

        result.ShouldNotHaveValidationErrorFor(x => x.Name);
    }

    // TEST 6: Company exceeding 150 characters
    [Fact]
    public void Company_WhenLongerThan150_ShouldHaveValidationError()
    {
        var request = ValidRequest() with
        {
            Company = new string('B', 151)
        };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Company);
    }

    // TEST 7: Company is optional
    [Fact]
    public void Company_WhenNull_ShouldPassValidation()
    {
        var request = ValidRequest() with { Company = null };

        var result = _validator.TestValidate(request);

        result.ShouldNotHaveValidationErrorFor(x => x.Company);
    }

    // TEST 8: Invalid email
    [Theory]
    [InlineData("invalid-email")]
    [InlineData("test@")]
    [InlineData("@example.com")]
    public void Email_WhenInvalid_ShouldHaveValidationError(
        string email)
    {
        var request = ValidRequest() with { Email = email };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    // TEST 9: Email is optional
    [Fact]
    public void Email_WhenNull_ShouldPassValidation()
    {
        var request = ValidRequest() with { Email = null };

        var result = _validator.TestValidate(request);

        result.ShouldNotHaveValidationErrorFor(x => x.Email);
    }

    // TEST 10: Empty email is optional
    [Fact]
    public void Email_WhenEmpty_ShouldPassValidation()
    {
        var request = ValidRequest() with { Email = "" };

        var result = _validator.TestValidate(request);

        result.ShouldNotHaveValidationErrorFor(x => x.Email);
    }

    // TEST 11: Phone exceeding 30 characters
    [Fact]
    public void Phone_WhenLongerThan30_ShouldHaveValidationError()
    {
        var request = ValidRequest() with
        {
            Phone = new string('1', 31)
        };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Phone);
    }

    // TEST 12: Phone exactly 30 characters
    [Fact]
    public void Phone_WhenExactly30_ShouldPassValidation()
    {
        var request = ValidRequest() with
        {
            Phone = new string('1', 30)
        };

        var result = _validator.TestValidate(request);

        result.ShouldNotHaveValidationErrorFor(x => x.Phone);
    }

    // TEST 13: Phone is optional
    [Fact]
    public void Phone_WhenNull_ShouldPassValidation()
    {
        var request = ValidRequest() with { Phone = null };

        var result = _validator.TestValidate(request);

        result.ShouldNotHaveValidationErrorFor(x => x.Phone);
    }

    // TEST 14: Inactive client is valid
    [Fact]
    public void IsActive_WhenFalse_ShouldPassValidation()
    {
        var request = ValidRequest() with { IsActive = false };

        var result = _validator.TestValidate(request);

        result.ShouldNotHaveAnyValidationErrors();
    }

    // TEST 15: Address is optional
    [Fact]
    public void Address_WhenNull_ShouldPassValidation()
    {
        var request = ValidRequest() with { Address = null };

        var result = _validator.TestValidate(request);

        result.ShouldNotHaveAnyValidationErrors();
    }
}
