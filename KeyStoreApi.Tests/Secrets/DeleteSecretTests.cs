using Azure;
using FastEndpoints;
using FluentAssertions;
using KeyStoreApi.Secrets;
using KeyStoreApi.Secrets.Delete;
using KeyStoreApi.Secrets.Service;
using KeyStoreApi.Tests.Fixtures;
using Xunit;

namespace KeyStoreApi.Tests.Secrets;

public class DeleteSecretTests {
    private const string VaultUri = "https://mock.vault";
    private readonly ErrorResponseFactory _err = new(VaultUri);

    [Fact]
    public async Task Happy() {
        // Arrange
        var mock = new MockSecretClient();
        mock.VaultSecrets.Add(new ValueTuple<string, string, bool>("Key1", "Value1", true));

        // Act
        var ep = Factory.Create<DeleteSecretEndpoint>(new SecretService(mock), _err);
        await ep.HandleAsync(new KeyRequest {
            Key = "Key1"
        }, CancellationToken.None);

        // Assert
        ep.HttpContext.Response.StatusCode.Should().Be(204);
    }

    [Fact]
    public async Task NotFound() {
        // Arrange
        var mock = new MockSecretClient {
            ToThrow = new RequestFailedException(404, "", "SecretNotFound", null)
        };

        // Act
        var ep = Factory.Create<DeleteSecretEndpoint>(new SecretService(mock), _err);
        await ep.HandleAsync(new KeyRequest {
            Key = "Key2"
        }, CancellationToken.None);

        // Assert
        ep.HttpContext.Response.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task Cancellation() {
        // Arrange
        var mock = new MockSecretClient();
        mock.VaultSecrets.Add(new ValueTuple<string, string, bool>("Key1", "Value1", true));
        var cancellationTokenSource = new CancellationTokenSource();
        var ctx = cancellationTokenSource.Token;
        await cancellationTokenSource.CancelAsync();

        // Act
        var ep = Factory.Create<DeleteSecretEndpoint>(new SecretService(mock), _err);
        await ep.HandleAsync(new KeyRequest {
            Key = "Key1"
        }, ctx);

        // Assert
        ep.HttpContext.Response.StatusCode.Should().Be(500);
        ep.HttpContext.Response.ContentLength.Should().Be(null);
    }
}