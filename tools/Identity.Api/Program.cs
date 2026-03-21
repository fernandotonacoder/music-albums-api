var builder = WebApplication.CreateBuilder(args);

// JWT signing key must come from environment (JWT_KEY), not from checked-in config.
var jwtSecret = Environment.GetEnvironmentVariable("JWT_KEY");
if (string.IsNullOrWhiteSpace(jwtSecret) || jwtSecret.Length < 32)
{
	throw new InvalidOperationException(
		"JWT_KEY must be configured and at least 32 characters long.");
}

builder.Services.AddControllers();

if (builder.Environment.IsDevelopment())
{
	builder.Services.AddEndpointsApiExplorer();
	builder.Services.AddSwaggerGen();
}

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
	app.UseSwagger();
	app.UseSwaggerUI();
}

app.UseAuthorization();

app.MapControllers();

await app.RunAsync();
