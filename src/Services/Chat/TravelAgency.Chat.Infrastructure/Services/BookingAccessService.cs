using System.Net.Http.Headers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using TravelAgency.Chat.Application.Abstractions;

namespace TravelAgency.Chat.Infrastructure.Services;

/// <summary>
/// Verifies booking access by calling the Booking service API with the forwarded JWT.
/// Returns true on 200, false on 403 or 404.
/// </summary>
public class BookingAccessService : IBookingAccessService
{
    private readonly HttpClient _httpClient;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly string _bookingServiceUrl;

    public BookingAccessService(
        HttpClient httpClient,
        IHttpContextAccessor httpContextAccessor,
        IConfiguration configuration)
    {
        _httpClient = httpClient;
        _httpContextAccessor = httpContextAccessor;
        _bookingServiceUrl = configuration["Services:BookingServiceUrl"]
            ?? configuration["GrpcClients:BookingServiceUrl"]
            ?? "http://localhost:5030";
    }

    public async Task<bool> CanAccessBookingAsync(Guid bookingId, string? authorizationHeader = null, CancellationToken ct = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"{_bookingServiceUrl.TrimEnd('/')}/bookings/{bookingId}");

        var authHeader = authorizationHeader
            ?? _httpContextAccessor.HttpContext?.Request.Headers.Authorization.FirstOrDefault();
        if (!string.IsNullOrEmpty(authHeader))
        {
            request.Headers.Authorization = AuthenticationHeaderValue.Parse(authHeader);
        }

        var response = await _httpClient.SendAsync(request, ct);
        return response.IsSuccessStatusCode;
    }
}
