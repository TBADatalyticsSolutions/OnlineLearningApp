using Microsoft.EntityFrameworkCore;
using OnlineLearningApp.Data;
using OnlineLearningApp.Models;

namespace OnlineLearningApp;

public class ModuleService : IModuleService
{
    private readonly OnlineLearningAppDbContext _context;

    public ModuleService(OnlineLearningAppDbContext context)
    {
        _context = context;
    }

    public async Task<Module?> GetModuleByIdAsync(int id)
    {
        return await _context.Modules.FindAsync(id);
    }

    public async Task<IEnumerable<Module>> GetAllModulesAsync()
    {
        return await _context.Modules.ToListAsync();
    }

    public async Task CreateModuleAsync(Module module)
    {
        await _context.Modules.AddAsync(module);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateModuleAsync(Module module)
    {
        _context.Modules.Update(module);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteModuleAsync(int id)
    {
        var module = await _context.Modules.FindAsync(id);
        if (module != null)
        {
            _context.Modules.Remove(module);
            await _context.SaveChangesAsync();
        }
    }
}
