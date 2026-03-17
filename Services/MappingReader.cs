using ClosedXML.Excel;
using AcademicAnalytics.Models;

namespace AcademicAnalytics.Services
{
    public class MappingReader
    {
        public List<QuestionMap> ReadMapping(string filePath)
        {
            List<QuestionMap> mapping = new();

            using (var workbook = new XLWorkbook(filePath))
            {
                var ws = workbook.Worksheet(1);

                var rows = ws.RangeUsed().RowsUsed().Skip(1);

                foreach (var row in rows)
                {
                    double maxMarks = 0;

                    var maxCell = row.Cell(5);

                    if (!maxCell.IsEmpty())
                    {
                        double.TryParse(maxCell.GetString(), out maxMarks);
                    }

                    QuestionMap map = new QuestionMap
                    {
                        Question = row.Cell(1).GetString().Trim(),
                        Unit = row.Cell(2).GetString().Trim(),
                        CO = row.Cell(3).GetString().Trim(),
                        Bloom = row.Cell(4).GetString().Trim(),
                        MaxMarks = maxMarks
                    };

                    mapping.Add(map);
                }
            }

            return mapping;
        }
    }
}