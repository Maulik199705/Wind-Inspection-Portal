using WindBladeInspector.Web.Components;
using WindBladeInspector.Core.Interfaces;
using WindBladeInspector.Core.Services;
using WindBladeInspector.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Register domain services
// CHANGED: Use Singleton to keep data alive in memory while app runs
builder.Services.AddSingleton<DashboardService>();
builder.Services.AddScoped<InspectionCalculationService>();

// Register infrastructure services
builder.Services.AddScoped<IFileStorageService>(sp =>
    new LocalFileStorageService(builder.Environment.WebRootPath));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseAntiforgery();
app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();