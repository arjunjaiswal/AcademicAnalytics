namespace AcademicAnalytics.Models
{
    public class StudentMark
    {
        public string RollNo { get; set; }

        public Dictionary<string, double> QuestionMarks { get; set; }
            = new Dictionary<string, double>();
    }
}