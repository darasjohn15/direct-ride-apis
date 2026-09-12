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
            FirstName = "Sample",
            LastName = "Driver",
            Email = "sample-driver@test.com",
            PhoneNumber = "555-555-5555",
            Role = 1,
            Password = "CorrectHorse123!"
        };

        var response = await _client.PostAsJsonAsync("/users", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var user = await response.Content.ReadFromJsonAsync<UserResponseDto>();

        user.Should().NotBeNull();
        user!.FirstName.Should().Be("Sample");
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

        var paginatedUsers = await response.Content.ReadFromJsonAsync<PaginatedResponseDto<UserResponseDto>>();

        paginatedUsers.Should().NotBeNull();
        paginatedUsers!.Items.Count.Should().BeGreaterThan(0);
        paginatedUsers.TotalItems.Should().BeGreaterThan(0);
        paginatedUsers.Items.Should().Contain(u =>
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
    public async Task PutUserById_ShouldUpdateUser_WhenUserExists()
    {
        var createResponse = await _client.PostAsJsonAsync("/users", new CreateUserDto
        {
            FirstName = "Original",
            LastName = "Driver",
            Email = "put-original@test.com",
            PhoneNumber = "555-111-2222",
            Role = 1,
            Password = "CorrectHorse123!"
        });
        var createdUser = await createResponse.Content.ReadFromJsonAsync<UserResponseDto>();

        var response = await _client.PutAsJsonAsync($"/users/{createdUser!.Id}", new UpdateUserDto
        {
            FirstName = "Updated",
            LastName = "Rider",
            Email = "put-updated@test.com",
            PhoneNumber = "555-333-4444",
            Role = 0,
            BaseFare = 12.50m
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var user = await response.Content.ReadFromJsonAsync<UserResponseDto>();

        user.Should().NotBeNull();
        user!.Id.Should().Be(createdUser.Id);
        user.FirstName.Should().Be("Updated");
        user.LastName.Should().Be("Rider");
        user.Email.Should().Be("put-updated@test.com");
        user.PhoneNumber.Should().Be("555-333-4444");
        user.Role.Should().Be("Rider");
        user.BaseFare.Should().Be(12.50m);
        user.CreatedAt.Should().Be(createdUser.CreatedAt);
    }

    [Fact]
    public async Task PutUserById_ShouldReturnNotFound_WhenUserDoesNotExist()
    {
        var response = await _client.PutAsJsonAsync($"/users/{Guid.NewGuid()}", new UpdateUserDto
        {
            FirstName = "Missing",
            LastName = "User",
            Email = "put-missing@test.com",
            PhoneNumber = "555-000-1111",
            Role = 0,
            BaseFare = 10.00m
        });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PatchUserById_ShouldUpdateOnlyProvidedFields_WhenUserExists()
    {
        var createResponse = await _client.PostAsJsonAsync("/users", new CreateUserDto
        {
            FirstName = "Partial",
            LastName = "Driver",
            Email = "patch-original@test.com",
            PhoneNumber = "555-777-8888",
            Role = 1,
            Password = "CorrectHorse123!"
        });
        var createdUser = await createResponse.Content.ReadFromJsonAsync<UserResponseDto>();

        var response = await _client.PatchAsJsonAsync($"/users/{createdUser!.Id}", new PatchUserDto
        {
            FirstName = "Patched",
            BaseFare = 22.75m
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var user = await response.Content.ReadFromJsonAsync<UserResponseDto>();

        user.Should().NotBeNull();
        user!.Id.Should().Be(createdUser.Id);
        user.FirstName.Should().Be("Patched");
        user.LastName.Should().Be("Driver");
        user.Email.Should().Be("patch-original@test.com");
        user.PhoneNumber.Should().Be("555-777-8888");
        user.Role.Should().Be("Driver");
        user.BaseFare.Should().Be(22.75m);
        user.CreatedAt.Should().Be(createdUser.CreatedAt);
    }

    [Fact]
    public async Task PatchUserById_ShouldReturnNotFound_WhenUserDoesNotExist()
    {
        var response = await _client.PatchAsJsonAsync($"/users/{Guid.NewGuid()}", new PatchUserDto
        {
            FirstName = "Missing"
        });

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
        user!.FirstName.Should().Be("Sample");
        user.LastName.Should().Be("Driver");
        user.Email.Should().Be("sample.driver@directride.com");
        user.PhoneNumber.Should().Be("555-555-5555");
        user.Role.Should().Be(UserRole.Driver);
    }

    [Fact]
    public async Task PutProfilePhoto_ShouldAllowUserToUpdateOwnPhoto()
    {
        var user = await CreateUserAsync("photo-owner@test.com");
        using var request = CreateProfilePhotoRequest(user.Id, user.Id, "Rider");

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task PutProfilePhoto_ShouldForbidUserFromUpdatingAnotherUsersPhoto()
    {
        var user = await CreateUserAsync("photo-target@test.com");
        using var request = CreateProfilePhotoRequest(user.Id, Guid.NewGuid(), "Rider");

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task PutProfilePhoto_ShouldRejectContentThatIsNotAnImage()
    {
        var user = await CreateUserAsync("invalid-photo@test.com");
        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent([1, 2, 3]);
        fileContent.Headers.ContentType = new("image/jpeg");
        content.Add(fileContent, "file", "profile.jpg");
        using var request = new HttpRequestMessage(
            HttpMethod.Put,
            $"/users/{user.Id}/profile-photo")
        {
            Content = content
        };
        request.Headers.Add(TestAuthHandler.UserIdHeaderName, user.Id.ToString());
        request.Headers.Add(TestAuthHandler.UserRoleHeaderName, "Rider");

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task DeleteProfilePhoto_ShouldForbidUserFromDeletingAnotherUsersPhoto()
    {
        var user = await CreateUserAsync("photo-delete-target@test.com");
        using var request = new HttpRequestMessage(
            HttpMethod.Delete,
            $"/users/{user.Id}/profile-photo");
        request.Headers.Add(TestAuthHandler.UserIdHeaderName, Guid.NewGuid().ToString());
        request.Headers.Add(TestAuthHandler.UserRoleHeaderName, "Rider");

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private async Task<UserResponseDto> CreateUserAsync(string email)
    {
        var response = await _client.PostAsJsonAsync("/users", new CreateUserDto
        {
            FirstName = "Photo",
            LastName = "User",
            Email = email,
            PhoneNumber = "555-555-1212",
            Role = 0,
            Password = "CorrectHorse123!"
        });

        return (await response.Content.ReadFromJsonAsync<UserResponseDto>())!;
    }

    private static HttpRequestMessage CreateProfilePhotoRequest(
        Guid targetUserId,
        Guid authenticatedUserId,
        string role)
    {
        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent([0xFF, 0xD8, 0xFF, 0xE0]);
        fileContent.Headers.ContentType = new("image/jpeg");
        content.Add(fileContent, "file", "profile.jpg");

        var request = new HttpRequestMessage(
            HttpMethod.Put,
            $"/users/{targetUserId}/profile-photo")
        {
            Content = content
        };
        request.Headers.Add(TestAuthHandler.UserIdHeaderName, authenticatedUserId.ToString());
        request.Headers.Add(TestAuthHandler.UserRoleHeaderName, role);

        return request;
    }

    private Task<HttpResponseMessage> GetUsersMeAsync(Guid userId)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/users/me");
        request.Headers.Add(TestAuthHandler.UserIdHeaderName, userId.ToString());

        return _client.SendAsync(request);
    }
}
