using LogisticsPlatform.Testing.Extensions.Helpers;
using System.Text;
using Xunit;

namespace LogisticsPlatform.Testing.Extensions.Extensions
{
    public class ResultExtensions
    {
        private static double PassedCount { get; set; }
        private static double FailedCount { get; set; }
        private static double TotalCount { get; set; }

        public static void StoreResult(string testStatus, string logFilePath)
        {
            if (testStatus == "Failed")
            {
                FailedCount++;
                var exceptionMessages = TestContext.Current.TestState.ExceptionMessages;
                foreach (var errorMessage in exceptionMessages)
                {
                    FileHelper.AppendLine(logFilePath, "Issue - " + errorMessage);
                }
                FileHelper.AppendLine(logFilePath, "Status - Fail");
                FileHelper.AppendLine(logFilePath, new string('=', 100));
            }
            else
            {
                PassedCount++;
                FileHelper.AppendLine(logFilePath, "Status - Pass");
                FileHelper.AppendLine(logFilePath, new string('=', 100));
            }
        }

        public static void StoreResult(string testStatus, string logFilePath, string exceptionMessage)
        {
            if (testStatus == "Failed")
            {
                FailedCount++;                
                FileHelper.AppendLine(logFilePath, "Issue - " + exceptionMessage);
                FileHelper.AppendLine(logFilePath, "Status - Fail");
                FileHelper.AppendLine(logFilePath, new string('=', 100));
            }
            else
            {
                PassedCount++;
                FileHelper.AppendLine(logFilePath, "Status - Pass");
                FileHelper.AppendLine(logFilePath, new string('=', 100));
            }
        }        

        public static void UpdateFinalResult(string logFilePath)
        {
            TotalCount = PassedCount + FailedCount;

            var sb = new StringBuilder();

            sb.AppendLine("Total Number of scenarios executed - " + TotalCount);
            sb.AppendLine("Number of scenarios Passed - " + PassedCount);
            sb.AppendLine("Number of scenarios Failed - " + FailedCount);
            sb.AppendLine("Pass percentage - " + Math.Round(PassedCount / TotalCount * 100, 2) + "%");
            sb.Append("Executed Date Time: " + DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"));

            FileHelper.AppendLine(logFilePath, new string('=', 120));
            FileHelper.AppendLine(logFilePath, sb.ToString());
            FileHelper.AppendLine(logFilePath, new string('=', 120));
        }
    }
}
