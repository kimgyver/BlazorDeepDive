using Microsoft.EntityFrameworkCore;
using Serilog;
using ServerManagement.Components;
using ServerManagement.Data;
using ServerManagement.Repositories;
using ServerManagement.Services;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
builder.Host.UseSerilog((context, services, configuration) =>
    configuration
        .MinimumLevel.Information()
        .WriteTo.Console(outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{Level:u3}] {Message:lj}{NewLine}{Exception}")
        .WriteTo.File(
            path: "logs/application-.log",
            outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}",
            rollingInterval: RollingInterval.Day,
            fileSizeLimitBytes: 10_485_760, // 10 MB
            retainedFileCountLimit: 7 // 7일치 보관
        )
        .Enrich.FromLogContext()
);

// Add services to the container.
builder.Services.AddDbContextFactory<ServerManagementContext>(options =>
{
    options.UseSqlite(builder.Configuration.GetConnectionString("ServerManagement"));
});

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddTransient<IServersEfCoreRepository, ServersEfCoreRepository>();
builder.Services.AddScoped<IServerService, ServerService>();
builder.Services.AddScoped<IStatisticsService, StatisticsService>();
builder.Services.AddScoped<IServerSuggestionRepository, ServerSuggestionRepository>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
