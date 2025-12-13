namespace ServerManagement.Models;

public class ServerService : IServerService
{
  private readonly IServersEfCoreRepository _repository;

  public ServerService(IServersEfCoreRepository repository)
  {
    _repository = repository;
  }

  public List<Server> GetServers()
  {
    return _repository.GetServers();
  }

  public List<Server> GetServersByCity(string cityName)
  {
    if (string.IsNullOrWhiteSpace(cityName))
      throw new ArgumentException("City name cannot be empty", nameof(cityName));

    return _repository.GetServersByCity(cityName);
  }

  public Server? GetServerById(int id)
  {
    if (id <= 0)
      throw new ArgumentException("ID must be greater than 0", nameof(id));

    return _repository.GetServerById(id);
  }

  public async Task AddServer(Server server)
  {
    if (server == null)
      throw new ArgumentNullException(nameof(server));

    if (string.IsNullOrWhiteSpace(server.Name))
      throw new ArgumentException("Server name cannot be empty");

    if (string.IsNullOrWhiteSpace(server.City))
      throw new ArgumentException("City cannot be empty");

    _repository.AddServer(server);
    await Task.CompletedTask;
  }

  public async Task UpdateServer(int serverId, Server server)
  {
    if (server == null)
      throw new ArgumentNullException(nameof(server));

    if (string.IsNullOrWhiteSpace(server.Name))
      throw new ArgumentException("Server name cannot be empty");

    _repository.UpdateServer(serverId, server);
    await Task.CompletedTask;
  }

  public async Task DeleteServer(int serverId)
  {
    if (serverId <= 0)
      throw new ArgumentException("ID must be greater than 0", nameof(serverId));

    _repository.DeleteServer(serverId);
    await Task.CompletedTask;
  }

  public List<Server> SearchServers(string searchFilter)
  {
    if (string.IsNullOrWhiteSpace(searchFilter))
      return new List<Server>();

    return _repository.SearchServers(searchFilter);
  }
}
