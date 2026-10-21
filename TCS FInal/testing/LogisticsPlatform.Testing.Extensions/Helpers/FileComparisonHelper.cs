using ClosedXML.Excel;
using CsvHelper;
using CsvHelper.Configuration;
using LogisticsPlatform.AutoCompare.Component;
using LogisticsPlatform.Testing.Extensions.Extensions;
using LogisticsPlatform.Testing.Extensions.Models;
using System.Globalization;
using System.Text;
using System.Text.Json;

namespace LogisticsPlatform.Testing.Extensions.Helpers
{
    public class FileComparisonHelper(string expectedFilesPath, string actualFilesPath, string filePrefix, string logFilePath, StringBuilder errorMessage = null, string componentName = null)
    {
        // Updated: sort by entire file name (case-insensitive), not relying on prefix grouping
        private static string[] GetSortedFiles(string path, string pattern)
        {
            var files = FileHelper.GetAllFiles(path, pattern);
            return files
                .OrderBy(f => Path.GetFileName(f), StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        // Overload: merges files matching any of the given patterns and sorts by file name
        private static string[] GetSortedFiles(string path, string[] patterns)
        {
            return patterns
                .SelectMany(p => FileHelper.GetAllFiles(path, p))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(f => Path.GetFileName(f), StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        private static string[] BuildFilePatterns(string prefix, string[] fileExtensions)
        {
            var extensions = fileExtensions is { Length: > 0 } ? fileExtensions : ["csv"];
            return extensions.Select(ext => $"{prefix}*.{ext.TrimStart('.')}").ToArray();
        }

        public bool CompareFlatFiles(List<SectionOptions> contentDefinitions)
        {
            var expectedFiles = GetSortedFiles(expectedFilesPath, filePrefix + "*");
            var actualFiles = GetSortedFiles(actualFilesPath, filePrefix + "*");
            bool isSuccess = true;
            if (expectedFiles.Length > 0)
            {
                if (actualFiles.Length > 0)
                {
                    FileHelper.AppendLine(logFilePath, $"{componentName} Files Comparison Started");

                    var allDifferences = new List<string>();
                    var allErrors = new List<string>();

                    int fileCount = Math.Min(expectedFiles.Length, actualFiles.Length);

                    for (int i = 0; i < fileCount; i++)
                    {
                        string expectedFile = expectedFiles[i];
                        string actualFile = actualFiles[i];

                        try
                        {
                            var expectedFileLines = File.ReadAllLines(expectedFile);
                            var actualFileLines = File.ReadAllLines(actualFile);

                            var expectedContentRows = expectedFileLines.Select(line => ParseContentRow(line, contentDefinitions)).Where(row => row.Count > 0).ToList();
                            var actualContentRows = actualFileLines.Select(line => ParseContentRow(line, contentDefinitions)).Where(row => row.Count > 0).ToList();

                            if (expectedContentRows.Count == 0)
                            {
                                isSuccess = false;
                                FileHelper.AppendLine(logFilePath, $"No content rows found in the expected file: {Path.GetFileName(expectedFile)}");
                                errorMessage?.AppendLine($"No content rows found in the expected file: {Path.GetFileName(expectedFile)}");
                                return isSuccess;
                            }

                            if (actualContentRows.Count == 0)
                            {
                                isSuccess = false;
                                FileHelper.AppendLine(logFilePath, $"No content rows found in the actual file: {Path.GetFileName(actualFile)}");
                                errorMessage?.AppendLine($"No content rows found in the actual file: {Path.GetFileName(actualFile)}");
                                return isSuccess;
                            }

                            var differences = new List<string>();

                            foreach (var contentDefinition in contentDefinitions)
                            {
                                var keyFieldName = contentDefinition.KeyFieldName;
                                var identifierFieldName = contentDefinition.IdentifierFieldName;

                                FileHelper.AppendLine(logFilePath, $"Comparing content definition: {contentDefinition.IdentifierFieldName}: {contentDefinition.Name}");
                                var expectedRows = expectedContentRows.Where(row => row.ContainsKey(identifierFieldName)).ToList();
                                var actualRows = actualContentRows.Where(row => row.ContainsKey(identifierFieldName)).ToList();

                                if (expectedRows.Count == 0)
                                {
                                    isSuccess = false;
                                    FileHelper.AppendLine(logFilePath, $"No rows found in the expected file for content definition '{contentDefinition.IdentifierFieldName}: {contentDefinition.Name}' in file: {Path.GetFileName(expectedFile)}");
                                    errorMessage?.AppendLine($"No rows found in the expected file for content definition '{contentDefinition.IdentifierFieldName}: {contentDefinition.Name}' in file: {Path.GetFileName(expectedFile)}");
                                    return isSuccess;
                                }

                                if (actualRows.Count == 0)
                                {
                                    isSuccess = false;
                                    FileHelper.AppendLine(logFilePath, $"No rows found in the actual file for content definition '{contentDefinition.IdentifierFieldName}: {contentDefinition.Name}' in file: {Path.GetFileName(actualFile)}");
                                    errorMessage?.AppendLine($"No rows found in the actual file for content definition '{contentDefinition.IdentifierFieldName}: {contentDefinition.Name}' in file: {Path.GetFileName(actualFile)}");
                                    return isSuccess;
                                }

                                var keyIndex = contentDefinition.Fields.FindIndex(f => f.Name == keyFieldName);
                                var keyLength = contentDefinition.Fields[keyIndex].Length;

                                var expectedGroupedRows = expectedRows.GroupBy(row => row[keyFieldName].Substring(0, keyLength));
                                var actualGroupedRows = actualRows.GroupBy(row => row[keyFieldName].Substring(0, keyLength));

                                foreach (var group in expectedGroupedRows)
                                {
                                    var key = group.Key;
                                    FileHelper.AppendLine(logFilePath, $"Comparison Started for Row with Key: {key}");

                                    var expectedGroupRows = group.ToList();
                                    var actualGroupRows = actualGroupedRows.FirstOrDefault(g => g.Key == key)?.ToList();

                                    if (actualGroupRows == null)
                                    {
                                        differences.Add($"Key {key} not found in actual file for {contentDefinition.IdentifierFieldName}: {contentDefinition.Name} in file: {Path.GetFileName(actualFile)} ");
                                        continue;
                                    }

                                    // Build order-independent bags keyed by row signature (non-ignored fields)
                                    var ignoredFieldNames = contentDefinition.Fields
                                        .Where(f => f.Ignore)
                                        .Select(f => f.Name)
                                        .ToHashSet(StringComparer.Ordinal);

                                    var expectedBag = BuildFlatRowBag(expectedGroupRows, ignoredFieldNames);
                                    var actualBag = BuildFlatRowBag(actualGroupRows, ignoredFieldNames);

                                    // Cancel out exact matches first
                                    CancelFlatExactMatches(expectedBag, actualBag);

                                    // Align remaining unmatched rows and collect differences
                                    AlignAndCollectFlatDifferences(key, contentDefinition, expectedBag, actualBag, ignoredFieldNames, differences, Path.GetFileName(actualFile));

                                    FileHelper.AppendLine(logFilePath, $"Comparison Ended for Row with Key: {key}");
                                }

                                foreach (var group in actualGroupedRows)
                                {
                                    var key = group.Key;
                                    if (!expectedGroupedRows.Any(g => g.Key == key))
                                    {
                                        differences.Add($"Key {key} not found in expected file for {contentDefinition.IdentifierFieldName}: {contentDefinition.Name}.");
                                    }
                                }

                                FileHelper.AppendLine(logFilePath, $"Finished comparing content definition: {contentDefinition.IdentifierFieldName}: {contentDefinition.Name}");
                            }
                            allDifferences.AddRange(differences);
                        }
                        catch (Exception ex)
                        {
                            allErrors.Add($"Error comparing files: {Path.GetFileName(expectedFile)} and {Path.GetFileName(actualFile)}: {ex.Message}");
                        }
                    }

                    foreach (var difference in allDifferences)
                    {
                        FileHelper.AppendLine(logFilePath, difference);
                        errorMessage?.AppendLine(difference);
                    }

                    if (allDifferences.Count > 0)
                    {
                        FileHelper.AppendLine(logFilePath, $"Comparison Failed: Differences found in files");
                        errorMessage?.AppendLine($"Comparison Failed: Differences found in files for component: {componentName}");
                        isSuccess = false;
                    }

                    if (allErrors.Count > 0)
                    {
                        FileHelper.AppendLine(logFilePath, $"Comparison Failed: Errors occurred during comparison: {string.Join(Environment.NewLine, allErrors)}");
                        errorMessage?.AppendLine($"Comparison Failed: Errors occurred during comparison for component: {componentName}: {string.Join(Environment.NewLine, allErrors)}");
                        isSuccess = false;
                    }

                    FileHelper.AppendLine(logFilePath, $"{componentName} Files Comparison Ended");
                    return isSuccess;
                }
                else
                {
                    isSuccess = false;
                    FileHelper.AppendLine(logFilePath, $"Actual files not generated in {actualFilesPath}");
                    LogResult(logFilePath, isSuccess, componentName, errorMessage);
                    errorMessage?.AppendLine($"Actual files not generated in {actualFilesPath}");
                    return isSuccess;
                }

            }
            else
            {
                isSuccess = false;
                FileHelper.AppendLine(logFilePath, $"Expected files not generated in {expectedFilesPath}");
                LogResult(logFilePath, isSuccess, componentName, errorMessage);
                errorMessage?.AppendLine($"Expected files not generated in {expectedFilesPath}");
                return isSuccess;
            }

        }

        public bool CompareFlatFiles(SectionOptions headerDefinition, List<SectionOptions> contentDefinitions, SectionOptions trailerDefinition)
        {
            var expectedFiles = GetSortedFiles(expectedFilesPath, filePrefix + "*");
            var actualFiles = GetSortedFiles(actualFilesPath, filePrefix + "*");
            bool isSuccess = true;
            if (expectedFiles.Length > 0)
            {
                if (actualFiles.Length > 0)
                {
                    FileHelper.AppendLine(logFilePath, $"{componentName} Files Comparison Started");

                    var allDifferences = new List<string>();
                    var allErrors = new List<string>();

                    int fileCount = Math.Min(expectedFiles.Length, actualFiles.Length);

                    for (int i = 0; i < fileCount; i++)
                    {
                        string expectedFile = expectedFiles[i];
                        string actualFile = actualFiles[i];

                        try
                        {
                            var expectedFileLines = File.ReadAllLines(expectedFile);
                            var actualFileLines = File.ReadAllLines(actualFile);

                            var expectedHeader = ParseFields(expectedFileLines.First(), headerDefinition.Fields);
                            var actualHeader = ParseFields(actualFileLines.First(), headerDefinition.Fields);

                            var expectedTrailer = ParseFields(expectedFileLines.Last(), trailerDefinition.Fields);
                            var actualTrailer = ParseFields(actualFileLines.Last(), trailerDefinition.Fields);

                            var expectedContentRows = expectedFileLines.Skip(1).Take(expectedFileLines.Length - 2).Select(line => ParseContentRow(line, contentDefinitions)).Where(row => row.Count > 0).ToList();
                            var actualContentRows = actualFileLines.Skip(1).Take(actualFileLines.Length - 2).Select(line => ParseContentRow(line, contentDefinitions)).Where(row => row.Count > 0).ToList();

                            if (expectedContentRows.Count == 0)
                            {
                                isSuccess = false;
                                FileHelper.AppendLine(logFilePath, $"No content rows found in the expected file: {Path.GetFileName(expectedFile)}");
                                errorMessage?.AppendLine($"No content rows found in the expected file: {Path.GetFileName(expectedFile)}");
                                return isSuccess;
                            }

                            if (actualContentRows.Count == 0)
                            {
                                isSuccess = false;
                                FileHelper.AppendLine(logFilePath, $"No content rows found in the actual file: {Path.GetFileName(actualFile)}");
                                errorMessage?.AppendLine($"No content rows found in the actual file: {Path.GetFileName(actualFile)}");
                                return isSuccess;
                            }

                            var differences = new List<string>();

                            // Compare headers
                            var headerDifferences = CompareSections(expectedHeader, actualHeader, headerDefinition.Fields, headerDefinition.Name);
                            differences.AddRange(headerDifferences);

                            // Compare trailers
                            var trailerDifferences = CompareSections(expectedTrailer, actualTrailer, trailerDefinition.Fields, trailerDefinition.Name);
                            differences.AddRange(trailerDifferences);

                            // Compare content rows
                            foreach (var contentDefinition in contentDefinitions)
                            {
                                var keyFieldName = contentDefinition.KeyFieldName;
                                var identifierFieldName = contentDefinition.IdentifierFieldName;

                                FileHelper.AppendLine(logFilePath, $"Comparing content definition: {contentDefinition.IdentifierFieldName}: {contentDefinition.Name}");
                                var expectedRows = expectedContentRows.Where(row => row.ContainsKey(identifierFieldName) && row[identifierFieldName] == contentDefinition.Name).ToList();
                                var actualRows = actualContentRows.Where(row => row.ContainsKey(identifierFieldName) && row[identifierFieldName] == contentDefinition.Name).ToList();

                                if (expectedRows.Count == 0)
                                {
                                    isSuccess = false;
                                    FileHelper.AppendLine(logFilePath, $"No rows found in the expected file for content definition '{contentDefinition.IdentifierFieldName}: {contentDefinition.Name}' in file: {Path.GetFileName(expectedFile)}");
                                    errorMessage?.AppendLine($"No rows found in the expected file for content definition '{contentDefinition.IdentifierFieldName}: {contentDefinition.Name}' in file: {Path.GetFileName(expectedFile)}");
                                    return isSuccess;
                                }

                                if (actualRows.Count == 0)
                                {
                                    isSuccess = false;
                                    FileHelper.AppendLine(logFilePath, $"No rows found in the actual file for content definition '{contentDefinition.IdentifierFieldName}: {contentDefinition.Name}' in file: {Path.GetFileName(actualFile)}");
                                    errorMessage?.AppendLine($"No rows found in the actual file for content definition '{contentDefinition.IdentifierFieldName}: {contentDefinition.Name}' in file: {Path.GetFileName(actualFile)}");
                                    return isSuccess;
                                }

                                var keyIndex = contentDefinition.Fields.FindIndex(f => f.Name == keyFieldName);
                                var keyLength = contentDefinition.Fields[keyIndex].Length;

                                var expectedGroupedRows = expectedRows.GroupBy(row => row[keyFieldName].Substring(0, keyLength));
                                var actualGroupedRows = actualRows.GroupBy(row => row[keyFieldName].Substring(0, keyLength));

                                foreach (var group in expectedGroupedRows)
                                {
                                    var key = group.Key;
                                    FileHelper.AppendLine(logFilePath, $"Comparison Started for Row with Key: {key}");

                                    var expectedGroupRows = group.ToList();
                                    var actualGroupRows = actualGroupedRows.FirstOrDefault(g => g.Key == key)?.ToList();

                                    if (actualGroupRows == null)
                                    {
                                        differences.Add($"Key {key} not found in Actual file for Field: {contentDefinition.IdentifierFieldName}: {contentDefinition.Name}.");
                                        continue;
                                    }

                                    // Build order-independent bags keyed by row signature (non-ignored fields)
                                    var ignoredFieldNames = contentDefinition.Fields
                                        .Where(f => f.Ignore)
                                        .Select(f => f.Name)
                                        .ToHashSet(StringComparer.Ordinal);

                                    var expectedBag = BuildFlatRowBag(expectedGroupRows, ignoredFieldNames);
                                    var actualBag = BuildFlatRowBag(actualGroupRows, ignoredFieldNames);

                                    // Cancel out exact matches first
                                    CancelFlatExactMatches(expectedBag, actualBag);

                                    // Align remaining unmatched rows and collect differences
                                    AlignAndCollectFlatDifferences(key, contentDefinition, expectedBag, actualBag, ignoredFieldNames, differences, Path.GetFileName(actualFile));

                                    FileHelper.AppendLine(logFilePath, $"Comparison Ended for Row with Key: {key}");
                                }

                                foreach (var group in actualGroupedRows)
                                {
                                    var key = group.Key;
                                    if (!expectedGroupedRows.Any(g => g.Key == key))
                                    {
                                        differences.Add($"Key {key} not found in Expected file for {contentDefinition.IdentifierFieldName}: {contentDefinition.Name}.");
                                    }
                                }

                                FileHelper.AppendLine(logFilePath, $"Finished comparing content definition: {contentDefinition.IdentifierFieldName}: {contentDefinition.Name}");
                            }
                            allDifferences.AddRange(differences);
                        }
                        catch (Exception ex)
                        {
                            allErrors.Add($"Error comparing files: {Path.GetFileName(expectedFile)} and {Path.GetFileName(actualFile)}: {ex.Message}");
                        }
                    }

                    foreach (var difference in allDifferences)
                    {
                        FileHelper.AppendLine(logFilePath, difference);
                        errorMessage?.AppendLine(difference);
                    }

                    if (allDifferences.Count > 0)
                    {
                        FileHelper.AppendLine(logFilePath, $"Comparison Failed: Differences found in files");
                        errorMessage?.AppendLine($"Comparison Failed: Differences found in files for component: {componentName}");
                        isSuccess = false;
                    }

                    if (allErrors.Count > 0)
                    {
                        FileHelper.AppendLine(logFilePath, $"Comparison Failed: Errors occurred during comparison: {string.Join(Environment.NewLine, allErrors)}");
                        errorMessage?.AppendLine($"Comparison Failed: Errors occurred during comparison for component: {componentName}: {string.Join(Environment.NewLine, allErrors)}");
                        isSuccess = false;
                    }

                    FileHelper.AppendLine(logFilePath, $"{componentName} Files Comparison Ended");
                    return isSuccess;
                }
                else
                {
                    isSuccess = false;
                    FileHelper.AppendLine(logFilePath, $"Actual files not generated in {actualFilesPath}");
                    LogResult(logFilePath, isSuccess, componentName, errorMessage);
                    errorMessage?.AppendLine($"Actual files not generated in {actualFilesPath}");
                    return isSuccess;
                }

            }
            else
            {
                isSuccess = false;
                FileHelper.AppendLine(logFilePath, $"Expected files not generated in {expectedFilesPath}");
                LogResult(logFilePath, isSuccess, componentName, errorMessage);
                errorMessage?.AppendLine($"Expected files not generated in {expectedFilesPath}");
                return isSuccess;
            }

        }

        private Dictionary<string, string> ParseFields(string line, List<FieldOptions> fieldDefinitions)
        {
            var fields = new Dictionary<string, string>();
            int currentIndex = 0;

            foreach (var field in fieldDefinitions)
            {
                try
                {
                    if (currentIndex + field.Length > line.Length)
                    {
                        throw new ArgumentOutOfRangeException($"Field '{field.Name}' is missing in the line.");
                    }
                    fields[field.Name] = line.Substring(currentIndex, field.Length);
                    currentIndex += field.Length;
                }
                catch (ArgumentOutOfRangeException ex)
                {
                    FileHelper.AppendLine(logFilePath, ex.Message);
                    throw new Exception($"Error parsing field '{field.Name}': {ex.Message}");
                }
            }

            // Check for extra content after the last field
            if (currentIndex < line.Length)
            {
                string extraContent = line.Substring(currentIndex);
                fields["__EXTRA__"] = extraContent; // Special key to track extra data
            }

            return fields;
        }

        private Dictionary<string, string> ParseContentRow(string line, List<SectionOptions> contentDefinitions)
        {
            foreach (var contentDefinition in contentDefinitions)
            {
                var identifierField = contentDefinition.Fields.First(f => f.Name == contentDefinition.IdentifierFieldName);
                var identifierStartIndex = contentDefinition.Fields.TakeWhile(f => f.Name != contentDefinition.IdentifierFieldName).Sum(f => f.Length);
                if (line.Length < identifierStartIndex + identifierField.Length) continue;
                var identifierValue = line.Substring(identifierStartIndex, identifierField.Length).Trim();

                if (identifierValue == contentDefinition.Name)
                {
                    return ParseFields(line, contentDefinition.Fields);
                }
            }

            return [];
        }

        private List<string> CompareSections(Dictionary<string, string> section1, Dictionary<string, string> section2, List<FieldOptions> fieldDefinitions, string sectionName)
        {
            FileHelper.AppendLine(logFilePath, $"Comparing {sectionName} section");
            var differences = new List<string>();

            foreach (var field in fieldDefinitions)
            {
                if (!field.Ignore && section1[field.Name] != section2[field.Name])
                {
                    differences.Add($"{sectionName} difference in {field.Name}: {Environment.NewLine} Expected: {section1[field.Name]} vs Actual: {section2[field.Name]}");
                }
            }
            FileHelper.AppendLine(logFilePath, $"Finished comparing {sectionName} section");
            return differences;
        }

        public bool CompareDelimitedFiles(int keyColumnIndex, int[] ignoreColumnIndices, string comparisonFiles = null, string[] fileExtensions = null)
        {
            var patterns = BuildFilePatterns(filePrefix, fileExtensions);
            var expectedFiles = GetSortedFiles(expectedFilesPath, patterns);
            var actualFiles = GetSortedFiles(actualFilesPath, patterns);
            bool isSuccess = true;

            if (expectedFiles.Length == 0)
            {
                isSuccess = false;
                FileHelper.AppendLine(logFilePath, $"Expected files not generated in {expectedFilesPath}");
                LogResult(logFilePath, isSuccess, comparisonFiles, errorMessage);
                errorMessage?.AppendLine($"Expected files not generated in {expectedFilesPath}");
                return isSuccess;
            }

            if (actualFiles.Length == 0)
            {
                isSuccess = false;
                FileHelper.AppendLine(logFilePath, $"Actual files not generated in {actualFilesPath}");
                LogResult(logFilePath, isSuccess, comparisonFiles, errorMessage);
                errorMessage?.AppendLine($"Actual files not generated in {actualFilesPath}");
                return isSuccess;
            }

            if (keyColumnIndex < 0)
            {
                isSuccess = false;
                FileHelper.AppendLine(logFilePath, $"Key column index '{keyColumnIndex}' is invalid (must be >= 0).");
                errorMessage?.AppendLine($"Key column index '{keyColumnIndex}' is invalid (must be >= 0).");
                return isSuccess;
            }

            var ignoreSet = new HashSet<int>(ignoreColumnIndices ?? Array.Empty<int>());
            var allDifferences = new List<string>();
            var allErrors = new List<string>();

            FileHelper.AppendLine(logFilePath, $"{componentName} Files Comparison Started");
            int fileCount = Math.Min(expectedFiles.Length, actualFiles.Length);
            for (int i = 0; i < fileCount; i++)
            {
                string expectedFile = expectedFiles[i];
                string actualFile = actualFiles[i];

                try
                {
                    FileHelper.AppendLine(logFilePath, $"Comparing files: Expected='{Path.GetFileName(expectedFile)}' Actual='{Path.GetFileName(actualFile)}'");

                    var expectedRows = ReadDelimitedNoHeader(expectedFile);
                    var actualRows = ReadDelimitedNoHeader(actualFile);

                    if (expectedRows.Count == 0)
                    {
                        isSuccess = false;
                        FileHelper.AppendLine(logFilePath, $"No rows found in expected file: {Path.GetFileName(expectedFile)}");
                        errorMessage?.AppendLine($"No rows found in expected file: {Path.GetFileName(expectedFile)}");
                        return isSuccess;
                    }

                    if (actualRows.Count == 0)
                    {
                        isSuccess = false;
                        FileHelper.AppendLine(logFilePath, $"No rows found in actual file: {Path.GetFileName(actualFile)}");
                        errorMessage?.AppendLine($"No rows found in actual file: {Path.GetFileName(actualFile)}");
                        return isSuccess;
                    }

                    var expectedGroups = GroupRowsByKey(expectedRows, keyColumnIndex);
                    var actualGroups = GroupRowsByKey(actualRows, keyColumnIndex);

                    // Compare each key present in expected
                    foreach (var kvp in expectedGroups)
                    {
                        var key = kvp.Key;
                        FileHelper.AppendLine(logFilePath, $"Comparison Started for Key: {key}");

                        var expectedGroupRows = kvp.Value;
                        if (!actualGroups.TryGetValue(key, out var actualGroupRows))
                        {
                            allDifferences.Add($"Key '{key}' not found in actual file: {Path.GetFileName(actualFile)}");
                            continue;
                        }

                        var expectedBag = BuildRowBagWithSamples(expectedGroupRows, ignoreSet);
                        var actualBag = BuildRowBagWithSamples(actualGroupRows, ignoreSet);

                        CancelExactMatches(expectedBag, actualBag);

                        AlignAndCollectDifferences(key, expectedBag, actualBag, ignoreSet, allDifferences);

                        FileHelper.AppendLine(logFilePath, $"Comparison Ended for Key: {key}");
                    }

                    // Keys only in actual
                    foreach (var key in actualGroups.Keys.Where(k => !expectedGroups.ContainsKey(k)))
                    {
                        allDifferences.Add($"Key '{key}' not found in expected file: {Path.GetFileName(expectedFile)}");
                    }
                }
                catch (Exception ex)
                {
                    allErrors.Add($"Error comparing files: {Path.GetFileName(expectedFile)} vs {Path.GetFileName(actualFile)}: {ex.Message}");
                }
            }

            // Log all differences/errors
            foreach (var diff in allDifferences)
            {
                FileHelper.AppendLine(logFilePath, diff);
                errorMessage?.AppendLine(diff);
            }

            if (allDifferences.Count > 0)
            {
                FileHelper.AppendLine(logFilePath, "Comparison Failed: Differences found in files");
                errorMessage?.AppendLine($"Comparison Failed: Differences found in files for component: {componentName}");
                isSuccess = false;
            }

            if (allErrors.Count > 0)
            {
                FileHelper.AppendLine(logFilePath,
                    $"Comparison Failed: Errors occurred during comparison:{Environment.NewLine}{string.Join(Environment.NewLine, allErrors)}");
                errorMessage?.AppendLine($"Comparison Failed: Errors occurred during comparison for component: {componentName}:{Environment.NewLine}{string.Join(Environment.NewLine, allErrors)}");
                isSuccess = false;
            }
            FileHelper.AppendLine(logFilePath, $"{componentName} Files Comparison Ended");
            return isSuccess;
        }

        /// <summary>
        /// Backward-compatible wrapper. Delegates to <see cref="CompareDelimitedFiles"/> with .csv extension.
        /// </summary>
        [Obsolete("Use CompareDelimitedFiles with fileExtensions parameter instead.")]
        public bool CompareCsvFiles(int keyColumnIndex, int[] ignoreColumnIndices, string comparisonFiles = null)
            => CompareDelimitedFiles(keyColumnIndex, ignoreColumnIndices, comparisonFiles);

        private static List<string[]> ReadDelimitedNoHeader(string filePath)
        {
            using var reader = new StreamReader(filePath);
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = false,
                Delimiter = ",",
                BadDataFound = null,
                TrimOptions = TrimOptions.None
            };
            using var csv = new CsvReader(reader, config);

            var rows = new List<string[]>();
            while (csv.Read())
            {
                var record = csv.Parser.Record;
                rows.Add([.. record]);
            }
            return rows;
        }

        private static Dictionary<string, List<string[]>> GroupRowsByKey(List<string[]> rows, int keyColumnIndex)
        {
            return rows
                .GroupBy(r => SafeIndex(r, keyColumnIndex) ?? string.Empty)
                .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.Ordinal);
        }

        private static Dictionary<string, (int count, string[] sample)> BuildRowBagWithSamples(IEnumerable<string[]> rows, HashSet<int> ignoreSet)
        {
            var bag = new Dictionary<string, (int count, string[] sample)>(StringComparer.Ordinal);
            foreach (var row in rows)
            {
                var sig = BuildRowSignature(row, ignoreSet);
                bag[sig] = bag.TryGetValue(sig, out var entry)
                    ? (entry.count + 1, entry.sample)
                    : (1, row);
            }
            return bag;
        }

        private static void CancelExactMatches(Dictionary<string, (int count, string[] sample)> expectedBag,
                                        Dictionary<string, (int count, string[] sample)> actualBag)
        {
            foreach (var sig in expectedBag.Keys.ToList())
            {
                var expCount = expectedBag[sig].count;
                if (!actualBag.TryGetValue(sig, out var aEntry)) continue;

                var actCount = aEntry.count;
                var cancel = Math.Min(expCount, actCount);
                if (cancel == 0) continue;

                expectedBag[sig] = (expCount - cancel, expectedBag[sig].sample);
                actualBag[sig] = (actCount - cancel, aEntry.sample);
            }
        }

        private static void AlignAndCollectDifferences(
            string key,
            Dictionary<string, (int count, string[] sample)> expectedBag,
            Dictionary<string, (int count, string[] sample)> actualBag,
            HashSet<int> ignoreSet,
            List<string> differences)
        {
            var missingSigs = expectedBag.Where(kv => kv.Value.count > 0).Select(kv => kv.Key).ToList();
            var extraSigs = actualBag.Where(kv => kv.Value.count > 0).Select(kv => kv.Key).ToList();

            // Align missing rows to closest extras for per-field reporting
            foreach (var msig in missingSigs)
            {
                var mCount = expectedBag[msig].count;
                var mSample = expectedBag[msig].sample;

                for (int m = 0; m < mCount; m++)
                {
                    string? bestSig = null;
                    string[]? bestSample = null;
                    int bestDiffs = int.MaxValue;

                    foreach (var esig in extraSigs)
                    {
                        var eCount = actualBag[esig].count;
                        if (eCount == 0) continue;

                        var eSample = actualBag[esig].sample;
                        var diffs = CountFieldDiffs(mSample, eSample, ignoreSet);
                        if (diffs < bestDiffs)
                        {
                            bestDiffs = diffs;
                            bestSig = esig;
                            bestSample = eSample;
                            if (bestDiffs == 0) break;
                        }
                    }

                    if (bestSample is not null && bestSig is not null)
                    {
                        var fieldDiffs = GetFieldDiffs(mSample, bestSample, ignoreSet);
                        foreach (var (index, expected, actual) in fieldDiffs)
                        {
                            differences.Add($"Key '{key}': Field {index} mismatch{Environment.NewLine}Expected: {expected}{Environment.NewLine}Actual: {actual}");
                        }

                        // consume one extra instance
                        var (count, sample) = actualBag[bestSig];
                        actualBag[bestSig] = (count - 1, sample);
                        if (actualBag[bestSig].count == 0)
                            extraSigs.Remove(bestSig);
                    }
                    else
                    {
                        differences.Add($"Key '{key}': Missing row{Environment.NewLine}Expected: {FormatRowForLog(mSample)}{Environment.NewLine}Actual: <none>");
                    }
                }
            }

            // Remaining extras
            foreach (var esig in actualBag.Keys)
            {
                var eCount = actualBag[esig].count;
                var eSample = actualBag[esig].sample;
                for (int e = 0; e < eCount; e++)
                {
                    differences.Add($"Key '{key}': Extra row{Environment.NewLine}Expected: <none>{Environment.NewLine}Actual: {FormatRowForLog(eSample)}");
                }
            }
        }

        private static string BuildRowSignature(string[] row, HashSet<int> ignoreSet)
        {
            var parts = new List<string>(row.Length);
            for (int i = 0; i < row.Length; i++)
            {
                if (ignoreSet.Contains(i)) continue;
                parts.Add(row[i] ?? string.Empty);
            }
            return string.Join("\u001F", parts);
        }

        private static int CountFieldDiffs(string[] expected, string[] actual, HashSet<int> ignoreSet)
        {
            int maxLen = Math.Max(expected.Length, actual.Length);
            int diffs = 0;
            for (int i = 0; i < maxLen; i++)
            {
                if (ignoreSet.Contains(i)) continue;
                var e = i < expected.Length ? expected[i] : string.Empty;
                var a = i < actual.Length ? actual[i] : string.Empty;
                if (!string.Equals(e, a, StringComparison.Ordinal))
                    diffs++;
            }
            return diffs;
        }

        private static List<(int index, string expected, string actual)> GetFieldDiffs(string[] expected, string[] actual, HashSet<int> ignoreSet)
        {
            var result = new List<(int, string, string)>();
            int maxLen = Math.Max(expected.Length, actual.Length);
            for (int i = 0; i < maxLen; i++)
            {
                if (ignoreSet.Contains(i)) continue;
                var e = i < expected.Length ? expected[i] : string.Empty;
                var a = i < actual.Length ? actual[i] : string.Empty;
                if (!string.Equals(e, a, StringComparison.Ordinal))
                    result.Add((i, e, a));
            }
            return result;
        }

        private static string FormatRowForLog(string[] row)
        {
            var parts = new List<string>(row.Length);

            parts.AddRange(row.Select(raw =>
            {
                var v = raw ?? string.Empty;
                if (v.Length == 0)
                    return "\"\"";
                if (v.Contains(',') || v.Contains('"') || v.Contains('\r') || v.Contains('\n'))
                    return $"\"{v.Replace("\"", "\"\"")}\"";
                return v;
            }));

            return string.Join(",", parts);
        }

