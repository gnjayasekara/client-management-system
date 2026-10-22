
using ClientManagement.Api.Features.FollowUps.DTOs;
using ClientManagement.Api.Features.FollowUps.Validators;
using FluentValidation.TestHelper;
using Xunit;

namespace ClientManagement.UnitTests.FollowUps;

public class UpdateFollowUpValidatorTests
{
    private readonly UpdateFollowUpValidator _validator = new();

    // Reusable valid update request
    private static UpdateFollowUpRequest ValidRequest() => new(
        ClientId: 1,
        Title: "Follow up with client",
        DueDate: new DateTime(2026, 11, 15),
        IsCompleted: false
    );

    // TEST 1: Valid update request
    [Fact]
    public void ValidFollowUp_ShouldPassValidation()
    {
        var request = ValidRequest();

        var result = _validator.TestValidate(request);

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

    // TEST 3: Negative ClientId
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

    // TEST 4: Positive ClientId
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

    // TEST 5: Empty title
    [Fact]
    public void Title_WhenEmpty_ShouldHaveValidationError()
    {
        var request = ValidRequest() with { Title = "" };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Title);
    }

    // TEST 6: Whitespace title
    [Fact]
    public void Title_WhenWhitespace_ShouldHaveValidationError()
    {
        var request = ValidRequest() with { Title = "   " };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Title);
    }

    // TEST 7: Title longer than 200 characters
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

    // TEST 8: Title exactly 200 characters
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

    // TEST 9: Default DueDate
    [Fact]
    public void DueDate_WhenDefault_ShouldHaveValidationError()
    {
        var request = ValidRequest() with { DueDate = default };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.DueDate);
    }

    // TEST 10: Future DueDate
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

    // TEST 11: Past DueDate
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

    // TEST 12: Today's date
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

    // TEST 13: IsCompleted = true
    [Fact]
    public void IsCompleted_WhenTrue_ShouldPassValidation()
    {
        var request = ValidRequest() with { IsCompleted = true };

        var result = _validator.TestValidate(request);

        result.ShouldNotHaveAnyValidationErrors();
    }

    // TEST 14: IsCompleted = false
    [Fact]
    public void IsCompleted_WhenFalse_ShouldPassValidation()
    {
        var request = ValidRequest() with { IsCompleted = false };

        var result = _validator.TestValidate(request);

        result.ShouldNotHaveAnyValidationErrors();
    }

    // TEST 15: Multiple invalid fields
    [Fact]
    public void MultipleInvalidFields_ShouldHaveMultipleValidationErrors()
    {
        var request = new UpdateFollowUpRequest(
            ClientId: 0,
            Title: "",
            DueDate: default,
            IsCompleted: false
        );

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.ClientId);
        result.ShouldHaveValidationErrorFor(x => x.Title);
        result.ShouldHaveValidationErrorFor(x => x.DueDate);
    }

    // TEST 16: Verify DueDate error message
    [Fact]
    public void DueDate_WhenDefault_ShouldReturnRequiredMessage()
    {
        var request = ValidRequest() with { DueDate = default };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.DueDate)
              .WithErrorMessage("Due date is required.");
    }
}
