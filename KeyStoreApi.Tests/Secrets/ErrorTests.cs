using Azure;
using Azure.Security.KeyVault.Secrets;
using KeyStoreApi.Secrets.Service;
using KeyStoreApi.Tests.Fixtures;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Newtonsoft.Json;
using Xunit;
using Xunit.Abstractions;

namespace KeyStoreApi.Tests.Secrets;

public class ErrorTests : IClassFixture<WebApplicationFactory<Program>> {
    private const string VaultUri = "https://mock.vault";

    private readonly HttpClient _httpClient;
    private readonly MockSecretClient _secretClient = new();

    public ErrorTests(WebApplicationFactory<Program> factory, ITestOutputHelper output) {
        _httpClient = factory.WithWebHostBuilder(x => x.ConfigureServices(c => {
            c.RemoveAll<SecretClient>();
            c.RemoveAll<ErrorResponseFactory>();
            c.TryAddSingleton<SecretClient>(_secretClient);
            c.TryAddSingleton(new ErrorResponseFactory(VaultUri));
        })).CreateClient();
    }

    [Fact]
    public async Task ErrorContext() {
        // Arrange
        _secretClient.ToThrow = new RequestFailedException(404, "", "SecretNotFound", null);

        // Act
        var resp = await _httpClient.GetAsync("/secrets/test1", CancellationToken.None);

        // Assert
        var reader = new StreamReader(await resp.Content.ReadAsStreamAsync());
        var errorResponse = await reader.ReadToEndAsync();

        // Lets just use newtonsoft, because it is more robust and easier to cast to dynamic.
        dynamic error = JsonConvert.DeserializeObject(errorResponse)!;
        Assert.Equal(VaultUri, error.backend.ToString());
    }
}