        private static string? SafeIndex(string[] row, int index)
        {
            return index >= 0 && index < row.Length ? row[index] : null;
        }

        /// <summary>
        /// Compares all expected vs actual Excel files matching the given prefix.
        /// </summary>
        public bool CompareExcelFiles(
            string expectedFilesPath,
            string actualFilesPath,
            string filePrefix,
            string comparisonFiles,
            string keyColumnName,
            List<string> ignoreColumnNames = null,
            int headerRowIndex = 1)
        {
            FileHelper.AppendLine(logFilePath, new string('-', 100));


            string[] expectedFiles = GetSortedFiles(expectedFilesPath, filePrefix + "*");
            string[] actualFiles = GetSortedFiles(actualFilesPath, filePrefix + "*");

            bool allMatched = true;

            if (expectedFiles.Length == 0)
            {
                allMatched = false;
                FileHelper.AppendLine(logFilePath, $"Expected files not generated in {expectedFilesPath}");
                LogResult(logFilePath, allMatched, comparisonFiles, errorMessage);
                errorMessage?.AppendLine($"Expected files not generated in {expectedFilesPath}");
                return allMatched;
            }


            if (actualFiles.Length == 0)
            {
                allMatched = false;
                FileHelper.AppendLine(logFilePath, $"Actual files not generated in {actualFilesPath}");
                LogResult(logFilePath, allMatched, comparisonFiles, errorMessage);
                errorMessage?.AppendLine($"Actual files not generated in {actualFilesPath}");
                return allMatched;
            }


            int pairCount = Math.Min(expectedFiles.Length, actualFiles.Length);
            FileHelper.AppendLine(logFilePath, $"{comparisonFiles} Files Comparison Started");
            for (int i = 0; i < pairCount; i++)
            {
                string expected = expectedFiles[i];
                string actual = actualFiles[i];

                List<string> differences = CompareExcelFilesByKeyColumnName(
                    expected,
                    actual,
                    keyColumnName,
                    ignoreColumnNames,
                    headerRowIndex,
                    componentName,
                    errorMessage);

                if (differences.Count > 0)
                {
                    allMatched = false;
                    FileHelper.AppendLine(logFilePath,
                        $"Differences found between:{Environment.NewLine}Expected: {Path.GetFileName(expected)}{Environment.NewLine}Actual: {Path.GetFileName(actual)}");
                    errorMessage?.AppendLine(
                        $"Differences found between files for component: {componentName}:{Environment.NewLine}Expected: {Path.GetFileName(expected)}{Environment.NewLine}Actual: {Path.GetFileName(actual)}");
                    foreach (string diff in differences)
                    {
                        FileHelper.AppendLine(logFilePath, diff);
                        errorMessage?.AppendLine(diff);
                    }

                }
                else
                {
                    FileHelper.AppendLine(logFilePath,
                        $"Success: Files matched:{Environment.NewLine}Expected: {Path.GetFileName(expected)}{Environment.NewLine}Actual: {Path.GetFileName(actual)}");
                }
            }

            if (expectedFiles.Length != actualFiles.Length)
            {
                allMatched = false;
                FileHelper.AppendLine(logFilePath,
                    $"File count mismatch: Expected {expectedFiles.Length}, Actual {actualFiles.Length}");
                errorMessage?.AppendLine(
                    $"File count mismatch for component: {componentName}: Expected {expectedFiles.Length}, Actual {actualFiles.Length}");
            }
            FileHelper.AppendLine(logFilePath, $"{comparisonFiles} Files Comparison Ended");
            return allMatched;
        }

