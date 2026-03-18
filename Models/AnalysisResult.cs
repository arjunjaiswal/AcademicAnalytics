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

        public Dictionary<string, string> ExpectedDifficulty { get; set; } = new();
        public Dictionary<string, string> ActualDifficulty { get; set; } = new();
        public Dictionary<string, bool> DifficultyMismatch { get; set; } = new();

        public Dictionary<string, double> DifficultyIndex { get; set; } = new Dictionary<string, double>();

        public Dictionary<string, string> DifficultyLevel { get; set; } = new Dictionary<string, string>();

        public List<string> Insights { get; set; } = new();
        public List<string> Recommendations { get; set; } = new();

        public ChartData QuestionChart { get; set; } = new();
        public ChartData UnitChart { get; set; } = new();
        public ChartData COChart { get; set; } = new();
        public ChartData BloomChart { get; set; } = new();

        public List<QuestionMap> Mapping { get; set; } = new();
    }

    public class ChartData
    {
        public List<string> Labels { get; set; } = new();
        public List<double> Values { get; set; } = new();
        public List<string> Colors { get; set; } = new();
        public string XAxisTitle { get; set; } = "";
        public string YAxisTitle { get; set; } = "";
    }
}