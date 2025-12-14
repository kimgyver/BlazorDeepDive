using ServerManagement.Models;

namespace ServerManagement.Services;

public class StatisticsService : IStatisticsService
{
  private readonly IServerService _serverService;
  private readonly ILogger<StatisticsService> _logger;

  public StatisticsService(IServerService serverService, ILogger<StatisticsService> logger)
  {
    _serverService = serverService;
    _logger = logger;
  }

  public ServerStatistics? GetServerStatistics()
  {
    try
    {
      _logger.LogInformation("Starting to load server statistics");

      var allServers = _serverService.GetServers();

      if (allServers == null || allServers.Count == 0)
      {
        _logger.LogWarning("No servers found in the system");
        return null;
      }

      _logger.LogInformation("Loaded {ServerCount} servers from service", allServers.Count);

      var onlineCount = allServers.Count(s => s.IsOnline);
      var totalCount = allServers.Count;

      var cityStats = allServers
        .GroupBy(s => s.City)
        .Select(g => new
        {
          CityName = g.Key ?? "Unknown",
          Servers = g.ToList()
        })
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

      // Sort cities according to CityOrderConfig for consistent ordering
      cityStats = CityOrderConfig.SortDictionary(cityStats);

      _logger.LogInformation("Statistics calculated: Total={TotalCount}, Online={OnlineCount}, Offline={OfflineCount}, Cities={CityCount}",
        totalCount, onlineCount, totalCount - onlineCount, cityStats.Count);

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
      _logger.LogError(ex, "Error loading server statistics");
      return null;
    }
  }
}
