using Microsoft.EntityFrameworkCore;
using UrlShortenerApi.Models;
using UrlShortenerApi.Services;
using UrlShortenerApi.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite("Data Source=urlshortenerapi.db"));
builder.Services.AddHttpContextAccessor(); 

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapPost("/api/shorten", async (AppDbContext db, CreateShortUrlRequest body, IHttpContextAccessor httpContextAccessor) =>
{
    if (string.IsNullOrEmpty(body.OriginalUrl))
        return Results.BadRequest("OriginalUrl is required");

    if (!Uri.TryCreate(body.OriginalUrl, UriKind.Absolute, out var uri) ||
        (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        return Results.BadRequest("Invalid URL format. Only HTTP/HTTPS allowed.");

    string shortCode;
    int maxAttempts = 5;
    int attempts = 0;

    do
    {
        if (attempts >= maxAttempts)
            return Results.StatusCode(500);

        shortCode = ShortCodeGenerator.Generate();
        attempts++;

        try
        {
            var shortened = new ShortenedUrl
            {
                ShortCode = shortCode,
                OriginalUrl = body.OriginalUrl,
                CreatedAt = DateTime.UtcNow
            };

            db.ShortenedUrls.Add(shortened);
            await db.SaveChangesAsync();
            break;
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("UNIQUE") == true)
        {
            continue;
        }
    } while (true);

    var request = httpContextAccessor.HttpContext?.Request;
    var shortUrl = $"{request?.Scheme}://{request?.Host}/{shortCode}";

    return Results.Created($"/api/shorten/{shortCode}", new { shortUrl });
});

app.MapGet("/{shortCode}", async (string shortCode, AppDbContext db) =>
{
    var entry = await db.ShortenedUrls.FirstOrDefaultAsync(u => u.ShortCode == shortCode);

    if (entry is null)
        return Results.NotFound("Short URL not found");

    return Results.Redirect(entry.OriginalUrl, permanent: false, preserveMethod: true);
});

app.Run();

record CreateShortUrlRequest(string OriginalUrl);