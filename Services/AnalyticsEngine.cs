using AcademicAnalytics.Models;

namespace AcademicAnalytics.Services
{
    public class AnalyticsEngine
    {
        private string Normalize(string q)
        {
            if (string.IsNullOrWhiteSpace(q)) return "";
            return q.Trim().ToUpper().Replace(" ", "").Replace(".", "");
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

        private string NormalizeCO(string co)
        {
            if (string.IsNullOrWhiteSpace(co)) return "";
            return co.Trim().ToUpper();
        }

        private string NormalizeBloom(string b)
        {
            if (string.IsNullOrWhiteSpace(b)) return "";
            return char.ToUpper(b[0]) + b.Substring(1).ToLower();
        }

        private string ColorPercent(double percent)
        {
            string color =
                percent >= 75 ? "#27ae60" :
                percent >= 50 ? "#2980b9" :
                "#c0392b";

            return $"<span style='color:{color};font-weight:bold'>{Math.Round(percent, 1)}%</span>";
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
            // DIFFICULTY
            // =========================
            foreach (var q in mapping)
            {
                if (!result.QuestionAverage.ContainsKey(q.Question))
                    continue;

                double avg = result.QuestionAverage[q.Question];
                if (q.MaxMarks <= 0) continue;

                double percentage = (avg / q.MaxMarks) * 100;

                result.DifficultyIndex[q.Question] = percentage;

                string level = percentage >= 75 ? "Easy"
                             : percentage >= 50 ? "Moderate"
                             : "Difficult";

                result.DifficultyLevel[q.Question] = level;

                string expected = NormalizeDifficulty(q.Difficulty);
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
                string coKey = NormalizeCO(q.CO);

                if (!result.COAttainment.ContainsKey(coKey))
                    result.COAttainment[coKey] = 0;

                double avg = result.QuestionAverage.ContainsKey(q.Question)
                    ? result.QuestionAverage[q.Question]
                    : 0;

                result.COAttainment[coKey] += avg;
            }

            // =========================
            // BLOOM DISTRIBUTION
            // =========================
            foreach (var q in mapping)
            {
                string bloom = NormalizeBloom(q.Bloom);

                if (!result.BloomDistribution.ContainsKey(bloom))
                    result.BloomDistribution[bloom] = 0;

                result.BloomDistribution[bloom]++;
            }

            // =========================
            // CHART DATA (unchanged)
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

                if (percent < 40) qColors.Add("#c0392b");
                else if (percent > 75) qColors.Add("#27ae60");
                else qColors.Add("#2980b9");
            }

            result.QuestionChart = new ChartData
            {
                Labels = qLabels,
                Values = qValues,
                Colors = qColors,
                XAxisTitle = "Questions",
                YAxisTitle = "Average Marks"
            };

            result.UnitChart = new ChartData
            {
                Labels = result.UnitPerformance.Keys.Select(u => $"Unit {u}").ToList(),
                Values = result.UnitPerformance.Values.ToList(),
                XAxisTitle = "Units",
                YAxisTitle = "Total Average Marks"
            };

            result.COChart = new ChartData
            {
                Labels = result.COAttainment.Keys.ToList(),
                Values = result.COAttainment.Values.ToList(),
                XAxisTitle = "CO",
                YAxisTitle = "Attainment"
            };

            result.BloomChart = new ChartData
            {
                Labels = result.BloomDistribution.Keys.ToList(),
                Values = result.BloomDistribution.Values.Select(v => (double)v).ToList(),
                XAxisTitle = "Bloom Levels",
                YAxisTitle = "Questions"
            };

            // =========================
            // INSIGHTS
            // =========================

            var strengths = new List<string>();
            var concerns = new List<string>();
            var observations = new List<string>();

            var weakQuestions = new List<string>();
            var weakAreas = new Dictionary<string, int>();
            var strongUnits = new Dictionary<string, int>();

            var bloomOrder = new List<string>
            {
                "Remember","Understand","Apply","Analyze","Evaluate","Create"
            };

            foreach (var q in result.QuestionAverage)
            {
                var map = mapping.FirstOrDefault(m => m.Question == q.Key);
                if (map == null || map.MaxMarks == 0) continue;

                double percent = (q.Value / map.MaxMarks) * 100;

                if (percent < 40)
                {
                    weakQuestions.Add(q.Key);

                    string key = $"Unit {map.Unit} - {NormalizeBloom(map.Bloom)}";

                    if (!weakAreas.ContainsKey(key))
                        weakAreas[key] = 0;

                    weakAreas[key]++;
                }
                else if (percent > 75)
                {
                    if (!strongUnits.ContainsKey(map.Unit))
                        strongUnits[map.Unit] = 0;

                    strongUnits[map.Unit]++;
                }
            }

            // ===== Strengths =====

            // 🔹 1. Strongest performing units (top performers)
            if (strongUnits.Count > 0)
            {
                var max = strongUnits.Max(u => u.Value);

                var topUnits = strongUnits
                    .Where(u => u.Value == max)
                    .Select(u => u.Key);

                var unitDetails = topUnits.Select(unit =>
                {
                    var questions = mapping.Where(m => m.Unit == unit);

                    double total = 0, obtained = 0;

                    foreach (var q in questions)
                    {
                        if (result.QuestionAverage.ContainsKey(q.Question) && q.MaxMarks > 0)
                        {
                            obtained += result.QuestionAverage[q.Question];
                            total += q.MaxMarks;
                        }
                    }

                    double percent = total > 0 ? (obtained / total) * 100 : 0;

                    var blooms = questions
                        .Select(m => NormalizeBloom(m.Bloom))
                        .Distinct()
                        .OrderBy(b => bloomOrder.IndexOf(b));

                    return $"Unit {unit} ({ColorPercent(percent)}, {string.Join(", ", blooms)})";
                });

                strengths.Add($"Strong performance in {string.Join(" and ", unitDetails)}");
            }


            // 🔹 2. Above average units (overall performance insight)
            if (result.UnitPerformance.Count > 0)
            {
                double avg = result.UnitPerformance.Values.Average();

                var aboveAvgUnits = result.UnitPerformance
                    .Where(u => u.Value > avg)
                    .Select(unit =>
                    {
                        var questions = mapping.Where(m => m.Unit == unit.Key);

                        double total = 0, obtained = 0;

                        foreach (var q in questions)
                        {
                            if (result.QuestionAverage.ContainsKey(q.Question) && q.MaxMarks > 0)
                            {
                                obtained += result.QuestionAverage[q.Question];
                                total += q.MaxMarks;
                            }
                        }

                        double percent = total > 0 ? (obtained / total) * 100 : 0;

                        return $"Unit {unit.Key} ({ColorPercent(percent)})";
                    })
                    .OrderBy(u => u)
                    .ToList();

                if (aboveAvgUnits.Count > 0)
                {
                    strengths.Add($"Overall performance is above average in {string.Join(" and ", aboveAvgUnits)}");
                }
            }

            // ===== Concerns =====

            // 🔹 1. Low performing questions count
            if (weakQuestions.Count > 0)
            {
                var formattedQuestions = weakQuestions
                    .Select(q => q.Replace(".", " "))
                    .ToList();

                string questionList = string.Join(", ", formattedQuestions);

                concerns.Add($"Low performance in {weakQuestions.Count} questions ({questionList})");
            }


            // 🔹 2. Weak areas (Unit + Bloom + %)
            if (weakAreas.Count > 0)
            {
                var topWeak = weakAreas
                    .OrderByDescending(a => a.Value)
                    .Take(2);

                var formatted = topWeak.Select(a =>
                {
                    var parts = a.Key.Split(" - ");
                    string unit = parts[0];
                    string bloom = parts[1];

                    // 🔥 Calculate % for bloom
                    var questions = mapping
                        .Where(m => m.Unit == unit.Replace("Unit ", "") && NormalizeBloom(m.Bloom) == bloom);

                    double total = 0, obtained = 0;

                    foreach (var q in questions)
                    {
                        if (result.QuestionAverage.ContainsKey(q.Question) && q.MaxMarks > 0)
                        {
                            obtained += result.QuestionAverage[q.Question];
                            total += q.MaxMarks;
                        }
                    }

                    double percent = total > 0 ? (obtained / total) * 100 : 0;

                    return $"{bloom} level within {unit} despite overall moderate performance ({ColorPercent(percent)})";
                });

                concerns.Add($"Students show weakness in {string.Join(" and ", formatted)}");
            }

            // ===== Observations =====
            var mostDifficult = result.QuestionAverage
                .Select(q =>
                {
                    var map = mapping.FirstOrDefault(m => m.Question == q.Key);
                    if (map == null || map.MaxMarks == 0) return null;

                    double percent = (q.Value / map.MaxMarks) * 100;

                    return new { q.Key, percent, map.Unit };
                })
                .Where(x => x != null)
                .OrderBy(x => x.percent)
                .FirstOrDefault();

            if (mostDifficult != null)
            {
                observations.Add(
                    $"{mostDifficult.Key.Replace(".", " ")} (Unit {mostDifficult.Unit}, {ColorPercent(mostDifficult.percent)}) is the most challenging question"
                );
            }

            var totalQ = result.BloomDistribution.Values.Sum();

            foreach (var b in result.BloomDistribution)
            {
                double percent = (double)b.Value / totalQ * 100;

                if (percent > 50)
                    observations.Add($"Majority of questions fall under {b.Key} level ({ColorPercent(percent)})");
            }

            // =========================
            // MERGE
            // =========================

            if (strengths.Count > 0)
            {
                result.Insights.Add("Strengths:");
                strengths.ForEach(s => result.Insights.Add($"• {s}"));
            }

            if (concerns.Count > 0)
            {
                result.Insights.Add("Concerns:");
                concerns.ForEach(c => result.Insights.Add($"• {c}"));
            }

            if (observations.Count > 0)
            {
                result.Insights.Add("Key Observations:");
                observations.ForEach(o => result.Insights.Add($"• {o}"));
            }

            // =========================
            // RECOMMENDATIONS (ALIGNED WITH BLOOM + % + PRIORITY)
            // =========================

            if (weakAreas.Count > 0)
            {
                foreach (var area in weakAreas.OrderByDescending(a => a.Value))
                {
                    var parts = area.Key.Split(" - ");
                    string unit = parts[0];
                    string bloom = parts[1];

                    // 🔥 Calculate Bloom-level %
                    var questions = mapping
                        .Where(m => $"Unit {m.Unit}" == unit && NormalizeBloom(m.Bloom) == bloom);

                    double total = 0, obtained = 0;

                    foreach (var q in questions)
                    {
                        if (result.QuestionAverage.ContainsKey(q.Question) && q.MaxMarks > 0)
                        {
                            obtained += result.QuestionAverage[q.Question];
                            total += q.MaxMarks;
                        }
                    }

                    double percent = total > 0 ? (obtained / total) * 100 : 0;

                    // 🔥 PRIORITY (based on %)
                    string priority =
                        percent < 40 ? "High" :
                        percent < 55 ? "Medium" :
                        "Low";

                    // 🔥 RECOMMENDATION TEXT
                    string recommendation = bloom.ToLower() switch
                    {
                        "remember" => $"Reinforce fundamental concepts (Remember level) in {unit}",
                        "understand" => $"Improve conceptual clarity (Understand level) in {unit}",
                        "apply" => $"Enhance problem-solving practice (Apply level) in {unit}",
                        "analyze" => $"Strengthen analytical skills (Analyze level) in {unit}",
                        "evaluate" => $"Develop evaluation and critical thinking skills in {unit}",
                        "create" => $"Encourage higher-order thinking and creativity in {unit}",
                        _ => $"Focus on improving {bloom} level skills in {unit}"
                    };

                    // 🔥 ADD WITH % + PRIORITY
                    result.Recommendations.Add(
                        $"{recommendation} ({Math.Round(percent, 1)}%)|{priority}"
                    );
                }
                result.Recommendations = result.Recommendations
                                        .OrderBy(r => r.Contains("|High") ? 0 :
                                                      r.Contains("|Medium") ? 1 : 2)
                                        .ToList();
            }
            else if (weakQuestions.Count > 0)
            {
                result.Recommendations.Add("Review low-performing questions and reinforce key concepts|Medium");
            }

            return result;
        }
    }
}