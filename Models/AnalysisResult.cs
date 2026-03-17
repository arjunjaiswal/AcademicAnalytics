namespace AcademicAnalytics.Models
{
    public class AnalysisResult
    {
        public Dictionary<string, double> QuestionAverage { get; set; }
            = new();

        public Dictionary<string, double> UnitPerformance { get; set; }
            = new();

        public Dictionary<string, double> COAttainment { get; set; }
            = new();

        public Dictionary<string, int> BloomDistribution { get; set; }
            = new();

        public Dictionary<string, double> DifficultyIndex { get; set; } = new Dictionary<string, double>();

        public Dictionary<string, string> DifficultyLevel { get; set; } = new Dictionary<string, string>();

    }
}