        /// <summary>
        /// Internal comparison logic for a single expected vs actual file pair using a key column.
        /// </summary>
        private static List<string> CompareExcelFilesByKeyColumnName(
            string expectedFilePath,
            string actualFilePath,
            string keyColumnName,
            List<string> ignoreColumnNames = null,
            int headerRowIndex = 1, string componentName = null, StringBuilder errorMessage = null)
        {
            var differences = new List<string>();

            ignoreColumnNames ??= new List<string>();
            if (headerRowIndex < 1) headerRowIndex = 1;

            using var expectedWorkbook = new XLWorkbook(expectedFilePath);
            using var actualWorkbook = new XLWorkbook(actualFilePath);

            var expectedSheet = expectedWorkbook.Worksheet(1);
            var actualSheet = actualWorkbook.Worksheet(1);

            var (expectedHeaderRow, expectedHeaderMap) = BuildHeaderMap(expectedSheet, headerRowIndex, keyColumnName);
            if (!expectedHeaderMap.TryGetValue(keyColumnName, out int expectedKeyColIndex))
            {
                differences.Add($"Key column '{keyColumnName}' not found in Expected file. Scanned rows {headerRowIndex} to {headerRowIndex + 4}.");
                return differences;
            }


            var (actualHeaderRow, actualHeaderMap) = BuildHeaderMap(actualSheet, headerRowIndex, keyColumnName);
            if (!actualHeaderMap.TryGetValue(keyColumnName, out int actualKeyColIndex))
            {
                differences.Add($"Key column '{keyColumnName}' not found in actual file. Scanned rows {headerRowIndex} to {headerRowIndex + 4}.");
                return differences;
            }

            // Pre-header row comparison logic (if header moved downward in either file).
            bool expectedHeaderMoved = expectedHeaderRow.RowNumber() > headerRowIndex &&
                                       expectedSheet.Row(headerRowIndex).CellsUsed().Any();
            bool actualHeaderMoved = actualHeaderRow.RowNumber() > headerRowIndex &&
                                     actualSheet.Row(headerRowIndex).CellsUsed().Any();

            if (expectedHeaderMoved || actualHeaderMoved)
            {
                var originalExpectedRow = expectedSheet.Row(headerRowIndex);
                var originalActualRow = actualSheet.Row(headerRowIndex);

                int maxCol = Math.Max(
                    originalExpectedRow.CellsUsed().Select(c => c.Address.ColumnNumber).DefaultIfEmpty(0).Max(),
                    originalActualRow.CellsUsed().Select(c => c.Address.ColumnNumber).DefaultIfEmpty(0).Max());

                for (int col = 1; col <= maxCol; col++)
                {
                    string expectedVal = originalExpectedRow.Cell(col).GetValue<string>().Trim();
                    string actualVal = originalActualRow.Cell(col).GetValue<string>().Trim();

                    if (!string.Equals(expectedVal, actualVal, StringComparison.Ordinal))
                    {
                        differences.Add($"Pre-header row difference (row {headerRowIndex}), Column {col}: Expected '{expectedVal}' vs Actual '{actualVal}'");
                    }
                }

                if (expectedHeaderMoved && !actualHeaderMoved)
                    differences.Add($"Pre-header row present in expected but absent in actual at row {headerRowIndex}.");
                else if (!expectedHeaderMoved && actualHeaderMoved)
                    differences.Add($"Pre-header row present in actual but absent in expected at row {headerRowIndex}.");
            }

            var ignoredColumns = new HashSet<string>(ignoreColumnNames, StringComparer.OrdinalIgnoreCase);

            // Build key -> row map for expected
            var expectedRowsByKey = new Dictionary<string, IXLRangeRow>(StringComparer.OrdinalIgnoreCase);
            var expectedRange = expectedSheet.RangeUsed();
            if (expectedRange != null)
            {
                int startDataRow = expectedHeaderRow.RowNumber() + 1;
                foreach (var row in expectedRange.RowsUsed().Where(r => r.RowNumber() >= startDataRow))
                {
                    string key = row.Cell(expectedKeyColIndex).GetValue<string>().Trim();
                    if (!string.IsNullOrEmpty(key))
                        expectedRowsByKey[key] = row;
                }
            }

            // Build key -> row map for actual
            var actualRowsByKey = new Dictionary<string, IXLRangeRow>(StringComparer.OrdinalIgnoreCase);
            var actualRange = actualSheet.RangeUsed();
            if (actualRange != null)
            {
                int startDataRow = actualHeaderRow.RowNumber() + 1;
                foreach (var row in actualRange.RowsUsed().Where(r => r.RowNumber() >= startDataRow))
                {
                    string key = row.Cell(actualKeyColIndex).GetValue<string>().Trim();
                    if (!string.IsNullOrEmpty(key))
                        actualRowsByKey[key] = row;
                }
            }

            // Compare rows by key
            foreach (var key in expectedRowsByKey.Keys)
            {
                if (!actualRowsByKey.TryGetValue(key, out var actualRow))
                {
                    differences.Add($"Missing key '{key}' in actual file.");
                    continue;
                }

                var expectedRow = expectedRowsByKey[key];

                foreach (var headerName in expectedHeaderMap.Keys)
                {
                    if (headerName.Equals(keyColumnName, StringComparison.OrdinalIgnoreCase))
                        continue;

                    if (ignoredColumns.Contains(headerName))
                        continue;

                    int expectedColIndex = expectedHeaderMap[headerName];

                    if (!actualHeaderMap.TryGetValue(headerName, out int actualColIndex))
                    {
                        differences.Add($"Key '{key}': Column '{headerName}' missing in actual file header.");
                        continue;
                    }

                    string expectedValue = expectedRow.Cell(expectedColIndex).GetValue<string>();
                    string actualValue = actualRow.Cell(actualColIndex).GetValue<string>();

                    if (!string.Equals(expectedValue, actualValue, StringComparison.Ordinal))
                        differences.Add($"Key '{key}', Column '{headerName}': Expected '{expectedValue}' vs Actual '{actualValue}'");
                }
            }

            // Detect extra keys in actual
            foreach (var key in actualRowsByKey.Keys)
            {
                if (!expectedRowsByKey.ContainsKey(key))
                    differences.Add($"Extra key '{key}' found in actual file.");
            }
            errorMessage?.AppendLine(
                $"Finished comparing files for component: {componentName}: {Path.GetFileName(expectedFilePath)} vs {Path.GetFileName(actualFilePath)}. " +
                $"Differences found ({differences.Count}): {string.Join("; ", differences)}");
            return differences;
        }

