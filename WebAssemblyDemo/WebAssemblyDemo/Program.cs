using WebAssemblyDemo.Client.Models;
using WebAssemblyDemo.Components;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveWebAssemblyComponents();

builder.Services.AddHttpClient("ServerApi", client =>
{
    client.BaseAddress = new Uri("https://webassemblydemo-16d51-default-rtdb.firebaseio.com/");
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});
builder.Services.AddTransient<IServersRepository, ServersApiRepository>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(WebAssemblyDemo.Client._Imports).Assembly);

app.Run();
