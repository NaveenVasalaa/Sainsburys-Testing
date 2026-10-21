using System.Text.Json;

namespace LogisticsPlatform.Testing.Extensions.Helpers;

public static class FileHelper
{
    public static bool DirectoryExists(string directoryPath)
    {
        return Directory.Exists(directoryPath);
    }

    public static bool FileExists(string filePath)
    {
        return File.Exists(filePath);
    }

    public static string[] GetAllFiles(string inputDirectoryPath, string inputFileNamePattern = "*")
    {
        return Directory.GetFiles(inputDirectoryPath.TrimEnd('/'), inputFileNamePattern);
    }

    public static string GetAllText(string filePath)
    {
        return File.ReadAllText(filePath.TrimEnd('/'));
    }

    public static string[] GetAllLines(string filePath)
    {
        return File.ReadAllLines(filePath.TrimEnd('/'));
    }

    public static string GetFirstLine(string filePath)
    {
        return File.ReadAllLines(filePath)[0];
    }

    public static void WriteLine(string filePath, string content)
    {
        File.WriteAllText(filePath, content + Environment.NewLine);
    }

    public static void WriteAllLines(string filePath, List<string> contents)
    {
        File.WriteAllLines(filePath, contents);
    }

    public static void AppendLine(string filePath, string content)
    {
        File.AppendAllText(filePath, content + Environment.NewLine);
    }

    public static void AppendAllLines(string filePath, List<string> contents)
    {
        File.AppendAllLines(filePath, contents);
    }

    public static void RenameFile(string sourceFileName, string destinationFileName)
    {
        File.Move(sourceFileName, destinationFileName);
    }

