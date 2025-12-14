namespace ServerManagement.Models;

/// <summary>
/// Centralized configuration for city ordering across the application.
/// This ensures consistent city ordering in Dashboard, Server list, and Charts.
/// </summary>
public static class CityOrderConfig
{
  /// <summary>
  /// Ordered list of cities for consistent display.
  /// Cities are displayed in this order in all components (Dashboard, Server List, Charts).
  /// </summary>
  public static readonly List<string> OrderedCities = new()
  {
    "Toronto",
    "Montreal",
    "Ottawa",
    "Calgary",
    "Halifax",
  };

  /// <summary>
  /// Sort a list of cities according to the predefined order.
  /// Cities not in the OrderedCities list will be placed at the end in their original order.
  /// </summary>
  public static List<string> SortCities(IEnumerable<string> cities)
  {
    return cities
      .OrderBy(city => OrderedCities.IndexOf(city) >= 0 ? OrderedCities.IndexOf(city) : int.MaxValue)
      .ToList();
  }

  /// <summary>
  /// Sort a dictionary by city keys according to the predefined order.
  /// </summary>
  public static Dictionary<string, T> SortDictionary<T>(IDictionary<string, T> dictionary)
  {
    return SortCities(dictionary.Keys)
      .ToDictionary(key => key, key => dictionary[key]);
  }
}
