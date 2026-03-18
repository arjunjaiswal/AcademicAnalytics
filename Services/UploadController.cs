using Microsoft.AspNetCore.Mvc;
using AcademicAnalytics.Services;
using AcademicAnalytics.Models;

namespace AcademicAnalytics.Controllers
{
    public class UploadController : Controller
    {
        public IActionResult Index()
        {
            ModelState.Clear();
            return View();
        }

        [HttpPost]
        public IActionResult UploadFiles(
            IFormFile marksFile,
            IFormFile mappingFile,
            string Subject,
            string Semester,
            string AcademicYear,
            string Department,
            string FacultyName)
        {
            // Save metadata
            HttpContext.Session.SetString("Subject", Subject ?? "");
            HttpContext.Session.SetString("Semester", Semester ?? "");
            HttpContext.Session.SetString("AcademicYear", AcademicYear ?? "");
            HttpContext.Session.SetString("Department", Department ?? "");
            HttpContext.Session.SetString("FacultyName", FacultyName ?? "");

            // Validate files
            if (marksFile == null || mappingFile == null)
            {
                TempData["Error"] = "Please upload both Excel files.";
                return RedirectToAction("Index");
            }

            string marksExt = Path.GetExtension(marksFile.FileName).ToLower();
            string mappingExt = Path.GetExtension(mappingFile.FileName).ToLower();

            if (marksExt != ".xlsx" || mappingExt != ".xlsx")
            {
                TempData["Error"] = "Only .xlsx Excel files are allowed.";
                return RedirectToAction("Index");
            }

            if (marksFile.Length > 5 * 1024 * 1024 || mappingFile.Length > 5 * 1024 * 1024)
            {
                TempData["Error"] = "File size must be less than 5 MB.";
                return RedirectToAction("Index");
            }

            try
            {
                string dataFolder = Path.Combine(Directory.GetCurrentDirectory(), "Data");

                if (!Directory.Exists(dataFolder))
                    Directory.CreateDirectory(dataFolder);

                string marksPath = Path.Combine(dataFolder, Guid.NewGuid() + "_marks.xlsx");
                string mappingPath = Path.Combine(dataFolder, Guid.NewGuid() + "_mapping.xlsx");

                using (var stream = new FileStream(marksPath, FileMode.Create))
                {
                    marksFile.CopyTo(stream);
                }

                using (var stream = new FileStream(mappingPath, FileMode.Create))
                {
                    mappingFile.CopyTo(stream);
                }

                TempData["MarksFile"] = marksPath;
                TempData["MappingFile"] = mappingPath;

                TempData["Message"] = "Files uploaded successfully. Click Analyze Results.";

                return RedirectToAction("Index");
            }
            catch
            {
                TempData["Error"] = "Error while saving files.";
                return RedirectToAction("Index");
            }
        }

        public IActionResult Analyze()
        {
            ExcelReader reader = new ExcelReader();
            MappingReader mapper = new MappingReader();
            AnalyticsEngine engine = new AnalyticsEngine();

            string marksPath = TempData["MarksFile"]?.ToString();
            string mappingPath = TempData["MappingFile"]?.ToString();

            if (string.IsNullOrEmpty(marksPath) || !System.IO.File.Exists(marksPath))
            {
                TempData["Error"] = "Marks file not found. Please upload again.";
                return RedirectToAction("Index");
            }

            if (string.IsNullOrEmpty(mappingPath) || !System.IO.File.Exists(mappingPath))
            {
                TempData["Error"] = "Mapping file not found. Please upload again.";
                return RedirectToAction("Index");
            }

            List<StudentMark> students;
            List<QuestionMap> mapping;

            try
            {
                students = reader.ReadMarks(marksPath);
                mapping = mapper.ReadMapping(mappingPath);
            }
            catch
            {
                TempData["Error"] = "Error reading Excel files. Please check format.";
                return RedirectToAction("Index");
            }

            if (students == null || students.Count == 0)
            {
                TempData["Error"] = "Marks file contains no student data.";
                return RedirectToAction("Index");
            }

            if (mapping == null || mapping.Count == 0)
            {
                TempData["Error"] = "Mapping file is empty or invalid.";
                return RedirectToAction("Index");
            }

            // Validate question consistency
            var excelQuestions = students.First().QuestionMarks.Keys
                .Select(q => q.Trim().ToUpper())
                .ToList();

            var mappingQuestions = mapping
                .Select(m => m.Question.Trim().ToUpper())
                .ToList();

            foreach (var q in excelQuestions)
            {
                if (!mappingQuestions.Contains(q))
                {
                    TempData["Error"] = "Mapping missing for question: " + q;
                    return RedirectToAction("Index");
                }
            }

            foreach (var mq in mappingQuestions)
            {
                if (!excelQuestions.Contains(mq))
                {
                    TempData["Error"] = "Mapping contains question not found in marks file: " + mq;
                    return RedirectToAction("Index");
                }
            }

            // 🔥 MAIN ANALYSIS
            AnalysisResult result = engine.Analyze(students, mapping);
            result.Mapping = mapping;

            // ---------------- DIFFICULTY COUNTS ----------------

            var difficultyCounts = new Dictionary<string, int>
            {
                { "Easy", 0 },
                { "Moderate", 0 },
                { "Difficult", 0 }
            };

            foreach (var level in result.DifficultyLevel.Values)
            {
                if (difficultyCounts.ContainsKey(level))
                    difficultyCounts[level]++;
            }

            ViewBag.DifficultyLabels = difficultyCounts.Keys.ToList();
            ViewBag.DifficultyData = difficultyCounts.Values.ToList();

            int easy = difficultyCounts["Easy"];
            int moderate = difficultyCounts["Moderate"];
            int difficult = difficultyCounts["Difficult"];

            string difficultySummary;

            if (difficult > easy && difficult > moderate)
                difficultySummary = "Overall paper difficulty appears high.";
            else if (easy > moderate && easy > difficult)
                difficultySummary = "Paper appears relatively easy for students.";
            else
                difficultySummary = "Paper difficulty appears balanced.";

            difficultySummary += $" ({easy} Easy, {moderate} Moderate, {difficult} Difficult).";

            ViewBag.DifficultySummary = difficultySummary;

            // ---------------- METADATA ----------------

            ViewBag.Subject = HttpContext.Session.GetString("Subject");
            ViewBag.Semester = HttpContext.Session.GetString("Semester");
            ViewBag.AcademicYear = HttpContext.Session.GetString("AcademicYear");
            ViewBag.Department = HttpContext.Session.GetString("Department");
            ViewBag.FacultyName = HttpContext.Session.GetString("FacultyName");

            // ---------------- CLEANUP ----------------

            try
            {
                if (System.IO.File.Exists(marksPath))
                    System.IO.File.Delete(marksPath);

                if (System.IO.File.Exists(mappingPath))
                    System.IO.File.Delete(mappingPath);
            }
            catch { }

            return View(result);
        }
    }
}
