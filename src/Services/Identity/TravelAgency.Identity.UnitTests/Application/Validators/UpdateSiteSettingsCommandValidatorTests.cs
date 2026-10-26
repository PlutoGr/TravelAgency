using TravelAgency.Identity.Application.DTOs;
using TravelAgency.Identity.Application.Features.SiteSettings.Commands.UpdateSiteSettings;

namespace TravelAgency.Identity.UnitTests.Application.Validators;

public class UpdateSiteSettingsCommandValidatorTests
{
    private readonly UpdateSiteSettingsCommandValidator _validator = new();

    [Fact]
    public async Task Validate_WithPublicFields_Passes()
    {
        var command = Command(
            siteName: "Агентство",
            phone: "+79991234567",
            email: "user@example.com",
            links:
            [
                new SocialLinkRequest("Сайт", "https://example.com/agency")
            ]);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_WithEmptyOptionalFields_Passes()
    {
        var command = Command(siteName: "TravelAgency");

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Validate_WithBlankSiteName_Fails(string? siteName)
    {
        var result = await _validator.ValidateAsync(Command(siteName: siteName));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == "siteName");
    }

    [Fact]
    public async Task Validate_WithSiteNameLongerThan120_Fails()
    {
        var result = await _validator.ValidateAsync(Command(siteName: new string('а', 121)));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == "siteName");
    }

    [Theory]
    [InlineData("8999")]
    [InlineData("+0123")]
    [InlineData("phone")]
    [InlineData("+7 999 123-45-67")]
    public async Task Validate_WithPhoneNotE164_Fails(string phone)
    {
        var result = await _validator.ValidateAsync(Command(siteName: "TravelAgency", phone: phone));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == "supportPhone");
    }

    [Fact]
    public async Task Validate_WithEmailNotAnEmail_Fails()
    {
        var result = await _validator.ValidateAsync(Command(siteName: "TravelAgency", email: "not-an-email"));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == "contactEmail");
    }

    [Theory]
    [InlineData("http://example.com")]
    [InlineData("javascript:alert(1)")]
    [InlineData("https://")]
    public async Task Validate_WithLinkThatIsNotHttps_Fails(string url)
    {
        var command = Command(
            siteName: "TravelAgency",
            links: [new SocialLinkRequest("Ссылка", url)]);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == "socialLinks[0].url");
    }

    [Fact]
    public async Task Validate_WithBannerTextLongerThan500_Fails()
    {
        var result = await _validator.ValidateAsync(Command(
            siteName: "TravelAgency",
            bannerText: new string('б', 501)));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == "bannerText");
    }

    [Fact]
    public async Task Validate_WithMoreThanTenLinks_Fails()
    {
        var links = Enumerable.Range(0, 11)
            .Select(index => new SocialLinkRequest($"L{index}", $"https://example.com/{index}"))
            .ToList();

        var result = await _validator.ValidateAsync(Command(siteName: "TravelAgency", links: links));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == "socialLinks");
    }

    private static UpdateSiteSettingsCommand Command(
        string? siteName,
        string? phone = null,
        string? email = null,
        string? bannerText = null,
        IReadOnlyList<SocialLinkRequest>? links = null) =>
        new(new UpdateSiteSettingsRequest(
            siteName,
            phone,
            email,
            null,
            null,
            links,
            bannerText,
            false));
}
