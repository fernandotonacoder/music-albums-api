var builder = WebApplication.CreateBuilder(args);


var jwtSecret = builder.Configuration["Jwt:Key"];
if (string.IsNullOrWhiteSpace(jwtSecret) || jwtSecret.Length < 32)
{
    throw new InvalidOperationException(
        "JWT_KEY must be configured (user-secrets or configuration) and at least 32 characters long.");
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
