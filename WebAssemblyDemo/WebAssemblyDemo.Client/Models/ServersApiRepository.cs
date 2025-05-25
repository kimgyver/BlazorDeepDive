using System.Text;
using System.Text.Json;
using WebAssemblyDemo.Client.Models;

public class ServersApiRepository : IServersRepository
{
  private const string apiName = "ServerApi";
  private readonly IHttpClientFactory httpClientFactory;

  public ServersApiRepository(IHttpClientFactory httpClientFactory)
  {
    this.httpClientFactory = httpClientFactory;
  }

  public async Task<Server?> GetServerByIdAsync(int id)
  {
    var httpClient = httpClientFactory.CreateClient(apiName);
    var response = await httpClient.GetAsync($"servers/{id}.json");
    response.EnsureSuccessStatusCode();
    var content = await response.Content.ReadAsStringAsync();
    var options = new JsonSerializerOptions
    {
      PropertyNameCaseInsensitive = true
    };
    return JsonSerializer.Deserialize<Server>(content, options);
  }

  public async Task<List<Server>> GetServersAsync()
  {
    var httpClient = httpClientFactory.CreateClient(apiName);
    var response = await httpClient.GetAsync("servers.json");
    System.Console.WriteLine("RESPONSE" + JsonSerializer.Serialize(response));
    response.EnsureSuccessStatusCode();

    var content = await response.Content.ReadAsStringAsync();
    System.Console.WriteLine("CONTENT:" + content);
    if (!string.IsNullOrEmpty(content) && content != "null")
    {
      var options = new JsonSerializerOptions
      {
        PropertyNameCaseInsensitive = true
      };

      var servers = JsonSerializer.Deserialize<List<Server?>>(content, options)
                    ?? new List<Server?>();

      // Remove nulls from the list
      return servers
          .Where(server => server != null)
          .Cast<Server>()
          .ToList();
    }
    else
    {
      return new List<Server>();
    }
  }

  public async Task AddServerAsync(Server server)
  {
    server.ServerId = await GetNextServerIdAsync();
    var httpClient = httpClientFactory.CreateClient(apiName);
    var content = new StringContent(JsonSerializer.Serialize(server), Encoding.UTF8, "application/json");
    var response = await httpClient.PutAsync($"servers/{server.ServerId}.json", content);
    response.EnsureSuccessStatusCode();
  }

  public async Task UpdateServerAsync(int serverId, Server server)
  {
    if (serverId != server.ServerId) return;
    var httpClient = httpClientFactory.CreateClient(apiName);
    var content = new StringContent(JsonSerializer.Serialize(server), Encoding.UTF8, "application/json");
    var response = await httpClient.PatchAsync($"servers/{server.ServerId}.json", content);
    response.EnsureSuccessStatusCode();
  }

  public async Task DeleteServerAsync(int serverId)
  {
    var httpClient = httpClientFactory.CreateClient(apiName);
    await httpClient.DeleteAsync($"servers/{serverId}.json");
  }

  private async Task<int> GetNextServerIdAsync()
  {
    var servers = await GetServersAsync();
    if (servers is not null && servers.Any())
    {
      return servers.Where(x => x is not null).Max(x => x.ServerId) + 1;
    }
    return 1;
  }
}