        /// <summary>
        /// Scans for header row and builds a column name -> column index map.
        /// </summary>
        private static (IXLRow Row, Dictionary<string, int> Map) BuildHeaderMap(IXLWorksheet sheet, int startRow, string requiredKey)
        {
            Dictionary<string, int> lastNonEmptyMap = null;
            IXLRow lastNonEmptyRow = null;

            for (int r = startRow; r <= startRow + 4; r++)
            {
                var row = sheet.Row(r);
                var cells = row.CellsUsed()
                               .Select(c => (Name: c.GetValue<string>().Trim(), Col: c.Address.ColumnNumber))
                               .Where(t => !string.IsNullOrEmpty(t.Name))
                               .ToList();

                if (cells.Count == 0)
                    continue;

                var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                foreach (var cell in cells)
                    map[cell.Name] = cell.Col;

                lastNonEmptyMap = map;
                lastNonEmptyRow = row;

                if (map.ContainsKey(requiredKey))
                    return (row, map);
            }

            if (lastNonEmptyMap == null || lastNonEmptyRow == null)
                return (sheet.Row(startRow), new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase));

            return (lastNonEmptyRow, lastNonEmptyMap);
        }

        /// <summary>
        /// Builds an order-independent bag for flat file rows.
        /// Key = unit-separator-joined values of non-ignored fields.
        /// Value = (occurrence count, one sample row dictionary).
        /// </summary>
        private static Dictionary<string, (int count, Dictionary<string, string> sample)> BuildFlatRowBag(
            IEnumerable<Dictionary<string, string>> rows,
            HashSet<string> ignoredFieldNames)
        {
            var bag = new Dictionary<string, (int count, Dictionary<string, string> sample)>(StringComparer.Ordinal);
            foreach (var row in rows)
            {
                var sig = BuildFlatRowSignature(row, ignoredFieldNames);
                bag[sig] = bag.TryGetValue(sig, out var entry)
                    ? (entry.count + 1, entry.sample)
                    : (1, row);
            }
            return bag;
        }

