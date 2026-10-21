using ClosedXML.Excel;
using ExcelDataReader;
using System.Data;
using System.Text;
using System.Xml.Serialization;

namespace LogisticsPlatform.Testing.Extensions.Helpers
{
    public class FileGenerationHelper(string testDataFilePath, string logFilePath)
    {
        public void GenerateJsonFiles(string scenario, string workSheetName, Func<string[], string> InputData, string outputPath, string filePrefix, string folderName, bool hasHeader = true)
        {
            var scenarioData = ReadExcelData(scenario, workSheetName, hasHeader);

            foreach (var inputData in scenarioData.Select((scenario, index) => new { Scenario = scenario, Index = index }))
            {
                string[] fields = inputData.Scenario.ItemArray.Select(field => field.ToString()).ToArray();

                var modelJson = InputData(fields);

                FileHelper.WriteLine(outputPath + "/" + filePrefix + (inputData.Index + 1) + ".json", modelJson);
            }

            FileHelper.AppendLine(logFilePath, $"Files generated in {folderName}");
        }

        public void GenerateJsonFiles(string scenario, string workSheetName, Func<string[], (string, string)> InputData, string outputPath, string folderName, bool hasHeader = true)
        {
            var scenarioData = ReadExcelData(scenario, workSheetName, hasHeader);
            string latestFilePrefix = string.Empty;
            foreach (var inputData in scenarioData.Select((scenario, index) => new { Scenario = scenario, Index = index }))
            {
                string[] fields = inputData.Scenario.ItemArray.Select(field => field.ToString()).ToArray();

                var (modelJson, newFilePrefix) = InputData(fields);
                latestFilePrefix = newFilePrefix;
                FileHelper.WriteLine(outputPath + "/" + latestFilePrefix + ".json", modelJson);
            }

            FileHelper.AppendLine(logFilePath, $"Files generated in {folderName}");
        }

        public void GenerateXmlFiles<TModel>(string scenario, string workSheetName, Func<string[], TModel> InputData, string outputPath, string filePrefix, string folderName, bool hasHeader = true)
        {
            var scenarioData = ReadExcelData(scenario, workSheetName, hasHeader);

            foreach (var inputData in scenarioData.Select((scenario, index) => new { Scenario = scenario, Index = index }))
            {
                string[] fields = inputData.Scenario.ItemArray.Select(field => field.ToString()).ToArray();

                var modelJson = InputData(fields);

                var xmlSerializer = new XmlSerializer(typeof(TModel));
                var streamWriter = new StreamWriter(outputPath + "/" + filePrefix + (inputData.Index + 1) + ".xml");
                xmlSerializer.Serialize(streamWriter, modelJson);
                streamWriter.Close();
            }

            FileHelper.AppendLine(logFilePath, $"Files generated in {folderName}");
        }

        public void GenerateFlatFiles(string scenario, string workSheetName, Func<string[], StringBuilder> InputData, string outputPath, string filePrefix, string folderName, string fileExtension, bool hasHeader = true)
        {
            var scenarioData = ReadExcelData(scenario, workSheetName, hasHeader);

            StringBuilder stringBuilder = new StringBuilder();

            foreach (var inputData in scenarioData.Select((scenario, index) => new { Scenario = scenario, Index = index }))
            {
                string[] fields = inputData.Scenario.ItemArray.Select(field => field.ToString()).ToArray();

                stringBuilder = InputData(fields);

                if (stringBuilder.Length == 0)
                    continue;
            }

            FileHelper.WriteLine(outputPath + "/" + filePrefix + fileExtension, stringBuilder.ToString().TrimEnd('\r', '\n'));

            FileHelper.AppendLine(logFilePath, $"Files generated in {folderName}");
        }

