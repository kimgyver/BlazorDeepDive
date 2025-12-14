using ServerManagement.Models;
using ServerManagement.Repositories;

namespace ServerManagement.Services;

public class ServerService : IServerService
{
  private readonly IServersEfCoreRepository _repository;
  private readonly ILogger<ServerService> _logger;

  public ServerService(IServersEfCoreRepository repository, ILogger<ServerService> logger)
  {
    _repository = repository;
    _logger = logger;
  }

  public List<Server> GetServers()
  {
    try
    {
      var servers = _repository.GetServers();
      _logger.LogInformation("Retrieved {ServerCount} servers from repository", servers.Count);
      return servers;
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error retrieving servers");
      throw;
    }
  }

  public List<Server> GetServersByCity(string cityName)
  {
    if (string.IsNullOrWhiteSpace(cityName))
      throw new ArgumentException("City name cannot be empty", nameof(cityName));

    try
    {
      var servers = _repository.GetServersByCity(cityName);
      _logger.LogInformation("Retrieved {ServerCount} servers from city '{CityName}'", servers.Count, cityName);
      return servers;
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error retrieving servers from city '{CityName}'", cityName);
      throw;
    }
  }

  public Server? GetServerById(int id)
  {
    if (id <= 0)
      throw new ArgumentException("ID must be greater than 0", nameof(id));

    try
    {
      var server = _repository.GetServerById(id);
      if (server != null)
        _logger.LogInformation("Retrieved server with ID {ServerId}: {ServerName}", id, server.Name);
      else
        _logger.LogWarning("Server with ID {ServerId} not found", id);

      return server;
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error retrieving server with ID {ServerId}", id);
      throw;
    }
  }

  public async Task AddServer(Server server)
  {
    if (server == null)
      throw new ArgumentNullException(nameof(server));

    if (string.IsNullOrWhiteSpace(server.Name))
      throw new ArgumentException("Server name cannot be empty");

    if (string.IsNullOrWhiteSpace(server.City))
      throw new ArgumentException("City cannot be empty");

    try
    {
      _repository.AddServer(server);
      _logger.LogInformation("Added new server: {ServerName} in city '{CityName}'", server.Name, server.City);
      await Task.CompletedTask;
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error adding server '{ServerName}' in city '{CityName}'", server.Name, server.City);
      throw;
    }
  }

  public async Task UpdateServer(int serverId, Server server)
  {
    if (server == null)
      throw new ArgumentNullException(nameof(server));

    if (string.IsNullOrWhiteSpace(server.Name))
      throw new ArgumentException("Server name cannot be empty");

    try
    {
      _repository.UpdateServer(serverId, server);
      _logger.LogInformation("Updated server ID {ServerId}: {ServerName} in city '{CityName}'", serverId, server.Name, server.City);
      await Task.CompletedTask;
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error updating server ID {ServerId}", serverId);
      throw;
    }
  }

  public async Task DeleteServer(int serverId)
  {
    if (serverId <= 0)
      throw new ArgumentException("ID must be greater than 0", nameof(serverId));

    try
    {
      _repository.DeleteServer(serverId);
      _logger.LogInformation("Deleted server ID {ServerId}", serverId);
      await Task.CompletedTask;
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error deleting server ID {ServerId}", serverId);
      throw;
    }
  }

  public List<Server> SearchServers(string searchFilter)
  {
    if (string.IsNullOrWhiteSpace(searchFilter))
    {
      _logger.LogWarning("Search filter is empty");
      return new List<Server>();
    }

    try
    {
      var servers = _repository.SearchServers(searchFilter);
      _logger.LogInformation("Searched servers with filter '{SearchFilter}': found {ServerCount} results", searchFilter, servers.Count);
      return servers;
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error searching servers with filter '{SearchFilter}'", searchFilter);
      throw;
    }
  }
}
