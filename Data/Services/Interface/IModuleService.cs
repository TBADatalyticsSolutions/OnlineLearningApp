using OnlineLearningApp.Models;

namespace OnlineLearningApp;

public interface IModuleService
{
    Task<Module?> GetModuleByIdAsync(int id);
    Task<IEnumerable<Module>> GetAllModulesAsync();
    Task CreateModuleAsync(Module module);
    Task UpdateModuleAsync(Module module);
    Task DeleteModuleAsync(int id);
}
