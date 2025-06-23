using System.Threading.Tasks;

namespace AI_CV_Analyze.Services.Interfaces
{
    public interface IEmailSender
    {
        Task SendEmailAsync(string toEmail, string subject, string message);
    }
} 