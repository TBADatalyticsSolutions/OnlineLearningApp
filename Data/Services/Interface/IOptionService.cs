using OnlineLearningApp.Models;

namespace OnlineLearningApp;

public interface IOptionService
{
    Task<Option?> GetOptionByIdAsync(int id);
    Task<IEnumerable<Option>> GetAllOptionsAsync();
    Task CreateOptionAsync(Option option);
    Task UpdateOptionAsync(Option option);
    Task DeleteOptionAsync(int id);
}
