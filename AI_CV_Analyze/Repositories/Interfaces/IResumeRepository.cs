using AI_CV_Analyze.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AI_CV_Analyze.Repositories.Interfaces
{
    public interface IResumeRepository
    {
        Task<Resume> GetByIdAsync(int id);
        Task<IEnumerable<Resume>> GetAllAsync();
        Task<IEnumerable<Resume>> GetByUserIdAsync(int userId);
        Task<Resume> CreateAsync(Resume resume);
        Task<Resume> UpdateAsync(Resume resume);
        Task DeleteAsync(int id);
        Task<Resume> GetLatestByUserIdAsync(int userId);
    }
} 