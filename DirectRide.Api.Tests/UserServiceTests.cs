using DirectRide.Api.Models;
using DirectRide.Api.Repositories;
using DirectRide.Api.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;

namespace DirectRide.Api.Tests;

public class UserServiceTests
{
    [Fact]
    public async Task UploadProfilePhotoAsync_ShouldRejectEmptyContent()
    {
        var service = CreateService();

        var result = await service.UploadProfilePhotoAsync(
            Guid.NewGuid(),
            [],
            "image/jpeg");

        result.Status.Should().Be(UserServiceResultStatus.BadRequest);
        result.Error.Should().Be("A profile photo is required.");
    }

    [Fact]
    public async Task UploadProfilePhotoAsync_ShouldRejectOversizedContent()
    {
        var service = CreateService();
        var content = new byte[UserService.MaxProfilePhotoSize + 1];

        var result = await service.UploadProfilePhotoAsync(
            Guid.NewGuid(),
            content,
            "image/jpeg");

        result.Status.Should().Be(UserServiceResultStatus.BadRequest);
        result.Error.Should().Be("Profile photos cannot exceed 5 MB.");
    }

    [Fact]
    public async Task UploadProfilePhotoAsync_ShouldRejectContentWithInvalidImageSignature()
    {
        var service = CreateService();

        var result = await service.UploadProfilePhotoAsync(
            Guid.NewGuid(),
            [1, 2, 3],
            "image/jpeg");

        result.Status.Should().Be(UserServiceResultStatus.BadRequest);
        result.Error.Should().Be("Only JPEG, PNG, and WebP images are supported.");
    }

    private static UserService CreateService()
    {
        return new UserService(
            new StubUserRepository(),
            new PasswordHasher<User>(),
            new StubFileStorageService());
    }

    private sealed class StubUserRepository : IUserRepository
    {
        public IQueryable<User> Query() => Array.Empty<User>().AsQueryable();

        public Task<User?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<User?>(null);

        public void Add(User user)
        {
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class StubFileStorageService : IFileStorageService
    {
        public Task<string> UploadProfilePhotoAsync(
            Guid userId,
            byte[] fileContent,
            string contentType,
            CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Invalid content must not reach file storage.");

        public Task DeleteProfilePhotoAsync(
            string key,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public string GetProfilePhotoUrl(string key) => key;
    }
}
