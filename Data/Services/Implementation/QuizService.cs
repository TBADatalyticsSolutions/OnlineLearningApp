using Microsoft.EntityFrameworkCore;
using OnlineLearningApp.Data;
using OnlineLearningApp.Models;

namespace OnlineLearningApp;

public class QuizService : IQuizService
{
    private readonly OnlineLearningAppDbContext _context;
    public QuizService(OnlineLearningAppDbContext context)
    {
        _context = context;
    }
    public async Task<Quiz?> GetQuizByIdAsync(int id)
    {
        return await _context.Quizzes.FindAsync(id);
    }

    public async Task<IEnumerable<Quiz>> GetAllQuizzesAsync()
    {
        return await _context.Quizzes.ToListAsync();
    }

    public async Task CreateQuizAsync(Quiz quiz)
    {
        await _context.Quizzes.AddAsync(quiz);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateQuizAsync(Quiz quiz)
    {
        _context.Quizzes.Update(quiz);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteQuizAsync(int id)
    {
        var quiz = await _context.Quizzes.FindAsync(id);
        if (quiz != null)
        {
            _context.Quizzes.Remove(quiz);
            await _context.SaveChangesAsync();
        }
    }
}
