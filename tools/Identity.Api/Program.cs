var builder = WebApplication.CreateBuilder(args);

// Prefer environment variables, but allow configuration providers (for example user-secrets in development).
var jwtSecret = Environment.GetEnvironmentVariable("JWT_KEY")
    ?? builder.Configuration["JWT_KEY"];
if (string.IsNullOrWhiteSpace(jwtSecret) || jwtSecret.Length < 32)
{
	throw new InvalidOperationException(
		"JWT_KEY must be configured (environment variable or configuration) and be at least 32 characters long.");
}

builder.Services.AddControllers();

if (builder.Environment.IsDevelopment())
{
	builder.Services.AddEndpointsApiExplorer();
	builder.Services.AddSwaggerGen(options =>
	{
		var xmlFile = $"{typeof(Program).Assembly.GetName().Name}.xml";
		var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
		if (File.Exists(xmlPath))
		{
			options.IncludeXmlComments(xmlPath);
		}
	});
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
