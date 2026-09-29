using Microsoft.EntityFrameworkCore;
using OnlineLearningApp.Data;
using OnlineLearningApp.Models;

namespace OnlineLearningApp;

public class OptionService : IOptionService
{
    private readonly OnlineLearningAppDbContext _context;

    public OptionService(OnlineLearningAppDbContext context)
    {
        _context = context;
    }

    public async Task<Option?> GetOptionByIdAsync(int id)
    {
        return await _context.Options.FindAsync(id);
    }

    public async Task<IEnumerable<Option>> GetAllOptionsAsync()
    {
        return await _context.Options.ToListAsync();
    }

    public async Task CreateOptionAsync(Option option)
    {
        await _context.Options.AddAsync(option);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateOptionAsync(Option option)
    {
        _context.Options.Update(option);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteOptionAsync(int id)
    {
        var option = await _context.Options.FindAsync(id);
        if (option != null)
        {
            _context.Options.Remove(option);
            await _context.SaveChangesAsync();
        }
    }
}
