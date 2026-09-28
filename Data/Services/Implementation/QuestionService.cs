using Microsoft.EntityFrameworkCore;
using OnlineLearningApp.Data;
using OnlineLearningApp.Models;

namespace OnlineLearningApp;
public class QuestionService : IQuestionService
{
    private readonly OnlineLearningAppDbContext _context;

    public QuestionService(OnlineLearningAppDbContext context)
    {
        _context = context;
    }

    public async Task<Question?> GetQuestionByIdAsync(int id)
    {
        return await _context.Questions.FindAsync(id);
    }

    public async Task<IEnumerable<Question>> GetAllQuestionsAsync()
    {
        return await _context.Questions.ToListAsync();
    }

    public async Task CreateQuestionAsync(Question question)
    {
        await _context.Questions.AddAsync(question);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateQuestionAsync(Question question)
    {
        _context.Questions.Update(question);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteQuestionAsync(int id)
    {
        var question = await _context.Questions.FindAsync(id);
        if (question != null)
        {
            _context.Questions.Remove(question);
            await _context.SaveChangesAsync();
        }
    }
}