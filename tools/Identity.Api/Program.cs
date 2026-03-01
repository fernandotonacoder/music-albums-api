var builder = WebApplication.CreateBuilder(args);

// User Secrets (local dev) and Environment Variables (production) are loaded automatically

var jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET");
if (string.IsNullOrWhiteSpace(jwtSecret) || jwtSecret.Length < 32)
{
	throw new InvalidOperationException(
		"JWT_SECRET environment variable must be configured and at least 32 characters long.");
}

builder.Services.AddControllers();

var app = builder.Build();

app.UseAuthorization();

app.MapControllers();

app.Run();
