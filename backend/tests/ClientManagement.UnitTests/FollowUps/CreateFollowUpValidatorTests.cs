
using ClientManagement.Api.Features.FollowUps.DTOs;
using ClientManagement.Api.Features.FollowUps.Validators;
using FluentValidation.TestHelper;
using Xunit;

namespace ClientManagement.UnitTests.FollowUps;

public class CreateFollowUpValidatorTests
{
    private readonly CreateFollowUpValidator _validator = new();

    // Reusable valid request
    private static CreateFollowUpRequest ValidRequest() => new(
        ClientId: 1,
        Title: "Call client about project",
        DueDate: new DateTime(2026, 11, 15)
    );

    // TEST 1: Valid request
    [Fact]
    public void ValidFollowUp_ShouldPassValidation()
    {
        // Arrange
        var request = ValidRequest();

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    // TEST 2: ClientId is zero
    [Fact]
    public void ClientId_WhenZero_ShouldHaveValidationError()
    {
        var request = ValidRequest() with { ClientId = 0 };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.ClientId);
    }

    // TEST 3: ClientId is negative
    [Theory]
    [InlineData(-1)]
    [InlineData(-10)]
    [InlineData(-100)]
    public void ClientId_WhenNegative_ShouldHaveValidationError(int clientId)
    {
        var request = ValidRequest() with { ClientId = clientId };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.ClientId);
    }

    // TEST 4: ClientId is positive
    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(100)]
    public void ClientId_WhenPositive_ShouldPassValidation(int clientId)
    {
        var request = ValidRequest() with { ClientId = clientId };

        var result = _validator.TestValidate(request);

        result.ShouldNotHaveValidationErrorFor(x => x.ClientId);
    }

    // TEST 5: Title is empty
    [Fact]
    public void Title_WhenEmpty_ShouldHaveValidationError()
    {
        var request = ValidRequest() with { Title = "" };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Title);
    }

    // TEST 6: Title contains only whitespace
    [Fact]
    public void Title_WhenWhitespace_ShouldHaveValidationError()
    {
        var request = ValidRequest() with { Title = "   " };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Title);
    }

    // TEST 7: Title exceeds 200 characters
    [Fact]
    public void Title_WhenLongerThan200_ShouldHaveValidationError()
    {
        var request = ValidRequest() with
        {
            Title = new string('A', 201)
        };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Title);
    }

    // TEST 8: Title is exactly 200 characters
    [Fact]
    public void Title_WhenExactly200_ShouldPassValidation()
    {
        var request = ValidRequest() with
        {
            Title = new string('A', 200)
        };

        var result = _validator.TestValidate(request);

        result.ShouldNotHaveValidationErrorFor(x => x.Title);
    }

    // TEST 9: DueDate is default DateTime
    [Fact]
    public void DueDate_WhenDefault_ShouldHaveValidationError()
    {
        var request = ValidRequest() with
        {
            DueDate = default
        };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.DueDate);
    }

    // TEST 10: DueDate is in the future
    [Fact]
    public void DueDate_WhenFuture_ShouldPassValidation()
    {
        var request = ValidRequest() with
        {
            DueDate = new DateTime(2027, 1, 15)
        };

        var result = _validator.TestValidate(request);

        result.ShouldNotHaveValidationErrorFor(x => x.DueDate);
    }

    // TEST 11: DueDate is in the past
    [Fact]
    public void DueDate_WhenPast_ShouldPassValidation()
    {
        var request = ValidRequest() with
        {
            DueDate = new DateTime(2020, 1, 15)
        };

        var result = _validator.TestValidate(request);

        result.ShouldNotHaveValidationErrorFor(x => x.DueDate);
    }

    // TEST 12: DueDate is today
    [Fact]
    public void DueDate_WhenToday_ShouldPassValidation()
    {
        var request = ValidRequest() with
        {
            DueDate = DateTime.Today
        };

        var result = _validator.TestValidate(request);

        result.ShouldNotHaveValidationErrorFor(x => x.DueDate);
    }

    // TEST 13: All required fields are invalid
    [Fact]
    public void MultipleInvalidFields_ShouldHaveMultipleValidationErrors()
    {
        var request = new CreateFollowUpRequest(
            ClientId: 0,
            Title: "",
            DueDate: default
        );

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.ClientId);
        result.ShouldHaveValidationErrorFor(x => x.Title);
        result.ShouldHaveValidationErrorFor(x => x.DueDate);
    }

    // TEST 14: DueDate returns the correct error message
    [Fact]
    public void DueDate_WhenDefault_ShouldReturnRequiredMessage()
    {
        var request = ValidRequest() with
        {
            DueDate = default
        };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.DueDate)
              .WithErrorMessage("Due date is required.");
    }
}
