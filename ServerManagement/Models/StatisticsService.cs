namespace ServerManagement.Models;

public class StatisticsService : IStatisticsService
{
  private readonly IServerService _serverService;

  public StatisticsService(IServerService serverService)
  {
    _serverService = serverService;
  }

  public ServerStatistics? GetServerStatistics()
  {
    try
    {
      var allServers = _serverService.GetServers();

      if (allServers == null || allServers.Count == 0)
        return null;

      var onlineCount = allServers.Count(s => s.IsOnline);
      var totalCount = allServers.Count;

      var cityStats = allServers
        .GroupBy(s => s.City)
        .Select(g => new
        {
          CityName = g.Key ?? "Unknown",
          Servers = g.ToList()
        })
        .OrderByDescending(x => x.Servers.Count)
        .ToDictionary(
          x => x.CityName,
          x => new CityStatistics
          {
            CityName = x.CityName,
            TotalCount = x.Servers.Count,
            OnlineCount = x.Servers.Count(s => s.IsOnline),
            OfflineCount = x.Servers.Count(s => !s.IsOnline)
          }
        );

      return new ServerStatistics
      {
        TotalCount = totalCount,
        OnlineCount = onlineCount,
        OfflineCount = totalCount - onlineCount,
        OnlinePercentage = (double)onlineCount / totalCount * 100,
        CityCounts = cityStats.ToDictionary(x => x.Key, x => x.Value.TotalCount),
        CityStatisticsDetails = cityStats
      };
    }
    catch (Exception ex)
    {
      Console.WriteLine($"Statistics load error: {ex.Message}");
      return null;
    }
  }
}
