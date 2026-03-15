using System.Diagnostics.CodeAnalysis;
using Serilog.Core;
using Serilog.Events;
using TravelAgency.Identity.Application.DTOs;
using TravelAgency.Identity.Application.Features.Auth.Commands.Login;
using TravelAgency.Identity.Application.Features.Auth.Commands.Register;
using TravelAgency.Identity.Application.Features.Profile.Commands.UpdateProfile;

namespace TravelAgency.Identity.API.Logging;

/// <summary>
/// Serilog destructuring policy that masks PII in auth DTOs. Ensures email is partially masked
/// (first 2 chars + ***) and password is never logged.
/// </summary>
public sealed class PiiMaskingDestructuringPolicy : IDestructuringPolicy
{
    private const string PasswordRedacted = "[REDACTED]";

    public bool TryDestructure(object value, ILogEventPropertyValueFactory propertyValueFactory, [MaybeNullWhen(false)] out LogEventPropertyValue result)
    {
        result = null;

        if (value is LoginRequest loginRequest)
        {
            result = CreateLoginRequestStructure(loginRequest);
            return true;
        }

        if (value is RegisterRequest registerRequest)
        {
            result = CreateRegisterRequestStructure(registerRequest);
            return true;
        }

        if (value is LoginCommand loginCommand)
        {
            result = CreateLoginCommandStructure(loginCommand);
            return true;
        }

        if (value is RegisterCommand registerCommand)
        {
            result = CreateRegisterCommandStructure(registerCommand);
            return true;
        }

        if (value is UpdateProfileRequest updateProfileRequest)
        {
            result = CreateUpdateProfileRequestStructure(updateProfileRequest);
            return true;
        }

        if (value is UpdateProfileCommand updateProfileCommand)
        {
            result = CreateUpdateProfileCommandStructure(updateProfileCommand);
            return true;
        }

        return false;
    }

    private static StructureValue CreateLoginRequestStructure(LoginRequest request)
    {
        var props = new List<LogEventProperty>
        {
            new("Email", new ScalarValue(MaskEmail(request.Email))),
            new("Password", new ScalarValue(PasswordRedacted))
        };
        return new StructureValue(props);
    }

    private static StructureValue CreateRegisterRequestStructure(RegisterRequest request)
    {
        var props = new List<LogEventProperty>
        {
            new("Email", new ScalarValue(MaskEmail(request.Email))),
            new("Password", new ScalarValue(PasswordRedacted)),
            new("FirstName", new ScalarValue(MaskName(request.FirstName))),
            new("LastName", new ScalarValue(MaskName(request.LastName))),
            new("Phone", new ScalarValue(MaskPhone(request.Phone)))
        };
        return new StructureValue(props);
    }

    private static StructureValue CreateLoginCommandStructure(LoginCommand command)
    {
        var maskedRequest = CreateLoginRequestStructure(command.Request);
        var props = new List<LogEventProperty> { new("Request", maskedRequest) };
        return new StructureValue(props);
    }

    private static StructureValue CreateRegisterCommandStructure(RegisterCommand command)
    {
        var maskedRequest = CreateRegisterRequestStructure(command.Request);
        var props = new List<LogEventProperty> { new("Request", maskedRequest) };
        return new StructureValue(props);
    }

    private static StructureValue CreateUpdateProfileRequestStructure(UpdateProfileRequest request)
    {
        var props = new List<LogEventProperty>
        {
            new("FirstName", new ScalarValue(MaskName(request.FirstName))),
            new("LastName", new ScalarValue(MaskName(request.LastName))),
            new("Phone", new ScalarValue(MaskPhone(request.Phone)))
        };
        return new StructureValue(props);
    }

    private static StructureValue CreateUpdateProfileCommandStructure(UpdateProfileCommand command)
    {
        var maskedRequest = CreateUpdateProfileRequestStructure(command.Request);
        var props = new List<LogEventProperty> { new("Request", maskedRequest) };
        return new StructureValue(props);
    }

    /// <summary>
    /// Masks email: first 2 chars of local part + ***, domain redacted (e.g. "us***@***").
    /// </summary>
    private static string MaskEmail(string? email)
    {
        if (string.IsNullOrEmpty(email))
            return "***";

        var atIndex = email.IndexOf('@');
        if (atIndex <= 0)
            return "***@***";

        var localPart = email[..atIndex];
        var visibleChars = Math.Min(2, localPart.Length);
        var maskedLocal = localPart.Length <= 2
            ? new string('*', localPart.Length)
            : localPart[..visibleChars] + "***";

        return maskedLocal + "@***";
    }

    /// <summary>
    /// Masks name: first 2 chars + *** (e.g. "Jo***" for "John").
    /// </summary>
    private static string MaskName(string? name)
    {
        if (string.IsNullOrEmpty(name))
            return "***";

        if (name.Length <= 2)
            return new string('*', name.Length);

        return name[..2] + "***";
    }

    /// <summary>
    /// Masks phone: full redaction to avoid PII exposure.
    /// </summary>
    private static string MaskPhone(string? phone)
    {
        if (string.IsNullOrEmpty(phone))
            return "***";

        return "***";
    }
}
