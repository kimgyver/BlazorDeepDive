using ServerManagement.Models;

namespace ServerManagement.Services;

public interface IServerService
{
  List<Server> GetServers();
  List<Server> GetServersByCity(string cityName);
  Server? GetServerById(int id);
  Task AddServer(Server server);
  Task UpdateServer(int serverId, Server server);
  Task DeleteServer(int serverId);
  List<Server> SearchServers(string searchFilter);
}
