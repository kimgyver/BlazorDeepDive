namespace ServerManagement.Models;

public interface IServerSuggestionRepository
{
  Task<List<string>> GetServerNameSuggestionsAsync(string filter);
}