        /// <summary>
        /// Builds a signature string from a flat file row, excluding ignored fields and the __EXTRA__ sentinel.
        /// </summary>
        private static string BuildFlatRowSignature(Dictionary<string, string> row, HashSet<string> ignoredFieldNames)
        {
            var parts = row
                .Where(kv => kv.Key != "__EXTRA__" && !ignoredFieldNames.Contains(kv.Key))
                .OrderBy(kv => kv.Key, StringComparer.Ordinal)
                .Select(kv => kv.Value ?? string.Empty);
            return string.Join("\u001F", parts);
        }

        /// <summary>
        /// Cancels out rows that are identical (exact signature match) between expected and actual bags.
        /// </summary>
        private static void CancelFlatExactMatches(
            Dictionary<string, (int count, Dictionary<string, string> sample)> expectedBag,
            Dictionary<string, (int count, Dictionary<string, string> sample)> actualBag)
        {
            foreach (var sig in expectedBag.Keys.ToList())
            {
                if (!actualBag.TryGetValue(sig, out var aEntry)) continue;

                var expCount = expectedBag[sig].count;
                var actCount = aEntry.count;
                var cancel = Math.Min(expCount, actCount);
                if (cancel == 0) continue;

                expectedBag[sig] = (expCount - cancel, expectedBag[sig].sample);
                actualBag[sig] = (actCount - cancel, aEntry.sample);
            }
        }

