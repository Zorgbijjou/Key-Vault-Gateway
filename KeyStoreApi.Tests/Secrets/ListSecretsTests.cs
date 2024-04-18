using Azure;
using FastEndpoints;
using FluentAssertions;
using KeyStoreApi.Secrets.List;
using KeyStoreApi.Secrets.Service;
using KeyStoreApi.Tests.Fixtures;
using Xunit;

namespace KeyStoreApi.Tests.Secrets;

public class ListSecretsTests {
    private const string VaultUri = "https://mock.vault";
    private readonly ErrorResponseFactory _err = new(VaultUri);

    [Fact]
    public async Task Happy() {
        // Arrange
        var mock = new MockSecretClient();
        mock.VaultSecrets.Add(new ValueTuple<string, string, bool>("Key1", "Value1", true));
        mock.VaultSecrets.Add(new ValueTuple<string, string, bool>("Key2", "Value2", false));

        // Act
        var ep = Factory.Create<ListKeysEndpoint>(new SecretService(mock), _err);
        await ep.HandleAsync(new EmptyRequest(), CancellationToken.None);

        // Assert
        ep.HttpContext.Response.StatusCode.Should().Be(200);
        ep.Response.Should().HaveCount(1);
    }

    [Fact]
    public async Task Panic() {
        // Arrange
        var mock = new MockSecretClient {
            ToThrow = new RequestFailedException("Mock")
        };

        // Act
        var ep = Factory.Create<ListKeysEndpoint>(new SecretService(mock), _err);
        await ep.HandleAsync(new EmptyRequest(), CancellationToken.None);

        // Assert
        ep.HttpContext.Response.StatusCode.Should().Be(500);
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
        var ep = Factory.Create<ListKeysEndpoint>(new SecretService(mock), _err);
        await ep.HandleAsync(new EmptyRequest(), ctx);

        // Assert
        ep.HttpContext.Response.StatusCode.Should().Be(503);
        ep.HttpContext.Response.ContentLength.Should().Be(null);
    }
}