    public static void CombineMultipleFilesIntoSingleFile(string inputDirectoryPath, string inputFileNamePattern, string outputFilePath)
    {
        var safeInputPath = ValidateAndGetSafePath(inputDirectoryPath);
        var safeOutputPath = ValidateAndGetSafePath(outputFilePath);

        if (!Directory.Exists(safeInputPath))
        {
            throw new DirectoryNotFoundException($"Input directory does not exist: {safeInputPath}");
        }

        if (safeOutputPath.StartsWith(safeInputPath, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Output file cannot be inside the input directory.");
        }

        var inputFilePaths = Directory.GetFiles(safeInputPath, inputFileNamePattern);

        Console.WriteLine("Processing Files in {0}.", safeInputPath);
        Console.WriteLine("Number of files matching {0}: {1}.", inputFileNamePattern, inputFilePaths.Length);

        using var outputStream = File.Create(safeOutputPath);
        using var writer = new StreamWriter(outputStream);

        foreach (var filePath in inputFilePaths)
        {
            var safeFilePath = ValidateAndGetSafePath(filePath, safeInputPath);
            var fileContent = File.ReadAllText(safeFilePath);

            writer.WriteLine(fileContent);

            Console.WriteLine("The file {0} has been processed.", safeFilePath);
        }
    }

    private static string ValidateAndGetSafePath(string path, string allowedParentDirectory = null)
    {
        if (allowedParentDirectory != null && !path.StartsWith(allowedParentDirectory, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Path {path} is not allowed. It must be within {allowedParentDirectory}.");
        }

        if (path.Contains(".."))
        {
            throw new ArgumentException($"Path {path} contains invalid characters.");
        }

        return Path.GetFullPath(path.TrimEnd('/', '\\'));
    }

    public static void CopyAllFiles(string sourceDirectoryPath, string destinationDirectoryPath, string fileNamePattern = "*")
    {
        string[] sourceFilePaths = Directory.GetFiles(sourceDirectoryPath.TrimEnd('/'), fileNamePattern);

        Console.WriteLine("Copying Files from {0}.", sourceDirectoryPath);
        Console.WriteLine("Number of files matching {0}: {1}.", sourceDirectoryPath, sourceFilePaths.Length);

        Directory.CreateDirectory(destinationDirectoryPath);

        foreach (var sourceFilePath in sourceFilePaths)
        {
            var destinationFilePath = Path.Combine(destinationDirectoryPath.TrimEnd('/'), Path.GetFileName(sourceFilePath));

            File.Copy(sourceFilePath, destinationFilePath);
            Console.WriteLine("The file {0} has been copied to {1}.", sourceFilePath, destinationFilePath);
        }
    }

    public static void MoveDirectory(string sourceDirectory, string destinationDirectory)
    {
        // Ensure the destination directory exists
        Directory.CreateDirectory(destinationDirectory);

        // Get all subdirectories in the source directory
        string[] subdirectories = Directory.GetDirectories(sourceDirectory, "*", SearchOption.AllDirectories);

        foreach (string subdirectory in subdirectories)
        {
            // Create the corresponding subdirectory in the destination directory
            string relativePath = Path.GetRelativePath(sourceDirectory, subdirectory);
            string destinationSubdirectory = Path.Combine(destinationDirectory, relativePath);
            Directory.CreateDirectory(destinationSubdirectory);

            // Get all files in the current subdirectory
            string[] files = Directory.GetFiles(subdirectory);

            foreach (string file in files)
            {
                // Get the file name
                string fileName = Path.GetFileName(file);

                // Create the destination file path
                string destinationFile = Path.Combine(destinationSubdirectory, fileName);

                // Copy the file
                File.Copy(file, destinationFile, true); // Overwrite if the file already exists
            }
        }

        Console.WriteLine("Subfolders and files copied successfully.");
    }

    public static void DeleteAllFiles(string inputDirectoryPath, string inputFileNamePattern)
    {
        string[] inputFilePaths = Directory.GetFiles(inputDirectoryPath.TrimEnd('/'), inputFileNamePattern);

        Console.WriteLine("Deleting Files in {0}.", inputDirectoryPath);
        Console.WriteLine("Number of files matching {0}: {1}.", inputFileNamePattern, inputFilePaths.Length);

        foreach (var inputFilePath in inputFilePaths)
        {
            File.Delete(inputFilePath);
            Console.WriteLine("The file {0} has been deleted.", inputFilePath);
        }
    }

    public static void SaveAsJson<T>(IEnumerable<T> documents, string filePath, string filePrefix)
    {
        int counter = 1;
        string filePathWithPrefix = Path.Combine(filePath, filePrefix);
        foreach (var document in documents)
        {
            var strResultJSON = JsonSerializer.Serialize(document);
            WriteLine($"{filePathWithPrefix}_{counter}.json", strResultJSON);
            counter++;
        }
    }

    public static Dictionary<string, string> CreateAllDirectories(string basePath, Dictionary<string, string> relativePaths, string scenario, string logFilePath)
    {
        Dictionary<string, string> createdDirectories = [];
        foreach (var entry in relativePaths)
        {
            string name = entry.Key;
            string relativePath = entry.Value;

            // Append the additional string to the relative path
            string modifiedRelativePath = Path.Combine(relativePath, scenario);

            // Combine the base path and relative path
            string fullPath = Path.Combine(basePath, modifiedRelativePath);
            // Check if the directory already exists
            if (!Directory.Exists(fullPath))
            {
                // Create the directory
                Directory.CreateDirectory(fullPath);
                AppendLine(logFilePath, $"Directory created at: {fullPath}");
            }
            else
            {
                AppendLine(logFilePath, $"Directory already exists at: {fullPath}");
            }

            // Add the full path to the dictionary with the name as the key
            createdDirectories[name] = fullPath;
        }

        return createdDirectories;
    }

    public static int GetNextFileCount(string folderPath, string filePrefix)
    {
        var files = Directory.GetFiles(folderPath, $"{filePrefix}*.json");
        if (files.Length == 0)
        {
            return 0;
        }

        var maxCount = files
                      .Select(Path.GetFileNameWithoutExtension)
                      .Select(fileName => fileName[filePrefix.Length..])
                      .Select(countStr => int.TryParse(countStr, out var count) ? count : 0)
                      .Max();

        return maxCount;
    }

    public static string GetTestFilePath(string folderName, string fileName)
    {
        try
        {
            string startupPath = AppDomain.CurrentDomain.BaseDirectory;
            var pathItems = startupPath.Split(Path.DirectorySeparatorChar);
            var pos = pathItems.Reverse().ToList().FindIndex(x => string.Equals("bin", x));
            string projectPath = string.Join(Path.DirectorySeparatorChar.ToString(), pathItems.Take(pathItems.Length - pos - 1));
            string filepath = Path.Combine(projectPath, folderName, fileName);
            return filepath;
        }
        catch (Exception)
        {
            throw;
        }
    }
}