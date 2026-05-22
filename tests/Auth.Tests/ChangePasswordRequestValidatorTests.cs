using Auth.Api.Validation;
using Auth.Application.Auth.Dto;

namespace Auth.Tests;

public sealed class ChangePasswordRequestValidatorTests
{
    [Fact]
    public void Validate_ShouldRejectInvalidNewPassword()
    {
        var validator = new ChangePasswordRequestValidator();

        var result = validator.Validate(new ChangePasswordRequest("Password1", "short"));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(ChangePasswordRequest.NewPassword));
    }

    [Fact]
    public void Validate_ShouldAcceptValidRequest()
    {
        var validator = new ChangePasswordRequestValidator();

        var result = validator.Validate(new ChangePasswordRequest("Password1", "NewPassword1"));

        Assert.True(result.IsValid);
    }
}
