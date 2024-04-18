using FastEndpoints;
using FluentAssertions;
using KeyStoreApi.Health;
using Xunit;

namespace KeyStoreApi.Tests.Health;

public sealed class HealthRouteTests {
    [Fact]
    public async Task GetHealth_Happy() {
        // Arrange
        var ep = Factory.Create<HealthEndpoint>();

        // Act
        await ep.HandleAsync(new EmptyRequest(), CancellationToken.None);

        // Assert
        ep.HttpContext.Response.StatusCode.Should().Be(200);
        ep.Response.Status.Should().Be(ServiceHealth.pass);
    }
}