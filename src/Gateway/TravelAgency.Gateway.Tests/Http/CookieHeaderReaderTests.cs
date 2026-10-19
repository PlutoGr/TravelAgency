using Microsoft.AspNetCore.Http;
using Xunit;
using TravelAgency.Gateway.Http;

namespace TravelAgency.Gateway.Tests.Http;

public class CookieHeaderReaderTests
{
    private const string Token = "ab+cd/ef==";
    private static readonly string EncodedToken = Uri.EscapeDataString(Token);

    [Fact]
    public void ReadFirst_WhenValueIsPercentEncoded_ReturnsOriginalToken()
    {
        var header = $"refresh_token={EncodedToken}";

        var value = CookieHeaderReader.ReadFirst(header, "refresh_token");

        Assert.Equal(Token, value);
        Assert.Equal("ab%2Bcd%2Fef%3D%3D", EncodedToken);
    }

    [Fact]
    public void ReadFirst_WhenValueHasNoEscapes_ReturnsItUnchanged()
    {
        Assert.Equal("from-auth-path", CookieHeaderReader.ReadFirst("refresh_token=from-auth-path", "refresh_token"));
    }

    [Fact]
    public void ReadFirst_WhenPlusIsLiteral_DoesNotTurnItIntoSpace()
    {
        Assert.Equal(Token, CookieHeaderReader.ReadFirst($"refresh_token={Token}", "refresh_token"));
    }

    [Fact]
    public void ReadFirst_WhenQuotedValueIsPercentEncoded_StripsQuotesAndDecodes()
    {
        var header = $"refresh_token=\"{EncodedToken}\"";

        Assert.Equal(Token, CookieHeaderReader.ReadFirst(header, "refresh_token"));
    }

    [Fact]
    public void ReadFirst_WhenTwoCookies_DecodesTheFirstAndIgnoresTheLegacyPath()
    {
        var header = $"refresh_token={EncodedToken}; refresh_token=old%2Bpath";

        Assert.Equal(Token, CookieHeaderReader.ReadFirst(header, "refresh_token"));
    }

    [Fact]
    public void RequestCookies_WhenTwoCookies_KeepsTheLaterShorterPath()
    {
        var header = $"refresh_token={EncodedToken}; refresh_token=old%2Bpath";
        var context = new DefaultHttpContext();
        context.Request.Headers.Cookie = header;

        Assert.Equal("old+path", context.Request.Cookies["refresh_token"]);
        Assert.Equal(Token, CookieHeaderReader.ReadFirst(header, "refresh_token"));
    }

    [Theory]
    [InlineData("%")]
    [InlineData("%2")]
    [InlineData("%ZZ")]
    [InlineData("%2G")]
    [InlineData("ab%")]
    [InlineData("%E0%A4%A")]
    public void ReadFirst_WhenPercentEncodingIsBroken_DoesNotThrow(string raw)
    {
        var header = $"refresh_token={raw}";

        var exception = Record.Exception(() => CookieHeaderReader.ReadFirst(header, "refresh_token"));

        Assert.Null(exception);
        Assert.Equal(SafeUnescape(raw), CookieHeaderReader.ReadFirst(header, "refresh_token"));
    }

    [Fact]
    public void ReadFirst_WhenFirstCookieEncodingIsBroken_DoesNotFallThroughToLegacyCookie()
    {
        // The longer path is first. Its value is kept even when the percent-sequence is broken.
        var header = "refresh_token=%ZZ; refresh_token=ab%2Bcd%2Fef%3D%3D";

        var exception = Record.Exception(() => CookieHeaderReader.ReadFirst(header, "refresh_token"));

        Assert.Null(exception);
        Assert.Equal("%ZZ", CookieHeaderReader.ReadFirst(header, "refresh_token"));
    }

    [Fact]
    public void ReadFirst_WhenHeaderIsMissing_ReturnsNull()
    {
        Assert.Null(CookieHeaderReader.ReadFirst(null, "refresh_token"));
        Assert.Null(CookieHeaderReader.ReadFirst("access_token=abc", "refresh_token"));
        Assert.Null(CookieHeaderReader.ReadFirst("refresh_token=", "refresh_token"));
    }

    private static string? SafeUnescape(string raw)
    {
        try
        {
            var decoded = Uri.UnescapeDataString(raw);
            return decoded.Length == 0 ? null : decoded;
        }
        catch (UriFormatException)
        {
            return null;
        }
    }
}
