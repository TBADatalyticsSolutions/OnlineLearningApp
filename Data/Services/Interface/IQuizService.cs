using OnlineLearningApp.Models;
namespace OnlineLearningApp;

public interface IQuizService
{
    Task<Quiz?> GetQuizByIdAsync(int id);
    Task<IEnumerable<Quiz>> GetAllQuizzesAsync();
    Task CreateQuizAsync(Quiz quiz);
    Task UpdateQuizAsync(Quiz quiz);
    Task DeleteQuizAsync(int id);
}
