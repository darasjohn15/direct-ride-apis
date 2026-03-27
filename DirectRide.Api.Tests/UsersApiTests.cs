using System.Net;
using System.Net.Http.Json;
using DirectRide.Api.DTOs;
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
            Role = 1
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
    public async Task GetUsers_ShouldReturnUsers()
    {
        var createUser = new CreateUserDto
        {
            FirstName = "Test",
            LastName = "User",
            Email = "testuser@test.com",
            PhoneNumber = "555-123-4567",
            Role = 0
        };

        await _client.PostAsJsonAsync("/users", createUser);

        var response = await _client.GetAsync("/users");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var users = await response.Content.ReadFromJsonAsync<List<UserResponseDto>>();

        users.Should().NotBeNull();
        users!.Count.Should().BeGreaterThan(0);
        users.Should().Contain(u => u.Email == "testuser@test.com");
    }
}