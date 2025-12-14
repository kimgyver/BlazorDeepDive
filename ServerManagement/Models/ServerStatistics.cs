namespace ServerManagement.Models;

public class ServerStatistics
{
  public int TotalCount { get; set; }
  public int OnlineCount { get; set; }
  public int OfflineCount { get; set; }
  public double OnlinePercentage { get; set; }
  public Dictionary<string, int> CityCounts { get; set; } = new();
  public Dictionary<string, CityStatistics> CityStatisticsDetails { get; set; } = new();
}

public class CityStatistics
{
  public string CityName { get; set; } = string.Empty;
  public int TotalCount { get; set; }
  public int OnlineCount { get; set; }
  public int OfflineCount { get; set; }
}

