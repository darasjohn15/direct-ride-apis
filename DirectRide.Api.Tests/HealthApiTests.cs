using System.Net;
using System.Net.Http.Json;
using FluentAssertions;

namespace DirectRide.Api.Tests;

public class HealthApiTests : IClassFixture<CustomWebApplicationFactory>
{
    private const string AllowedOrigin = "http://direct-ride-frontend-dev.s3-website-us-east-1.amazonaws.com";
    private readonly HttpClient _client;

    public HealthApiTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetHealth_ShouldReturnHealthy()
    {
        var response = await _client.GetAsync("/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<HealthResponse>();
        body.Should().NotBeNull();
        body!.Status.Should().Be("Healthy");
    }

    [Fact]
    public async Task GetHealth_WithAllowedOrigin_ShouldReturnCorsHeader()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/health");
        request.Headers.Add("Origin", AllowedOrigin);

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.GetValues("Access-Control-Allow-Origin")
            .Should().ContainSingle()
            .Which.Should().Be(AllowedOrigin);
    }

    [Fact]
    public async Task OptionsHealth_WithAllowedOrigin_ShouldAllowPreflight()
    {
        using var request = new HttpRequestMessage(HttpMethod.Options, "/health");
        request.Headers.Add("Origin", AllowedOrigin);
        request.Headers.Add("Access-Control-Request-Method", "GET");

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        response.Headers.GetValues("Access-Control-Allow-Origin")
            .Should().ContainSingle()
            .Which.Should().Be(AllowedOrigin);
        response.Headers.GetValues("Access-Control-Allow-Methods")
            .Should().Contain(methods => methods.Contains("GET", StringComparison.OrdinalIgnoreCase));
    }

    private sealed class HealthResponse
    {
        public string Status { get; set; } = string.Empty;
    }
}
