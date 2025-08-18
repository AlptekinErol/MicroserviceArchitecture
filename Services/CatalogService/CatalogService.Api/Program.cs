using CatalogService.Api.Bootstrappers;
using CatalogService.Api.Extensions;
using CatalogService.Api.Infrastructure.Context;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "CatalogService.Api", Version = "v1" });
});

// DbContext kayýtlarý
builder.Services.AddDatabase(builder.Configuration);
builder.Services.Configure<CatalogSettingsBootstrapper>(builder.Configuration.GetSection("CatalogSettings"));

var app = builder.Build();

// --- Migrate + Seed via extension ---
await app.MigrateDbContextAsync<CatalogContext>(async (ctx, sp) =>
{
    var env = sp.GetRequiredService<IWebHostEnvironment>();
    var logger = sp.GetRequiredService<ILogger<CatalogContextSeed>>();
    await new CatalogContextSeed().SeedAsync(ctx, env, logger);
});
// ------------------------------------

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAuthorization();
app.MapControllers();
app.Run();
