using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using WebAssemblyDemo.Client;
using WebAssemblyDemo.Client.Models;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddSingleton<ContainerStorage>();
builder.Services.AddHttpClient("ServerApi", client =>
{
  client.BaseAddress = new Uri("https://webassemblydemo-16d51-default-rtdb.firebaseio.com/");
  client.DefaultRequestHeaders.Add("Accept", "application/json");
});
builder.Services.AddTransient<IServersRepository, ServersApiRepository>();

await builder.Build().RunAsync();
