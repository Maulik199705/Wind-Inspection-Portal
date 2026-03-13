using WindBladeInspector.Web.Components;
using WindBladeInspector.Core.Interfaces;
using WindBladeInspector.Core.Services;
using WindBladeInspector.Infrastructure;
using WindBladeInspector.Infrastructure.Reports;
using WindBladeInspector.Infrastructure.Persistence;
using WindBladeInspector.Web.InspectionLogic;
using Serilog;
using Serilog.Events;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
    .MinimumLevel.Override("System", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
    .WriteTo.File(
        path: "logs/wind-inspection-.log",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
    .CreateLogger();

try
{
    Log.Information("Starting Wind Blade Inspector application");

    var builder = WebApplication.CreateBuilder(args);
    builder.Host.UseSerilog();

    // ?? Persistent data directory (survives redeploys) ??????????????????????
    // On a server: set DATA_DIR env var to e.g. "D:\AppData\WindBladeInspector"
    // Locally: falls back to App_Data next to the executable
    var dataDir = builder.Configuration["DataDirectory"]
                  ?? Path.Combine(builder.Environment.ContentRootPath, "App_Data");
    Directory.CreateDirectory(dataDir);

    var imagesDir = Path.Combine(dataDir, "blade-images");
    Directory.CreateDirectory(imagesDir);

    var dbPath = Path.Combine(dataDir, "windblade.db");
    Log.Information("Data directory: {DataDir}", dataDir);

    builder.Services.AddRazorComponents()
        .AddInteractiveServerComponents();

    // ?? Persistence ??????????????????????????????????????????????????????????
    builder.Services.AddSingleton<IProjectRepository>(
        new LiteDbProjectRepository(dbPath));

    builder.Services.AddSingleton<DashboardService>();
    builder.Services.AddScoped<InspectionCalculationService>();
    builder.Services.AddScoped<InspectionState>();
    builder.Services.AddScoped<DefectClassificationValidator>();
    builder.Services.AddScoped<DefectMigrationService>();
    builder.Services.AddScoped<DefectClassificationBuilderService>();

    // ?? File storage: writes to persistent dataDir, serves via /blade-images ?
    builder.Services.AddSingleton<IFileStorageService>(
        new LocalFileStorageService(imagesDir, builder.Environment.WebRootPath));

    builder.Services.AddScoped<IReportGenerationService>(sp =>
    {
        var env = sp.GetRequiredService<IHostEnvironment>();
        var logger = sp.GetRequiredService<ILogger<PdfReportGenerationService>>();
        return new PdfReportGenerationService(env, logger, imagesDir);
    });

    var app = builder.Build();

    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Error", createScopeForErrors: true);
        app.UseHsts();
    }

    app.UseHttpsRedirection();
    app.UseAntiforgery();

    // Serve blade images from the persistent directory outside wwwroot
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(imagesDir),
        RequestPath = "/blade-images"
    });

    app.MapStaticAssets();
    app.MapRazorComponents<App>()
        .AddInteractiveServerRenderMode();

    Log.Information("Wind Blade Inspector started. DB: {DbPath}", dbPath);
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}