using AI_CV_Analyze.Data;
using AI_CV_Analyze.Models;
using AI_CV_Analyze.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AI_CV_Analyze.Repositories.Implementation
{
    public class ResumeRepository : IResumeRepository
    {
        private readonly ApplicationDbContext _context;

        public ResumeRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Resume> GetByIdAsync(int id)
        {
            return await _context.Resumes
                .FirstOrDefaultAsync(r => r.ResumeId == id);
        }

        public async Task<IEnumerable<Resume>> GetAllAsync()
        {
            return await _context.Resumes
                .OrderByDescending(r => r.UploadDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<Resume>> GetByUserIdAsync(int userId)
        {
            return await _context.Resumes
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.UploadDate)
                .ToListAsync();
        }

        public async Task<Resume> CreateAsync(Resume resume)
        {
            await _context.Resumes.AddAsync(resume);
            await _context.SaveChangesAsync();
            return resume;
        }

        public async Task<Resume> UpdateAsync(Resume resume)
        {
            _context.Resumes.Update(resume);
            await _context.SaveChangesAsync();
            return resume;
        }

        public async Task DeleteAsync(int id)
        {
            var resume = await GetByIdAsync(id);
            if (resume != null)
            {
                _context.Resumes.Remove(resume);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<Resume> GetLatestByUserIdAsync(int userId)
        {
            return await _context.Resumes
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.UploadDate)
                .FirstOrDefaultAsync();
        }
    }
} 