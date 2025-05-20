namespace ServerManagement.Models;

public interface IServersEfCoreRepository
{
  void AddServer(Server server);
  void DeleteServer(int serverId);
  Server? GetServerById(int id);
  List<Server> GetServers();
  List<Server> GetServersByCity(string cityName);
  List<Server> SearchServers(string serverFilter);
  void UpdateServer(int serverId, Server server);
}