        public void GenerateFlatFiles(string scenario, string workSheetName, Func<string[], (StringBuilder, string)> InputData, string outputPath, string filePrefix, string folderName, string fileExtension, bool hasHeader = true)
        {
            var scenarioData = ReadExcelData(scenario, workSheetName, hasHeader);

            StringBuilder stringBuilder = new StringBuilder();
            string latestFilePrefix = filePrefix;

            foreach (var inputData in scenarioData.Select((scenario, index) => new { Scenario = scenario, Index = index }))
            {
                string[] fields = inputData.Scenario.ItemArray.Select(field => field.ToString()).ToArray();

                // Append data to StringBuilder and get the latest filePrefix
                var (sb, newFilePrefix) = InputData(fields);

                latestFilePrefix = newFilePrefix;

                // Condition to skip certain rows
                if (sb.Length == 0)
                {
                    continue;
                }
                FileHelper.AppendLine(outputPath + "/" + latestFilePrefix + fileExtension, sb.ToString().TrimEnd('\r', '\n'));
                sb.Clear(); // Clear the StringBuilder for the next iteration
            }

            FileHelper.AppendLine(logFilePath, $"Files generated in {folderName}");
        }

        public IEnumerable<DataRow> ReadExcelData(string scenario, string workSheetName, bool hasHeader = true)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            DataSet result;

            using (var stream = File.Open(testDataFilePath, FileMode.Open, FileAccess.Read))
            {
                IExcelDataReader excelReader = ExcelReaderFactory.CreateOpenXmlReader(stream);
                result = excelReader.AsDataSet(new ExcelDataSetConfiguration()
                {
                    ConfigureDataTable = (_) => new ExcelDataTableConfiguration()
                    {
                        UseHeaderRow = hasHeader
                    }
                });
            }

            var scenarioData = result.Tables[workSheetName].AsEnumerable().GroupBy(d => new
            {
                id = d.Field<string>("Scenario")
            })
                .Where(d => d.Key.id != null && d.Key.id == scenario)
                ?.Select(d => d.ToList())?.SelectMany(d => d) ?? Array.Empty<DataRow>();

            return scenarioData;
        }

        public void GenerateExcelFiles(string scenario, string testDataWorkSheetName, string expectedWorkSheetName, string outputPath, string filePrefix, string[] headers)
        {
            var scenarioData = ReadExcelData(scenario, testDataWorkSheetName);

            var workBook = new XLWorkbook();

            var workSheet = workBook.Worksheets.Add(expectedWorkSheetName);

            // Set headers for worksheet
            for (int i = 0; i < headers.Length; i++)
            {
                workSheet.Cell(1, i + 1).Value = headers[i];
            }

            int rowCount = 2;

            foreach (var inputData in scenarioData)
            {
                string[] fields = inputData.ItemArray.Select(field => field.ToString()).ToArray();

                WriteRow(workSheet, rowCount++, fields);
                workBook.SaveAs($"{outputPath}/{filePrefix}.xlsx");
            }
        }

        public void GenerateExcelFiles(
            string scenario,
            string testDataWorkSheetName,
            string expectedWorkSheetName,
            Func<string[], string> InputData,
            string outputPath,
            string[] headers,
            Dictionary<string, string[]>? mergedHeaderMap = null)
        {
            var scenarioData = ReadExcelData(scenario, testDataWorkSheetName);
            var workBook = new XLWorkbook();
            var workSheet = workBook.Worksheets.Add(expectedWorkSheetName);

            bool hasMergedHeaders = mergedHeaderMap != null && mergedHeaderMap.Count > 0;

            int currentColumn = 1;
            int leafColumnCount = 0;

            if (!hasMergedHeaders)
            {
                // Original simple header behavior
                for (int i = 0; i < headers.Length; i++)
                {
                    workSheet.Cell(1, i + 1).Value = headers[i];
                }
                leafColumnCount = headers.Length;
            }
            else
            {
                // Row 1: parent headers (merge where needed)
                foreach (var header in headers)
                {
                    if (mergedHeaderMap.TryGetValue(header, out var childHeaders) && childHeaders is { Length: > 0 })
                    {
                        int span = childHeaders.Length;
                        var range = workSheet.Range(1, currentColumn, 1, currentColumn + span - 1);
                        range.Merge();
                        workSheet.Cell(1, currentColumn).Value = header;

                        // Row 2: child headers
                        for (int j = 0; j < span; j++)
                        {
                            workSheet.Cell(2, currentColumn + j).Value = childHeaders[j];
                        }

                        leafColumnCount += span;
                        currentColumn += span;
                    }
                    else
                    {
                        // Single (unmerged) header
                        workSheet.Cell(1, currentColumn).Value = header;
                        workSheet.Cell(2, currentColumn).Value = header; // replicate for alignment
                        leafColumnCount += 1;
                        currentColumn += 1;
                    }
                }
            }

            int dataStartRow = hasMergedHeaders ? 3 : 2;
            int rowCount = dataStartRow;

            foreach (var inputData in scenarioData)
            {
                string[] fields = inputData.ItemArray.Select(f => f.ToString()).ToArray();
                var filePrefixFromData = InputData(fields);
                if (string.IsNullOrWhiteSpace(filePrefixFromData))
                    continue;

                WriteRowDynamic(workSheet, rowCount++, fields, 2, leafColumnCount);
                workBook.SaveAs($"{outputPath}/{filePrefixFromData}.xlsx");
            }
        }

