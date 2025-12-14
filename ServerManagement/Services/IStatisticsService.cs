using ServerManagement.Models;

namespace ServerManagement.Services;

public interface IStatisticsService
{
  ServerStatistics? GetServerStatistics();
}
