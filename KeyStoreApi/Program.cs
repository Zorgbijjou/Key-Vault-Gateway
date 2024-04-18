using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using FastEndpoints;
using KeyStoreApi.Secrets.Service;

var builder = WebApplication.CreateBuilder();

// Endpoint discovery
builder.Services.AddFastEndpoints();

// Key Vault
var keyVaultUri = builder.Configuration["KeyVault:Uri"] ?? throw new ArgumentException("KeyVault:Uri is required.");

builder.Services.AddSingleton<SecretService>();
builder.Services.AddSingleton(new SecretClient(
    new Uri(keyVaultUri),
    new DefaultAzureCredential())
    );

// Utility
builder.Services.AddSingleton(new ErrorResponseFactory(keyVaultUri));

// Logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

// Start application
var app = builder.Build();
app.UseFastEndpoints();
app.Run();

// Provides handle for test harness
public partial class Program {
    protected Program() { }
}
