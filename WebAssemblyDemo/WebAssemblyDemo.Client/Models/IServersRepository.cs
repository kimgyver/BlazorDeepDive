namespace WebAssemblyDemo.Client.Models;

public interface IServersRepository
{
  Task<Server?> GetServerByIdAsync(int id);
  Task<List<Server>> GetServersAsync();
  Task AddServerAsync(Server server);
  Task UpdateServerAsync(int serverId, Server server);
  Task DeleteServerAsync(int serverId);
}
