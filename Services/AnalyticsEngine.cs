using AcademicAnalytics.Models;

namespace AcademicAnalytics.Services
{
    public class AnalyticsEngine
    {
        private string Normalize(string q)
        {
            if (string.IsNullOrWhiteSpace(q))
                return "";
            return q.Trim()
                    .ToUpper()
                    .Replace(" ", "")
                    .Replace(".", "");
        }

        public AnalysisResult Analyze(List<StudentMark> students, List<QuestionMap> mapping)
        {
            AnalysisResult result = new AnalysisResult();

            if (students == null || students.Count == 0)
                return result;

            // QUESTION AVERAGES
            foreach (var q in mapping)
            {
                double total = 0;
                int count = 0;

                string mapQuestion = Normalize(q.Question);

                foreach (var student in students)
                {
                    foreach (var entry in student.QuestionMarks)
                    {
                        if (Normalize(entry.Key) == mapQuestion)
                        {
                            total += entry.Value;
                            count++;
                            break;
                        }
                    }
                }

                if (count > 0)
                    result.QuestionAverage[q.Question] = total / count;
            }

            // DIFFICULTY INDEX CALCULATION
            foreach (var q in mapping)
            {
                if (!result.QuestionAverage.ContainsKey(q.Question))
                    continue;

                double avg = result.QuestionAverage[q.Question];

                if (q.MaxMarks <= 0)
                    continue;

                double difficulty = (avg / q.MaxMarks) * 100;

                result.DifficultyIndex[q.Question] = difficulty;

                if (difficulty > 70)
                    result.DifficultyLevel[q.Question] = "Easy";
                else if (difficulty >= 30)
                    result.DifficultyLevel[q.Question] = "Moderate";
                else
                    result.DifficultyLevel[q.Question] = "Difficult";
            }

            // UNIT PERFORMANCE
            foreach (var q in mapping)
            {
                if (!result.UnitPerformance.ContainsKey(q.Unit))
                    result.UnitPerformance[q.Unit] = 0;

                double avg = result.QuestionAverage.ContainsKey(q.Question)
                    ? result.QuestionAverage[q.Question]
                    : 0;

                result.UnitPerformance[q.Unit] += avg;
            }

            // CO ATTAINMENT
            foreach (var q in mapping)
            {
                if (!result.COAttainment.ContainsKey(q.CO))
                    result.COAttainment[q.CO] = 0;

                double avg = result.QuestionAverage.ContainsKey(q.Question)
                    ? result.QuestionAverage[q.Question]
                    : 0;

                result.COAttainment[q.CO] += avg;
            }

            // BLOOM DISTRIBUTION
            foreach (var q in mapping)
            {
                if (!result.BloomDistribution.ContainsKey(q.Bloom))
                    result.BloomDistribution[q.Bloom] = 0;

                result.BloomDistribution[q.Bloom]++;
            }

            return result;
        }
    }

}