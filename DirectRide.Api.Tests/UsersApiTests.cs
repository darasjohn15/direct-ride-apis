using System.Net;
using System.Net.Http.Json;
using DirectRide.Api.DTOs;
using DirectRide.Api.DTOs.Auth;
using DirectRide.Api.Models;
using FluentAssertions;

namespace DirectRide.Api.Tests;

public class UsersApiTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public UsersApiTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task PostUsers_ShouldCreateUser()
    {
        var request = new CreateUserDto
        {
            FirstName = "Razzo",
            LastName = "Driver",
            Email = "razzo@test.com",
            PhoneNumber = "555-555-5555",
            Role = 1,
            Password = "CorrectHorse123!"
        };

        var response = await _client.PostAsJsonAsync("/users", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var user = await response.Content.ReadFromJsonAsync<UserResponseDto>();

        user.Should().NotBeNull();
        user!.FirstName.Should().Be("Razzo");
        user.LastName.Should().Be("Driver");
        user.Role.Should().Be("Driver");
    }

    [Fact]
    public async Task PostUsers_ShouldHashPassword_ForLogin()
    {
        var request = new CreateUserDto
        {
            FirstName = "Hash",
            LastName = "Check",
            Email = "hash-check@test.com",
            PhoneNumber = "555-555-1111",
            Role = 0,
            Password = "CorrectHorse123!"
        };

        await _client.PostAsJsonAsync("/users", request);

        var response = await _client.PostAsJsonAsync("/auth/login", new LoginDto
        {
            Email = "hash-check@test.com",
            Password = "CorrectHorse123!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetUsers_ShouldReturnUsers()
    {
        var createUser = new CreateUserDto
        {
            FirstName = "Test",
            LastName = "User",
            Email = "testuser@test.com",
            PhoneNumber = "555-123-4567",
            Role = 0,
            Password = "CorrectHorse123!"
        };

        await _client.PostAsJsonAsync("/users", createUser);

        var response = await _client.GetAsync("/users");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var users = await response.Content.ReadFromJsonAsync<List<UserResponseDto>>();

        users.Should().NotBeNull();
        users!.Count.Should().BeGreaterThan(0);
        users.Should().Contain(u =>
            u.Email == "testuser@test.com"
            && u.PhoneNumber == "555-123-4567"
            && u.BaseFare == 0.00m);
    }

    [Fact]
    public async Task GetUserById_ShouldReturnUser_WhenUserExists()
    {
        var createUser = new CreateUserDto
        {
            FirstName = "Lookup",
            LastName = "Driver",
            Email = "lookup-driver@test.com",
            PhoneNumber = "555-321-7654",
            Role = 1,
            Password = "CorrectHorse123!"
        };

        var createResponse = await _client.PostAsJsonAsync("/users", createUser);
        var createdUser = await createResponse.Content.ReadFromJsonAsync<UserResponseDto>();

        var response = await _client.GetAsync($"/users/{createdUser!.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var user = await response.Content.ReadFromJsonAsync<UserResponseDto>();

        user.Should().NotBeNull();
        user!.Id.Should().Be(createdUser.Id);
        user.FirstName.Should().Be("Lookup");
        user.LastName.Should().Be("Driver");
        user.Email.Should().Be("lookup-driver@test.com");
        user.PhoneNumber.Should().Be("555-321-7654");
        user.Role.Should().Be("Driver");
        user.BaseFare.Should().Be(0.00m);
    }

    [Fact]
    public async Task GetUserById_ShouldReturnNotFound_WhenUserDoesNotExist()
    {
        var response = await _client.GetAsync($"/users/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetUsersMe_ShouldReturnCurrentUser_WhenAuthenticatedUserExists()
    {
        var createUser = new CreateUserDto
        {
            FirstName = "Current",
            LastName = "Rider",
            Email = "current-rider@test.com",
            PhoneNumber = "555-654-9876",
            Role = 0,
            Password = "CorrectHorse123!"
        };

        var createResponse = await _client.PostAsJsonAsync("/users", createUser);
        var createdUser = await createResponse.Content.ReadFromJsonAsync<UserResponseDto>();

        var response = await GetUsersMeAsync(createdUser!.Id);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var user = await response.Content.ReadFromJsonAsync<UserResponseDto>();

        user.Should().NotBeNull();
        user!.Id.Should().Be(createdUser.Id);
        user.FirstName.Should().Be("Current");
        user.LastName.Should().Be("Rider");
        user.Email.Should().Be("current-rider@test.com");
        user.PhoneNumber.Should().Be("555-654-9876");
        user.Role.Should().Be("Rider");
        user.BaseFare.Should().Be(0.00m);
    }

    [Fact]
    public async Task GetUsersMe_ShouldReturnNotFound_WhenAuthenticatedUserDoesNotExist()
    {
        var response = await GetUsersMeAsync(Guid.NewGuid());

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetUsersMe_ShouldReturnUnauthorized_WhenAuthenticatedUserIdIsNotGuid()
    {
        var response = await _client.GetAsync("/users/me");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetUsersTest_ShouldReturnSampleDriver()
    {
        var response = await _client.GetAsync("/users/test");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var user = await response.Content.ReadFromJsonAsync<User>();

        user.Should().NotBeNull();
        user!.FirstName.Should().Be("Razzo");
        user.LastName.Should().Be("Driver");
        user.Email.Should().Be("razzo@directride.com");
        user.PhoneNumber.Should().Be("555-555-5555");
        user.Role.Should().Be(UserRole.Driver);
    }

    private Task<HttpResponseMessage> GetUsersMeAsync(Guid userId)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/users/me");
        request.Headers.Add(TestAuthHandler.UserIdHeaderName, userId.ToString());

        return _client.SendAsync(request);
    }
}
