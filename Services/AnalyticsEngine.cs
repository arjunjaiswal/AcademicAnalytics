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

        private string NormalizeDifficulty(string input)
        {
            return input.ToLower() switch
            {
                "low" => "Easy",
                "medium" => "Moderate",
                "high" => "Difficult",
                _ => input
            };
        }

        public AnalysisResult Analyze(List<StudentMark> students, List<QuestionMap> mapping)
        {
            AnalysisResult result = new AnalysisResult();

            if (students == null || students.Count == 0)
                return result;

            // =========================
            // QUESTION AVERAGES
            // =========================
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

            // =========================
            // DIFFICULTY INDEX + EXPECTED VS ACTUAL (UNIFIED)
            // =========================
            foreach (var q in mapping)
            {
                if (!result.QuestionAverage.ContainsKey(q.Question))
                    continue;

                double avg = result.QuestionAverage[q.Question];

                if (q.MaxMarks <= 0)
                    continue;

                double percentage = (avg / q.MaxMarks) * 100;

                // Store percentage
                result.DifficultyIndex[q.Question] = percentage;

                // 🔥 SINGLE CLASSIFICATION LOGIC
                string level;
                if (percentage >= 75)
                    level = "Easy";
                else if (percentage >= 50)
                    level = "Moderate";
                else
                    level = "Difficult";

                // Use same level everywhere
                result.DifficultyLevel[q.Question] = level;

                // Expected (from faculty input → normalized)
                string expected = NormalizeDifficulty(q.Difficulty);

                // Compare
                bool mismatch = !expected.Equals(level, StringComparison.OrdinalIgnoreCase);

                result.ExpectedDifficulty[q.Question] = expected;
                result.ActualDifficulty[q.Question] = level;
                result.DifficultyMismatch[q.Question] = mismatch;
            }

            // =========================
            // UNIT PERFORMANCE
            // =========================
            foreach (var q in mapping)
            {
                if (!result.UnitPerformance.ContainsKey(q.Unit))
                    result.UnitPerformance[q.Unit] = 0;

                double avg = result.QuestionAverage.ContainsKey(q.Question)
                    ? result.QuestionAverage[q.Question]
                    : 0;

                result.UnitPerformance[q.Unit] += avg;
            }

            // =========================
            // CO ATTAINMENT
            // =========================
            foreach (var q in mapping)
            {
                if (!result.COAttainment.ContainsKey(q.CO))
                    result.COAttainment[q.CO] = 0;

                double avg = result.QuestionAverage.ContainsKey(q.Question)
                    ? result.QuestionAverage[q.Question]
                    : 0;

                result.COAttainment[q.CO] += avg;
            }

            // =========================
            // BLOOM DISTRIBUTION
            // =========================
            foreach (var q in mapping)
            {
                if (!result.BloomDistribution.ContainsKey(q.Bloom))
                    result.BloomDistribution[q.Bloom] = 0;

                result.BloomDistribution[q.Bloom]++;
            }

            // =========================
            // CHART DATA
            // =========================

            var qLabels = new List<string>();
            var qValues = new List<double>();
            var qColors = new List<string>();

            foreach (var q in result.QuestionAverage)
            {
                var map = mapping.FirstOrDefault(m => m.Question == q.Key);
                if (map == null || map.MaxMarks == 0) continue;

                double percent = (q.Value / map.MaxMarks) * 100;

                qLabels.Add(q.Key.Replace(".", " "));
                qValues.Add(q.Value);

                if (percent < 40)
                    qColors.Add("#c0392b");
                else if (percent > 75)
                    qColors.Add("#27ae60");
                else
                    qColors.Add("#2980b9");
            }

            result.QuestionChart = new ChartData
            {
                Labels = qLabels,
                Values = qValues,
                Colors = qColors,
                XAxisTitle = "Questions",
                YAxisTitle = "Average Marks"
            };

            var sortedUnits = result.UnitPerformance
                .OrderBy(u => u.Key)   // 🔥 sort by Unit name (U1, U2, U3...)
                .ToList();

            result.UnitChart = new ChartData
            {
                Labels = sortedUnits.Select(u => $"Unit {u.Key}").ToList(),
                Values = sortedUnits.Select(u => u.Value).ToList(),
                XAxisTitle = "Units",
                YAxisTitle = "Total Average Marks"
            };

            result.COChart = new ChartData
            {
                Labels = result.COAttainment.Keys.ToList(),
                Values = result.COAttainment.Values.ToList(),
                XAxisTitle = "Course Outcomes (CO)",
                YAxisTitle = "Attainment"
            };

            result.BloomChart = new ChartData
            {
                Labels = result.BloomDistribution.Keys.ToList(),
                Values = result.BloomDistribution.Values.Select(v => (double)v).ToList(),
                XAxisTitle = "Bloom Levels",
                YAxisTitle = "Number of Questions"
            };

            // =========================
            // EXISTING INSIGHTS (UNCHANGED)
            // =========================

            var weakQuestions = new List<string>();
            var weakAreas = new Dictionary<string, int>();

            foreach (var q in result.QuestionAverage)
            {
                var map = mapping.FirstOrDefault(m => m.Question == q.Key);
                if (map == null || map.MaxMarks == 0) continue;

                double percent = (q.Value / map.MaxMarks) * 100;

                string qLabel = $"{q.Key.Replace(".", " ")} (Unit {map.Unit})";

                if (percent < 40)
                {
                    weakQuestions.Add(qLabel);

                    string key = $"Unit {map.Unit} - {map.Bloom}";

                    if (!weakAreas.ContainsKey(key))
                        weakAreas[key] = 0;

                    weakAreas[key]++;
                }
                else if (percent > 75)
                {
                    result.Insights.Add($"{qLabel} shows strong student understanding");
                }
            }

            if (weakQuestions.Count > 0)
            {
                result.Insights.Add($"{weakQuestions.Count} questions show low student performance");

                var mostDifficult = result.QuestionAverage
                    .Select(q =>
                    {
                        var map = mapping.FirstOrDefault(m => m.Question == q.Key);
                        if (map == null || map.MaxMarks == 0) return null;

                        double percent = (q.Value / map.MaxMarks) * 100;

                        return new { Question = q.Key, Percent = percent, Unit = map.Unit };
                    })
                    .Where(x => x != null)
                    .OrderBy(x => x.Percent)
                    .FirstOrDefault();

                if (mostDifficult != null)
                {
                    string label = $"{mostDifficult.Question.Replace(".", " ")} (Unit {mostDifficult.Unit})";
                    result.Insights.Add($"{label} appears to be the most challenging question");
                }
            }

            if (result.UnitPerformance.Count > 0)
            {
                double avg = result.UnitPerformance.Values.Average();

                foreach (var unit in result.UnitPerformance)
                {
                    if (unit.Value > avg)
                        result.Insights.Add($"Unit {unit.Key} performance is above average");
                }
            }

            var totalQuestions = result.BloomDistribution.Values.Sum();

            foreach (var b in result.BloomDistribution)
            {
                double percent = (double)b.Value / totalQuestions * 100;

                if (percent > 50)
                    result.Insights.Add($"Majority questions fall under {b.Key} level");
            }

            foreach (var area in weakAreas.OrderByDescending(a => a.Value))
            {
                string[] parts = area.Key.Split(" - ");
                string unit = parts[0];
                string bloom = parts[1];

                string recommendation = bloom switch
                {
                    "Remember" => $"Reinforce basic concepts in {unit}",
                    "Understand" => $"Improve conceptual clarity in {unit}",
                    "Apply" => $"Provide more problem-solving practice in {unit}",
                    "Analyze" => $"Encourage analytical thinking exercises in {unit}",
                    "Evaluate" => $"Introduce evaluation-based questions in {unit}",
                    "Create" => $"Promote higher-order thinking tasks in {unit}",
                    _ => $"Focus on improving performance in {unit}"
                };

                result.Recommendations.Add(recommendation);
            }

            return result;
        }
    }
}