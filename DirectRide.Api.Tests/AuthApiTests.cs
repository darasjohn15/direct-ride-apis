using System.Net;
using System.Net.Http.Json;
using DirectRide.Api.DTOs;
using DirectRide.Api.DTOs.Auth;
using FluentAssertions;

namespace DirectRide.Api.Tests;

public class AuthApiTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AuthApiTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task PostLogin_ShouldReturnTokenAndUser_WhenCredentialsAreValid()
    {
        await _client.PostAsJsonAsync("/users", new CreateUserDto
        {
            FirstName = "Login",
            LastName = "Rider",
            Email = "login-rider@test.com",
            PhoneNumber = "555-111-2222",
            Role = 0,
            Password = "CorrectHorse123!"
        });

        var response = await _client.PostAsJsonAsync("/auth/login", new LoginDto
        {
            Email = "login-rider@test.com",
            Password = "CorrectHorse123!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var login = await response.Content.ReadFromJsonAsync<LoginResponse>();

        login.Should().NotBeNull();
        login!.Token.Should().NotBeNullOrWhiteSpace();
        login.User.Email.Should().Be("login-rider@test.com");
        login.User.FirstName.Should().Be("Login");
        login.User.LastName.Should().Be("Rider");
        login.User.Role.Should().Be("Rider");
    }

    [Fact]
    public async Task PostLogin_ShouldReturnUnauthorized_WhenEmailDoesNotExist()
    {
        var response = await _client.PostAsJsonAsync("/auth/login", new LoginDto
        {
            Email = "missing-user@test.com",
            Password = "CorrectHorse123!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PostLogin_ShouldReturnUnauthorized_WhenPasswordIsInvalid()
    {
        await _client.PostAsJsonAsync("/users", new CreateUserDto
        {
            FirstName = "Password",
            LastName = "Check",
            Email = "password-check@test.com",
            PhoneNumber = "555-333-4444",
            Role = 1,
            Password = "CorrectHorse123!"
        });

        var response = await _client.PostAsJsonAsync("/auth/login", new LoginDto
        {
            Email = "password-check@test.com",
            Password = "WrongPassword123!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private sealed class LoginResponse
    {
        public string Token { get; set; } = string.Empty;

        public LoginUserResponse User { get; set; } = new();
    }

    private sealed class LoginUserResponse
    {
        public Guid Id { get; set; }

        public string FirstName { get; set; } = string.Empty;

        public string LastName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string Role { get; set; } = string.Empty;
    }
}
