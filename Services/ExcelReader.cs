using ClosedXML.Excel;
using AcademicAnalytics.Models;

namespace AcademicAnalytics.Services
{
    public class ExcelReader
    {
        public List<StudentMark> ReadMarks(string filePath)
        {
            List<StudentMark> students = new();

            using (var workbook = new XLWorkbook(filePath))
            {
                var worksheet = workbook.Worksheet(1);

                var headerRow = worksheet.Row(1);

                var headers = headerRow.Cells()
                    .Select(c => CleanHeader(c.GetString()))
                    .ToList();

                // Identify question columns automatically
                Dictionary<int, string> questionColumns = new();

                for (int i = 1; i <= headers.Count; i++)
                {
                    string header = headers[i - 1];

                    if (header.StartsWith("Q", StringComparison.OrdinalIgnoreCase))
                    {
                        questionColumns[i] = header;
                    }
                }

                var rows = worksheet.RangeUsed().RowsUsed().Skip(1);

                foreach (var row in rows)
                {
                    StudentMark student = new();

                    // Roll number usually in column 2 in exam-section file
                    student.RollNo = row.Cell(2).GetString().Trim();

                    foreach (var q in questionColumns)
                    {
                        int columnIndex = q.Key;
                        string question = q.Value;

                        var cell = row.Cell(columnIndex);

                        double marks = 0;

                        if (!cell.IsEmpty())
                        {
                            double.TryParse(cell.GetString(), out marks);
                        }

                        student.QuestionMarks[question] = marks;
                    }

                    students.Add(student);
                }
            }

            return students;
        }

        private string CleanHeader(string header)
        {
            if (string.IsNullOrWhiteSpace(header))
                return "";

            header = header.Trim();

            // Remove numbering like "1)"
            int index = header.IndexOf(")");
            if (index > 0)
                header = header.Substring(index + 1);

            header = header.Trim();

            // Remove marks like "(10.00)"
            int bracketIndex = header.IndexOf("(");
            if (bracketIndex > 0)
                header = header.Substring(0, bracketIndex);

            return header.Trim();
        }
    }
}