using AI_CV_Analyze.Data;
using AI_CV_Analyze.Models;
using AI_CV_Analyze.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AI_CV_Analyze.Repositories.Implementation
{
    public class ResumeAnalysisResultRepository : IResumeAnalysisResultRepository
    {
        private readonly ApplicationDbContext _context;

        public ResumeAnalysisResultRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<ResumeAnalysisResult> GetByIdAsync(int id)
        {
            return await _context.ResumeAnalysisResults
                .Include(r => r.Resume)
                .FirstOrDefaultAsync(r => r.AnalysisResultId == id);
        }

        public async Task<IEnumerable<ResumeAnalysisResult>> GetAllAsync()
        {
            return await _context.ResumeAnalysisResults
                .Include(r => r.Resume)
                .ToListAsync();
        }

        public async Task<ResumeAnalysisResult> CreateAsync(ResumeAnalysisResult analysisResult)
        {
            await _context.ResumeAnalysisResults.AddAsync(analysisResult);
            await _context.SaveChangesAsync();
            return analysisResult;
        }

        public async Task<ResumeAnalysisResult> UpdateAsync(ResumeAnalysisResult analysisResult)
        {
            _context.ResumeAnalysisResults.Update(analysisResult);
            await _context.SaveChangesAsync();
            return analysisResult;
        }

        public async Task DeleteAsync(int id)
        {
            var analysisResult = await GetByIdAsync(id);
            if (analysisResult != null)
            {
                _context.ResumeAnalysisResults.Remove(analysisResult);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<IEnumerable<ResumeAnalysisResult>> GetByResumeIdAsync(int resumeId)
        {
            return await _context.ResumeAnalysisResults
                .Include(r => r.Resume)
                .Where(r => r.ResumeId == resumeId)
                .ToListAsync();
        }
    }
}