        /// <summary>
        /// Aligns unmatched expected rows to their closest actual counterpart and reports per-field differences.
        /// Any remaining actual rows are reported as extra rows.
        /// </summary>
        private static void AlignAndCollectFlatDifferences(
            string key,
            SectionOptions contentDefinition,
            Dictionary<string, (int count, Dictionary<string, string> sample)> expectedBag,
            Dictionary<string, (int count, Dictionary<string, string> sample)> actualBag,
            HashSet<string> ignoredFieldNames,
            List<string> differences,
            string actualFileName)
        {
            var missingSigs = expectedBag.Where(kv => kv.Value.count > 0).Select(kv => kv.Key).ToList();
            var extraSigs = actualBag.Where(kv => kv.Value.count > 0).Select(kv => kv.Key).ToList();

            foreach (var msig in missingSigs)
            {
                var mCount = expectedBag[msig].count;
                var mSample = expectedBag[msig].sample;

                for (int m = 0; m < mCount; m++)
                {
                    // Find the closest matching actual row by fewest field differences
                    string bestSig = null;
                    Dictionary<string, string> bestSample = null;
                    int bestDiffs = int.MaxValue;

                    foreach (var esig in extraSigs)
                    {
                        if (actualBag[esig].count == 0) continue;
                        var eSample = actualBag[esig].sample;
                        int diffs = CountFlatFieldDiffs(mSample, eSample, contentDefinition.Fields, ignoredFieldNames);
                        if (diffs < bestDiffs)
                        {
                            bestDiffs = diffs;
                            bestSig = esig;
                            bestSample = eSample;
                            if (bestDiffs == 0) break;
                        }
                    }

                    if (bestSample is not null && bestSig is not null)
                    {
                        var differencesInRow = new List<string>();
                        foreach (var field in contentDefinition.Fields)
                        {
                            if (ignoredFieldNames.Contains(field.Name)) continue;

                            var expectedVal = mSample.TryGetValue(field.Name, out var ev) ? ev : string.Empty;
                            var actualVal = bestSample.TryGetValue(field.Name, out var av) ? av : string.Empty;

                            if (expectedVal != actualVal)
                                differencesInRow.Add($"{field.Name}: Expected: {expectedVal} vs Actual: {actualVal}");
                        }

                        if (bestSample.TryGetValue("__EXTRA__", out string extraContent))
                            differencesInRow.Add($"Extra content in actual file after last expected field: '{extraContent}'");

                        if (differencesInRow.Count > 0)
                            differences.Add($"Difference for key {key} in Record Type {contentDefinition.IdentifierFieldName}: {contentDefinition.Name}:{Environment.NewLine} {string.Join(Environment.NewLine, differencesInRow)}");

                        // Consume one instance of the matched actual row
                        var (count, sample) = actualBag[bestSig];
                        actualBag[bestSig] = (count - 1, sample);
                        if (actualBag[bestSig].count == 0)
                            extraSigs.Remove(bestSig);
                    }
                    else
                    {
                        differences.Add($"Extra row in Expected file for key {key} in Record Type {contentDefinition.IdentifierFieldName}: {contentDefinition.Name}: {string.Join(",", mSample.Values)}");
                    }
                }
            }

            // Remaining actual rows with no expected counterpart
            foreach (var esig in actualBag.Keys)
            {
                var eCount = actualBag[esig].count;
                var eSample = actualBag[esig].sample;
                for (int e = 0; e < eCount; e++)
                {
                    differences.Add($"Extra row in Actual file for key {key} in Record Type {contentDefinition.IdentifierFieldName}: {contentDefinition.Name}: {string.Join(",", eSample.Values)}");
                }
            }
        }

