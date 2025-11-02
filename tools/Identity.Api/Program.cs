var builder = WebApplication.CreateBuilder(args);

// User Secrets (local dev) and Environment Variables (production) are loaded automatically

builder.Services.AddControllers();

var app = builder.Build();

app.UseAuthorization();

app.MapControllers();

app.Run();
