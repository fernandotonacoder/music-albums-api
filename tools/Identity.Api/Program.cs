var builder = WebApplication.CreateBuilder(args);

// Jwt:Secret is populated from appsettings, User Secrets (local dev),
// or the Jwt__Secret environment variable on Azure.
var jwtSecret = builder.Configuration["Jwt:Secret"];
if (string.IsNullOrWhiteSpace(jwtSecret) || jwtSecret.Length < 32)
{
	throw new InvalidOperationException(
		"Jwt:Secret must be configured and at least 32 characters long.");
}

builder.Services.AddControllers();

var app = builder.Build();

app.UseAuthorization();

app.MapControllers();

await app.RunAsync();
