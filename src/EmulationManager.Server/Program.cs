using System.Threading.RateLimiting;
using EmulationManager.Server.Components;
using EmulationManager.Server.Data;
using EmulationManager.Server.Services;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Serilog;

// Bootstrap Serilog early for startup logging
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // Serilog
    builder.Host.UseSerilog((context, services, config) => config
        .ReadFrom.Configuration(context.Configuration)
        .WriteTo.Console()
        .WriteTo.File(
            Path.Combine("logs", "server-.log"),
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 14)
        .Enrich.FromLogContext());

    // Configuration
    builder.Services.Configure<StorageOptions>(
        builder.Configuration.GetSection(StorageOptions.SectionName));

    // Database
    builder.Services.AddDbContext<EmulationManagerDbContext>(options =>
        options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

    // Services
    builder.Services.AddScoped<IGameService, GameService>();
    builder.Services.AddScoped<IGameAdminService, GameAdminService>();
    builder.Services.AddScoped<IFileProxyService, FileProxyService>();
    builder.Services.AddHostedService<LibraryScanService>();

    // Health checks
    builder.Services.AddHealthChecks();

    // Rate limiting for download endpoints
    builder.Services.AddRateLimiter(options =>
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        options.AddFixedWindowLimiter("downloads", limiter =>
        {
            limiter.PermitLimit = 5;
            limiter.Window = TimeSpan.FromSeconds(10);
            limiter.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            limiter.QueueLimit = 10;
        });
    });

    // API Controllers
    builder.Services.AddControllers()
        .AddJsonOptions(o =>
        {
            o.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
        });

    // SignalR
    builder.Services.AddSignalR();

    // Blazor
    builder.Services.AddRazorComponents()
        .AddInteractiveServerComponents();

    var app = builder.Build();

    // Apply pending migrations and seed data on startup
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<EmulationManagerDbContext>();
        await db.Database.MigrateAsync();
        await SeedDataService.SeedAsync(db);
    }

    // Serilog request logging (replaces default Microsoft request logging)
    app.UseSerilogRequestLogging();

    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Error", createScopeForErrors: true);
        app.UseHsts();
    }

    app.UseHttpsRedirection();
    app.UseStaticFiles();
    app.UseRouting();
    app.UseRateLimiter();
    app.UseAntiforgery();

    app.MapHealthChecks("/health");
    app.MapControllers();
    app.MapStaticAssets();
    app.MapRazorComponents<App>()
        .AddInteractiveServerRenderMode();

    Log.Information("EmulationManager server starting");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Server terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
