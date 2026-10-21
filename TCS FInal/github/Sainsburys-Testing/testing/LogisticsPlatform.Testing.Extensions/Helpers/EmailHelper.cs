using LogisticsPlatform.Testing.Extensions.Models;
using System.Net.Mail;
using System.Text;

namespace LogisticsPlatform.Testing.Extensions.Helpers;

public class EmailHelper
{
    public static void SendEmail(
        List<string> recipients,
        string subject,
        string htmlReportFilePath,
        List<ScenarioResult> scenarioResults)
    {
        try
        {
            MailMessage mailMessage = new MailMessage
            {
                From = new MailAddress("Logistics.Platform@sainsburys.co.uk"),
                Subject = subject,
                Body = BuildMailMessageBody(scenarioResults),
                IsBodyHtml = true
            };
            foreach (string recipient in recipients)
            {
                mailMessage.To.Add(recipient);
            }

            string[] allFiles = FileHelper.GetAllFiles(htmlReportFilePath);
            foreach (string fileName in allFiles)
            {
                mailMessage.Attachments.Add(new Attachment(fileName));
            }

            using SmtpClient smtpClient = new SmtpClient("smtp.bc.jsplc.net", 25);
            smtpClient.Send(mailMessage);
        }
        catch (Exception)
        {
            throw;
        }
    }

    private static string BuildMailMessageBody(List<ScenarioResult> scenarioResults)
    {
        int totalTests = scenarioResults.Count;
        int totalPassed = scenarioResults.Count(r => r.Passed);
        int totalFailed = totalTests - totalPassed;
        double passPercentage = (totalTests > 0) ? (totalPassed * 100.0 / totalTests) : 0.0;
        double failPercentage = (totalTests > 0) ? (totalFailed * 100.0 / totalTests) : 0.0;

        StringBuilder stringBuilder = new StringBuilder();
        stringBuilder.AppendLine(@"
            <table border='1' cellpadding='6' cellspacing='0' style='border-collapse:collapse; font-family:Arial,sans-serif; font-size:14px;'>
                <tr>
                    <th colspan='5' style='text-align:center; font-size:16px; font-weight:bold;'>Overall Test Summary</th>
                </tr>
                <tr>
                    <th style='font-weight:bold;'>Total<br/>Tests</th>
                    <th style='font-weight:bold;'>Passed</th>
                    <th style='font-weight:bold;'>Failed</th>
                    <th style='font-weight:bold;'>% Passed</th>
                    <th style='font-weight:bold;'>% Failed</th>
                </tr>
                <tr>
                    <td style='text-align:center; font-weight:bold;'>" + totalTests + @"</td>
                    <td style='text-align:center; color:green; font-weight:bold;'>" + totalPassed + @"</td>
                    <td style='text-align:center; color:red; font-weight:bold;'>" + totalFailed + @"</td>
                    <td style='text-align:center;'>" + passPercentage.ToString("F2") + @"%</td>
                    <td style='text-align:center;'>" + failPercentage.ToString("F2") + @"%</td>
                </tr>
            </table>
            <br/>
        ");
        stringBuilder.AppendLine("<p>All test results are summarized in the above table. Please review the attached detailed HTML report for scenario-level details.</p>");
        stringBuilder.AppendLine("<p>If you have any questions, please contact the Logistics Service Platform Testing Team.</p>");
        stringBuilder.Append("<br/><br/>");
        stringBuilder.Append("Regards,");
        stringBuilder.Append("<br/>");
        stringBuilder.Append("Logistics Service Platform Team");
        stringBuilder.Append("<br/><br/>");
        stringBuilder.Append("<p style=\"color:#686868;\">Please do not reply to this e-mail. The mailbox is not monitored and you will not receive a response.</p>");
        return stringBuilder.ToString();
    }
}
