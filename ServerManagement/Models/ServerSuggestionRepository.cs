using Microsoft.EntityFrameworkCore;
using ServerManagement.Data;

namespace ServerManagement.Models;

public class ServerSuggestionRepository : IServerSuggestionRepository
{
  private readonly ServerManagementContext _db;
  public ServerSuggestionRepository(ServerManagementContext db) => _db = db;

  public async Task<List<string>> GetServerNameSuggestionsAsync(string filter)
  {
    if (string.IsNullOrWhiteSpace(filter)) return new();
    return await _db.Servers
        .Where(s => EF.Functions.Like(s.Name, $"%{filter}%"))
        .OrderBy(s => s.Name)
        .Select(s => s.Name)
        .Take(10)
        .ToListAsync();
  }
}