        /// <summary>
        /// Counts the number of differing non-ignored fields between two flat file row dictionaries.
        /// </summary>
        private static int CountFlatFieldDiffs(
            Dictionary<string, string> expected,
            Dictionary<string, string> actual,
            List<FieldOptions> fields,
            HashSet<string> ignoredFieldNames)
        {
            int diffs = 0;
            foreach (var field in fields)
            {
                if (ignoredFieldNames.Contains(field.Name)) continue;
                var e = expected.TryGetValue(field.Name, out var ev) ? ev : string.Empty;
                var a = actual.TryGetValue(field.Name, out var av) ? av : string.Empty;
                if (e != a) diffs++;
            }
            return diffs;
        }

        /// <summary>
        /// Reads all JSON files from expected and actual folders, deserializes them as T,
        /// then checks for missing, extra, duplicate and mismatched records.
        /// </summary>
        public bool CompareJsonFiles<T>(
            string comparisonFiles,
            Func<T, object> keySelector) where T : class
        {
            return CompareJsonFiles<T>(
                expectedFilesPath,
                actualFilesPath,
                filePrefix,
                comparisonFiles,
                logFilePath,
                keySelector, errorMessage);
        }

        private static bool CompareJsonFiles<T>(
            string expectedFilesPath,
            string actualFilesPath,
            string filePrefix,
            string comparisonFiles,
            string logFilePath,
            Func<T, object> keySelector, StringBuilder errorMessage = null) where T : class
        {
            FileHelper.AppendLine(logFilePath, new string('-', 100));

            var sb = new StringBuilder();
            bool isSuccess;

            var expectedFiles = FileHelper.GetAllFiles(expectedFilesPath, filePrefix + "*.json");
            var actualFiles = FileHelper.GetAllFiles(actualFilesPath, filePrefix + "*.json");
            if (expectedFiles.Length <= 0)
            {
                isSuccess = false;
                sb.AppendLine($"Expected files not generated in {expectedFilesPath}");
                FileHelper.AppendLine(logFilePath, sb.ToString());
                LogResult(logFilePath, isSuccess, comparisonFiles, errorMessage);
                errorMessage?.AppendLine($"Expected files not generated in {expectedFilesPath}");
                return isSuccess;
            }
            else if (actualFiles.Length <= 0)
            {
                isSuccess = false;
                sb.AppendLine($"Actual files not generated in {actualFilesPath}");
                FileHelper.AppendLine(logFilePath, sb.ToString());
                LogResult(logFilePath, isSuccess, comparisonFiles, errorMessage);
                errorMessage?.AppendLine($"Actual files not generated in {actualFilesPath}");
                return isSuccess;
            }
            FileHelper.AppendLine(logFilePath, $"{comparisonFiles} Files Comparison Started");
            try
            {
                List<T> expectedList = [.. expectedFiles.Select(f => JsonSerializer.Deserialize<T>(FileHelper.GetAllText(f), JsonSerializerExtensions.DefaultSerializerOptions))];

                List<T> actualList = [.. actualFiles.Select(f => JsonSerializer.Deserialize<T>(FileHelper.GetAllText(f), JsonSerializerExtensions.DefaultSerializerOptions))];

                sb.AppendLine($"Expected count : {expectedList.Count}");
                sb.AppendLine($"Actual count : {actualList.Count}");

                isSuccess = EvaluateCompareList(expectedList, actualList, keySelector, sb, errorMessage);

                FileHelper.AppendLine(logFilePath, sb.ToString());
                LogResult(logFilePath, isSuccess, comparisonFiles, errorMessage);
                FileHelper.AppendLine(logFilePath, $"{comparisonFiles} Files Comparison Ended");
                return isSuccess;
            }
            catch (Exception ex)
            {
                FileHelper.AppendLine(logFilePath, sb.ToString());
                FileHelper.AppendLine(logFilePath, $"Exception: {ex.Message}. Stack: {ex.StackTrace}");
                throw;
            }
        }