        // New helper record to express ordered parent/children groups (supports multiple empty parents)
        public sealed record HeaderGroup(string? Parent, string[] Children);

        // New overload to support multiple empty parent headers each with distinct child headers.
        // If Parent is null or empty, the merged top cell is left blank while children appear on row 2.
        public void GenerateExcelFiles(
            string scenario,
            string testDataWorkSheetName,
            string expectedWorkSheetName,
            Func<string[], string> InputData,
            string outputPath,
            IReadOnlyList<HeaderGroup> headerGroups)
        {
            var scenarioData = ReadExcelData(scenario, testDataWorkSheetName);
            var workBook = new XLWorkbook();
            var workSheet = workBook.Worksheets.Add(expectedWorkSheetName);

            // Build headers (supports empty parent with distinct children)
            int currentColumn = 1;
            int leafColumnCount = 0;

            foreach (var group in headerGroups)
            {
                var children = group.Children ?? Array.Empty<string>();
                if (children.Length == 0)
                {
                    // No children: single standalone leaf header
                    workSheet.Cell(1, currentColumn).Value = group.Parent ?? string.Empty;
                    workSheet.Cell(2, currentColumn).Value = group.Parent ?? string.Empty;
                    leafColumnCount++;
                    currentColumn++;
                    continue;
                }

                if (children.Length == 1)
                {
                    // Single child: treat as simple leaf column, parent may be blank
                    workSheet.Cell(1, currentColumn).Value = group.Parent ?? string.Empty;
                    workSheet.Cell(2, currentColumn).Value = children[0];
                    leafColumnCount++;
                    currentColumn++;
                    continue;
                }

                // Multiple children: merge parent (may be blank) and write children on row 2
                int span = children.Length;
                var range = workSheet.Range(1, currentColumn, 1, currentColumn + span - 1);
                range.Merge();
                if (!string.IsNullOrWhiteSpace(group.Parent))
                {
                    workSheet.Cell(1, currentColumn).Value = group.Parent;
                }

                for (int j = 0; j < span; j++)
                {
                    workSheet.Cell(2, currentColumn + j).Value = children[j];
                }

                leafColumnCount += span;
                currentColumn += span;
            }

            int dataStartRow = 3;
            int rowCount = dataStartRow;

            foreach (var inputData in scenarioData)
            {
                string[] fields = inputData.ItemArray.Select(f => f.ToString()).ToArray();
                var filePrefixFromData = InputData(fields);
                if (string.IsNullOrWhiteSpace(filePrefixFromData))
                    continue;

                // Starting at field index 2 (skip Scenario + something), map sequentially to leaf columns
                WriteRowDynamic(workSheet, rowCount++, fields, 2, leafColumnCount);
                workBook.SaveAs($"{outputPath}/{filePrefixFromData}.xlsx");
            }
        }

        private static void WriteRow(IXLWorksheet worksheet, int rowCount, string[] fields)
        {
            for (int i = 2; i < fields.Length; i++)
            {
                worksheet.Cell(rowCount, i - 1).Value = fields[i];
            }
        }

        private static void WriteRowDynamic(IXLWorksheet worksheet, int rowCount, string[] fields, int startFieldIndex, int totalLeafColumns)
        {
            // Maps sequential field values (starting at startFieldIndex) to leaf columns.
            for (int col = 1; col <= totalLeafColumns; col++)
            {
                int fieldIndex = startFieldIndex + col - 1;
                if (fieldIndex >= fields.Length)
                    break;
                worksheet.Cell(rowCount, col).Value = fields[fieldIndex];
            }
        }
    }
}
