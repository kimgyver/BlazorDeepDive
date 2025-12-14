namespace ServerManagement.Repositories;

public interface IServerSuggestionRepository
{
  Task<List<string>> GetServerNameSuggestionsAsync(string filter);
}