        private static bool EvaluateCompareList<T, TKey>(
            List<T> expectedList,
            List<T> actualList,
            Func<T, TKey> keySelector,
            StringBuilder sb, StringBuilder errorMessage = null) where T : class
        {
            bool isSuccess = true;

            var compareList = expectedList
                .GroupJoin(actualList,
                    e => keySelector(e),
                    a => keySelector(a),
                    (e, a) => new { expected = e, actual = a })
                .ToArray();

            var missingList = compareList
                .Where(ae => !ae.actual.Any())
                .Select(mi => mi.expected)
                .ToList();

            if (missingList.Count != 0)
            {
                isSuccess = false;
                sb.AppendLine($"Missing Messages : {missingList.Count}");
                errorMessage?.AppendLine($"Missing Messages : {missingList.Count}");
                foreach (var missing in missingList)
                {
                    sb.AppendLine($" Key - {keySelector(missing)}");
                    errorMessage?.AppendLine($" Key - {keySelector(missing)}");
                }
                sb.AppendLine(new string('*', 100));
            }

            var extraList = actualList
                .GroupJoin(expectedList,
                    a => keySelector(a),
                    e => keySelector(e),
                    (a, e) => new { actual = a, expected = e })
                .Where(ae => !ae.expected.Any())
                .Select(mc => mc.actual)
                .ToList();

            if (extraList.Count != 0)
            {
                isSuccess = false;
                sb.AppendLine($"Extra Messages : {extraList.Count}");
                errorMessage?.AppendLine($"Extra Messages : {extraList.Count}");
                foreach (var extra in extraList)
                {
                    sb.AppendLine($" Key - {keySelector(extra)}");
                    errorMessage?.AppendLine($" Key - {keySelector(extra)}");
                }
                sb.AppendLine(new string('*', 100));
            }

            var duplicateList = compareList
                .Where(ae => ae.actual.Count() > 1)
                .ToList();

            if (duplicateList.Count != 0)
            {
                isSuccess = false;
                sb.AppendLine($"Duplicate Messages : {duplicateList.Count}");
                errorMessage?.AppendLine($"Duplicate Messages : {duplicateList.Count}");
                foreach (var duplicate in duplicateList)
                {
                    sb.AppendLine($" Expected:  Key - {keySelector(duplicate.expected)}");
                    errorMessage?.AppendLine($" Expected:  Key - {keySelector(duplicate.expected)}");
                    foreach (var actualDuplicate in duplicate.actual)
                    {
                        sb.AppendLine($" Actual  :  Key - {keySelector(actualDuplicate)}");
                        errorMessage?.AppendLine($" Actual  :  Key - {keySelector(actualDuplicate)}");
                    }

                }
                sb.AppendLine(new string('*', 100));
            }

            var mismatchList = compareList
                .Where(m => m.actual.Count() == 1)
                .Select(m => new
                {
                    m.expected,
                    actual = m.actual.First(),
                    differences = Comparer.Compare(m.expected, m.actual.First())
                })
                .Where(m => m.differences.Any())
                .ToList();

            if (mismatchList.Count != 0)
            {
                isSuccess = false;
                sb.AppendLine($"Mismatch Messages : {mismatchList.Count}");
                errorMessage?.AppendLine($"Mismatch Messages : {mismatchList.Count}");
                foreach (var mismatch in mismatchList)
                {
                    sb.AppendLine($" Key - {keySelector(mismatch.expected)}");
                    errorMessage?.AppendLine($" Key - {keySelector(mismatch.expected)}");
                    foreach (var diff in mismatch.differences)
                    {
                        sb.AppendLine($"  {diff.Name}: Expected - {diff.OldValue}, Actual - {diff.NewValue}");
                        errorMessage?.AppendLine($"  {diff.Name}: Expected - {diff.OldValue}, Actual - {diff.NewValue}");
                    }

                }
                sb.AppendLine(new string('*', 100));
            }
            return isSuccess;
        }

        private static void LogResult(string logFilePath, bool isSuccess, string comparisonFiles, StringBuilder errorMessage = null)
        {
            if (isSuccess)
            {
                FileHelper.AppendLine(logFilePath, $"No issues found in {comparisonFiles}");
                FileHelper.AppendLine(logFilePath, new string('-', 100));
            }
            else
            {
                FileHelper.AppendLine(logFilePath, $"Comparison fail in {comparisonFiles} - {isSuccess}");
                errorMessage?.AppendLine($"Comparison fail in {comparisonFiles} - {isSuccess}");
            }
        }
    }
}