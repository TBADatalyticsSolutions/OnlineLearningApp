using OnlineLearningApp.Models;
namespace OnlineLearningApp;

public interface IQuestionService
{
    Task<Question?> GetQuestionByIdAsync(int id);
    Task<IEnumerable<Question>> GetAllQuestionsAsync();
    Task CreateQuestionAsync(Question question);
    Task UpdateQuestionAsync(Question question);
    Task DeleteQuestionAsync(int id);
}
