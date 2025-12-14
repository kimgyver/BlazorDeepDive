using ServerManagement.Models;

namespace ServerManagement.Repositories;

public static class CitiesRepository
{
  /// <summary>
  /// Get all cities in the predefined order from CityOrderConfig.
  /// This ensures consistent city ordering across all components.
  /// </summary>
  public static List<string> GetCities() => CityOrderConfig.OrderedCities;
}
