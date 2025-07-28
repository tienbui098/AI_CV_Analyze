namespace AI_CV_Analyze.Models.DTOs
{
    public class ScoreAnalysisDto
    {
        public int Layout { get; set; }
        public int Skill { get; set; }
        public int Experience { get; set; }
        public int Education { get; set; }
        public int Keyword { get; set; }
        public int Format { get; set; }
        public string RawJson { get; set; }
    }
} 