using FastEndpoints;
using FluentAssertions;
using KeyStoreApi.Secrets.Post;
using KeyStoreApi.Secrets.Service;
using KeyStoreApi.Tests.Fixtures;
using Xunit;

namespace KeyStoreApi.Tests.Secrets;

public class SetSecretTests {
    private const string VaultUri = "https://mock.vault";
    private readonly ErrorResponseFactory _err = new(VaultUri);

    private SecretKeyValuePairRequest GetRequest(string key, string value) {
        return new SecretKeyValuePairRequest {
            Key = key,
            RequestBody = new SecretRequestBody {
                Secret = value
            }
        };
    }

    [Theory, InlineData("test"), InlineData("test_with_bad_chars"), InlineData(
     "very_long_key_very_long_key_very_long_key_very_long_key_very_long_key_very_long_key_very_long_key_very_long_key_very_long_key_very_long_key_very_long_key_very_long_key")]
    public async Task Happy(string key) {
        // Arrange
        var mock = new MockSecretClient();

        // Act
        var ep = Factory.Create<UpdateSecretEndpoint>(new SecretService(mock), _err);
        var value = $"{key}Value";
        await ep.HandleAsync(GetRequest(key, value), CancellationToken.None);

        //Assert
        ep.HttpContext.Response.StatusCode.Should().Be(200);
        ep.Response.Secret.Should().Be(value);
    }

    [Fact]
    public async Task Cancellation() {
        // Arrange
        var mock = new MockSecretClient();
        var cancellationTokenSource = new CancellationTokenSource();
        var ctx = cancellationTokenSource.Token;
        await cancellationTokenSource.CancelAsync();

        // Act
        var ep = Factory.Create<UpdateSecretEndpoint>(new SecretService(mock), _err);
        await ep.HandleAsync(GetRequest("Key1", "Value1"), ctx);

        // Assert
        ep.HttpContext.Response.StatusCode.Should().Be(503);
        ep.HttpContext.Response.ContentLength.Should().Be(null);
    }

    [Fact]
    public async Task Conflict() {
        // Arrange
        var mock = new MockSecretClient();
        mock.VaultSecrets.Add((SecretService.Encode("Value1"), "Key1", true));

        // Act
        var ep = Factory.Create<UpdateSecretEndpoint>(new SecretService(mock), _err);
        await ep.HandleAsync(GetRequest("Value1", "Key1"), CancellationToken.None);

        // Assert
        ep.HttpContext.Response.StatusCode.Should().Be(409);
        ep.HttpContext.Response.ContentLength.Should().Be(null);
    }
}