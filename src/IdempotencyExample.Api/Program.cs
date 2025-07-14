using IdempotencyExample.Api.Idempotency;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Memory Cache servisi
builder.Services.AddMemoryCache();

// Idempotency servisleri
builder.Services.AddScoped<IIdempotencyStore, MemoryCacheIdempotencyStore>();
builder.Services.AddScoped<IdempotencyFilter>();

// Logging
builder.Services.AddLogging();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();

// Test projesi için Program sınıfını public yapmak
public partial class Program { }
