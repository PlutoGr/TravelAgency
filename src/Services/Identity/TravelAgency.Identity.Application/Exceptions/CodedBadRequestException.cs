namespace TravelAgency.Identity.Application.Exceptions;

public sealed class CodedBadRequestException : AppException
{
    public string Code { get; }

    public CodedBadRequestException(string code)
        : base("The request is invalid.", 400)
    {
        Code = code;
    }
}
