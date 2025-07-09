//using AI_CV_Analyze.Data;
//using AI_CV_Analyze.Models;
//using AI_CV_Analyze.Repositories.Interfaces;
//using Microsoft.EntityFrameworkCore;
//using System.Collections.Generic;
//using System.Threading.Tasks;

//namespace AI_CV_Analyze.Repositories.Implementation
//{
//    public class ResumeAnalysisResultRepository : IResumeAnalysisResultRepository
//    {
//        private readonly ApplicationDbContext _context;

//        public ResumeAnalysisResultRepository(ApplicationDbContext context)
//        {
//            _context = context;
//        }

//        public async Task<ResumeAnalysisResult> GetByIdAsync(int id)
//        {
//            return await _context.ResumeAnalysisResults
//                .Include(r => r.Resume)
//                .FirstOrDefaultAsync(r => r.AnalysisResultId == id);
//        }

//        public async Task<IEnumerable<ResumeAnalysisResult>> GetAllAsync()
//        {
//            return await _context.ResumeAnalysisResults
//                .Include(r => r.Resume)
//                .ToListAsync();
//        }

//        public async Task<ResumeAnalysisResult> CreateAsync(ResumeAnalysisResult analysisResult)
//        {
//            await _context.ResumeAnalysisResults.AddAsync(analysisResult);
//            await _context.SaveChangesAsync();
//            return analysisResult;
//        }

//        public async Task<ResumeAnalysisResult> UpdateAsync(ResumeAnalysisResult analysisResult)
//        {
//            _context.ResumeAnalysisResults.Update(analysisResult);
//            await _context.SaveChangesAsync();
//            return analysisResult;
//        }

//        public async Task DeleteAsync(int id)
//        {
//            var analysisResult = await GetByIdAsync(id);
//            if (analysisResult != null)
//            {
//                _context.ResumeAnalysisResults.Remove(analysisResult);
//                await _context.SaveChangesAsync();
//            }
//        }

//        public async Task<IEnumerable<ResumeAnalysisResult>> GetByResumeIdAsync(int resumeId)
//        {
//            return await _context.ResumeAnalysisResults
//                .Include(r => r.Resume)
//                .Where(r => r.ResumeId == resumeId)
//                .ToListAsync();
//        }
//    }
//} 

using AI_CV_Analyze.Data;
using AI_CV_Analyze.Models;
using AI_CV_Analyze.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AI_CV_Analyze.Repositories.Implementation
{
    public class ResumeAnalysisResultRepository : IResumeAnalysisResultRepository
    {
        private readonly ApplicationDbContext _context;

        public ResumeAnalysisResultRepository(ApplicationDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<ResumeAnalysisResult> GetByIdAsync(int id)
        {
            try
            {
                var result = await _context.ResumeAnalysisResults
                    .Include(r => r.Resume)
                    .FirstOrDefaultAsync(r => r.AnalysisResultId == id);

                return result ?? throw new KeyNotFoundException($"Analysis result with ID {id} not found.");
            }
            catch (Exception ex)
            {
                throw new RepositoryException($"Failed to retrieve analysis result with ID {id}: {ex.Message}", ex);
            }
        }

        public async Task<IEnumerable<ResumeAnalysisResult>> GetAllAsync()
        {
            try
            {
                return await _context.ResumeAnalysisResults
                    .Include(r => r.Resume)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                throw new RepositoryException($"Failed to retrieve analysis results: {ex.Message}", ex);
            }
        }

        public async Task<ResumeAnalysisResult> CreateAsync(ResumeAnalysisResult analysisResult)
        {
            if (analysisResult == null)
                throw new ArgumentNullException(nameof(analysisResult));

            try
            {
                if (analysisResult.ResumeId > 0)
                {
                    var resumeExists = await _context.Resumes.AnyAsync(r => r.ResumeId == analysisResult.ResumeId);
                    if (!resumeExists)
                        throw new RepositoryException($"Resume with ID {analysisResult.ResumeId} does not exist.");
                }

                await _context.ResumeAnalysisResults.AddAsync(analysisResult);
                await _context.SaveChangesAsync();
                return analysisResult;
            }
            catch (DbUpdateException ex)
            {
                throw new RepositoryException($"Failed to create analysis result: {ex.Message}", ex);
            }
        }

        public async Task<ResumeAnalysisResult> UpdateAsync(ResumeAnalysisResult analysisResult)
        {
            if (analysisResult == null)
                throw new ArgumentNullException(nameof(analysisResult));

            try
            {
                var existing = await _context.ResumeAnalysisResults
                    .FirstOrDefaultAsync(r => r.AnalysisResultId == analysisResult.AnalysisResultId);

                if (existing == null)
                    throw new KeyNotFoundException($"Analysis result with ID {analysisResult.AnalysisResultId} not found.");

                _context.Entry(existing).CurrentValues.SetValues(analysisResult);
                await _context.SaveChangesAsync();
                return existing;
            }
            catch (DbUpdateException ex)
            {
                throw new RepositoryException($"Failed to update analysis result: {ex.Message}", ex);
            }
        }

        public async Task DeleteAsync(int id)
        {
            try
            {
                var analysisResult = await _context.ResumeAnalysisResults
                    .FirstOrDefaultAsync(r => r.AnalysisResultId == id);

                if (analysisResult == null)
                    throw new KeyNotFoundException($"Analysis result with ID {id} not found.");

                _context.ResumeAnalysisResults.Remove(analysisResult);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                throw new RepositoryException($"Failed to delete analysis result with ID {id}: {ex.Message}", ex);
            }
        }

        public async Task<IEnumerable<ResumeAnalysisResult>> GetByResumeIdAsync(int resumeId)
        {
            try
            {
                return await _context.ResumeAnalysisResults
                    .Include(r => r.Resume)
                    .Where(r => r.ResumeId == resumeId)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                throw new RepositoryException($"Failed to retrieve analysis results for resume ID {resumeId}: {ex.Message}", ex);
            }
        }

        public async Task<IEnumerable<ResumeAnalysisResult>> SearchAsync(string? searchTerm, DateTime? startDate, DateTime? endDate)
        {
            try
            {
                var query = _context.ResumeAnalysisResults
                    .Include(r => r.Resume)
                    .AsQueryable();

                if (!string.IsNullOrEmpty(searchTerm))
                {
                    searchTerm = searchTerm.ToLower();
                    query = query.Where(r =>
                        (r.Name != null && r.Name.ToLower().Contains(searchTerm)) ||
                        (r.Skills != null && r.Skills.ToLower().Contains(searchTerm)) ||
                        (r.Education != null && r.Education.ToLower().Contains(searchTerm)) ||
                        (r.Experience != null && r.Experience.ToLower().Contains(searchTerm)));
                }

                if (startDate.HasValue)
                    query = query.Where(r => r.AnalysisDate >= startDate.Value);

                if (endDate.HasValue)
                    query = query.Where(r => r.AnalysisDate <= endDate.Value);

                return await query.ToListAsync();
            }
            catch (Exception ex)
            {
                throw new RepositoryException($"Failed to search analysis results: {ex.Message}", ex);
            }
        }
    }

    public class RepositoryException : Exception
    {
        public RepositoryException(string message) : base(message) { }
        public RepositoryException(string message, Exception innerException) : base(message, innerException) { }
    }
}