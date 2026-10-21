using LogisticsPlatform.Testing.Extensions.Models;
using System.Text;

namespace LogisticsPlatform.Testing.Extensions.Helpers;

public class HtmlReportHelper
{
    public static void GenerateTestResultsHtmlReport(
        DateTime testsExecutionStartTime,
        DateTime totalTestsExecutionEndTime,
        string serviceName,
        List<ScenarioResult> scenarioResults,
        string reportPath,
        Dictionary<string, bool> config = null)
    {
        var html = new StringBuilder();

        var minStart = testsExecutionStartTime;
        var maxEnd = totalTestsExecutionEndTime;
        var actualStartTime = minStart.ToString("yyyy-MM-dd HH:mm:ss");
        var actualTotalTime = (maxEnd - minStart);
        var actualTotalTimeStr = $"{(int)actualTotalTime.TotalMinutes} Min {actualTotalTime.Seconds} Seconds";

        var totalTests = scenarioResults.Count;
        var totalPassed = scenarioResults.Count(r => r.Passed);
        var totalFailed = totalTests - totalPassed;
        var percentPassed = totalTests > 0 ? (totalPassed * 100.0 / totalTests) : 0;
        var percentFailed = totalTests > 0 ? (totalFailed * 100.0 / totalTests) : 0;

        html.AppendLine("<!DOCTYPE html>");
        html.AppendLine("<html lang='en'><head>");
        html.AppendLine("<meta charset='UTF-8'><title>Test Results Summary</title>");
        html.AppendLine(@"
<style>
    body { font-family: Arial, sans-serif; margin: 40px; background: #f9f9f9; }
    h1 { color: #222; font-size: 2.2em; margin-bottom: 0.2em; }
    h3 { color: #555; font-size: 1.1em; margin-top: 0; margin-bottom: 1.5em; }

    .summary-table {
        margin-bottom: 30px;
        width: 220px;
        border-collapse: collapse;
        font-size: 0.98em;
        margin-left: 0;
        box-shadow: 0 2px 8px #e0e0e0;
    }
    .summary-table th, .summary-table td {
        border: 1px solid #ddd;
        padding: 6px 10px;
        text-align: center;
    }
    .summary-table th { font-size: 1em; }

    table { border-collapse: collapse; width: 100%; background: #fff; }
    th, td { border: 1px solid #ddd; padding: 8px; }
    th { background: #4CAF50; color: white; }
    tr.failed { background-color: #ffe6e6; }
    tr.passed { background-color: #e6ffe6; }
    tr:hover { background-color: #f1f1f1; }

    .chart-container { width: 300px; margin: 30px auto; }
    details { margin-bottom: 20px; }
    summary {
        font-size: 1.1em;
        font-weight: bold;
        cursor: pointer;
        background: #43a047;
        color: #fff;
        padding: 8px 12px;
        border-radius: 4px;
    }
    summary:focus { outline: 2px solid #43a047; }
    .positive-summary {
        font-size: 1.1em;
        font-weight: bold;
        cursor: pointer;
        background: #a5d6a7;
        color: #222;
        padding: 8px 12px;
        border-radius: 4px;
        border: 1px solid #388e3c;
    }
    .positive-summary:focus { outline: 2px solid #388e3c; }
    .negative-summary {
        font-size: 1.1em;
        font-weight: bold;
        cursor: pointer;
        background: #ffcdd2;
        color: #222;
        padding: 8px 12px;
        border-radius: 4px;
        border: 1px solid #d32f2f;
    }
    .negative-summary:focus { outline: 2px solid #d32f2f; }

    /* --- Time summary boxes (no colored headers, full text visible) --- */
    .time-summary-table {
        margin: 16px 0 24px 0;
        width: 100%;
        border-collapse: collapse;
        font-size: 0.98em;
        box-shadow: 0 2px 8px #e0e0e0;
        table-layout: auto;
    }
    .time-summary-table th, .time-summary-table td {
        border: 1px solid #ddd;
        padding: 10px 12px;
        text-align: left;
        background: #fff;
        color: #111;
        white-space: normal;
        overflow: visible;
        text-overflow: clip;
        word-break: break-word;
    }
    .time-summary-table th {
        font-weight: 700;
    }
    .time-summary-table td.value {
        font-weight: 600;
    }
    .exception-link {
        color: #007bff;
        text-decoration: underline;
        cursor: pointer;
    }
    .exception-details {
        display: none;
        background: #f8d7da;
        color: #721c24;
        border: 1px solid #f5c6cb;
        padding: 8px;
        margin-top: 4px;
        white-space: pre-wrap;
        font-size: 0.95em;
    }
    .error-link {
        color: #d32f2f;
        text-decoration: underline;
        cursor: pointer;
        margin-left: 8px;
    }
    .error-details {
        display: none;
        background: #fff3cd;
        color: #856404;
        border: 1px solid #ffeeba;
        padding: 8px;
        margin-top: 4px;
        white-space: pre-wrap;
        font-size: 0.95em;
    }
</style>");
        html.AppendLine(@"<script src=""https://cdn.jsdelivr.net/npm/chart.js""></script>");
        html.AppendLine(@"<script src=""https://cdn.jsdelivr.net/npm/chartjs-plugin-datalabels@2""></script>");
        html.AppendLine(@"<script>
function toggleException(id) {
    var el = document.getElementById(id);
    if (el.style.display === 'none' || el.style.display === '') {
        el.style.display = 'block';
    } else {
        el.style.display = 'none';
    }
}
function toggleError(id) {
    var el = document.getElementById(id);
    if (el.style.display === 'none' || el.style.display === '') {
        el.style.display = 'block';
    } else {
        el.style.display = 'none';
    }
}
function toggleDescription(id) {
    var el = document.getElementById(id);
    if (el.style.display === 'none' || el.style.display === '') {
        el.style.display = 'block';
    } else {
        el.style.display = 'none';
    }
}
</script>");
        html.AppendLine("</head><body>");

        html.AppendLine($@"
<h1 style='text-align:center; font-size:2.4em; margin-bottom:0.2em; color:#111; font-weight:bold;'>
    Service Name : {System.Net.WebUtility.HtmlEncode(serviceName)}
</h1>
");
        html.AppendLine("<h1>Test Results Report</h1>");

        // Start a flex container for summary and skipped tables
        html.AppendLine(@"
<div style='display: flex; flex-wrap: wrap; gap: 40px; align-items: flex-start; margin-bottom: 30px;'>
");

        // Overall Test Summary table 
        html.AppendLine($@"
<div>
<h2>Overall Test Summary</h2>
<table class='summary-table'>
    <tr>
        <th>Total Tests</th>
        <th>Passed</th>
        <th>Failed</th>
        <th>% Passed</th>
        <th>% Failed</th>
    </tr>
    <tr>
        <td>{totalTests}</td>
        <td style='color:green;font-weight:bold;'>{totalPassed}</td>
        <td style='color:red;font-weight:bold;'>{totalFailed}</td>
        <td>{percentPassed:F1}%</td>
        <td>{percentFailed:F1}%</td>
    </tr>
</table>
</div>
");

        // Skipped Tests table 
        if (config != null)
        {
            var skippedComponents = config
                .Where(kvp => !kvp.Value)
                .Select(kvp => kvp.Key)
                .ToList();

            if (skippedComponents.Any())
            {
                html.AppendLine(@"
<div>
<h2>Tests Execution Skipped</h2>
<table class='summary-table'>
    <tr><th>Components</th></tr>");
                foreach (var component in skippedComponents)
                {
                    html.AppendLine($"<tr><td>{System.Net.WebUtility.HtmlEncode(component)}</td></tr>");
                }
                html.AppendLine(@"</table>
</div>");
            }
        }

        // End flex container
        html.AppendLine("</div>");

        html.AppendLine($@"
<h2>Time Summary</h2>
<table class='time-summary-table'>
    <tr>
    <th>Tests Execution Start Time</th>
    <th>Total Run Time</th>
    </tr>
    <tr>
    <td class='value'>{System.Net.WebUtility.HtmlEncode(actualStartTime)}</td>
    <td class='value'>{System.Net.WebUtility.HtmlEncode(actualTotalTimeStr)}</td>
    </tr>
</table>
");

        // Group by component
        var components = scenarioResults
            .GroupBy(r => r.ComponentName)
            .OrderBy(g => g.Key);

        int chartIndex = 0;
        foreach (var componentGroup in components)
        {
            var component = componentGroup.FirstOrDefault()?.Component ?? "N/A";
            var componentName = componentGroup.Key ?? "N/A";
            var total = componentGroup.Count();
            var failed = componentGroup.Count(r => !r.Passed);
            var passed = total - failed;

            var positiveScenarios = componentGroup
                .Where(r => r.ScenarioType != null && r.ScenarioType.Equals("Positive", StringComparison.OrdinalIgnoreCase))
                .ToList();
            var negativeScenarios = componentGroup
                .Where(r => r.ScenarioType != null && r.ScenarioType.Equals("Negative", StringComparison.OrdinalIgnoreCase))
                .ToList();

            int positiveTotal = positiveScenarios.Count;
            int positivePassed = positiveScenarios.Count(r => r.Passed);
            int positiveFailed = positiveTotal - positivePassed;
            int negativeTotal = negativeScenarios.Count;
            int negativePassed = negativeScenarios.Count(r => r.Passed);
            int negativeFailed = negativeTotal - negativePassed;

            html.AppendLine($@"
<details>
    <summary style='background:#e0e0e0;color:#222;font-size:1.2em;font-weight:bold;padding:10px 14px;border-radius:4px;border:1px solid #bdbdbd;'>
    {component}: {System.Net.WebUtility.HtmlEncode(componentName)} &mdash; Total: {total}, Passed: {passed}, Failed: {failed}
    </summary>
    <div style='margin:16px 0;'>
    <table class='summary-table'>
        <tr>
            <th style='background:#1976d2;color:#fff;'>Total Tests</th>
            <th style='background:#388e3c;color:#fff;'>Passed</th>
            <th style='background:#d32f2f;color:#fff;'>Failed</th>
        </tr>
        <tr>
            <td style='text-align:center;font-weight:bold;'>{total}</td>
            <td style='text-align:center;font-weight:bold;'>{passed}</td>
            <td style='text-align:center;font-weight:bold;'>{failed}</td>
        </tr>
    </table>

    <div class='chart-container'><canvas id='resultChart_{chartIndex}'></canvas></div>
    <script>
        (function() {{
            const ctx = document.getElementById('resultChart_{chartIndex}').getContext('2d');
            new Chart(ctx, {{
                type: 'pie',
                data: {{
                    labels: ['Passed', 'Failed'],
                    datasets: [{{
                        data: [{passed}, {failed}],
                        backgroundColor: ['#4CAF50', '#F44336'],
                    }}]
                }},
                options: {{
                    responsive: true,
                    plugins: {{
                        legend: {{
                            position: 'bottom'
                        }},
                        title: {{
                            display: true,
                            text: 'Test Results'
                        }},
                        datalabels: {{
                            color: '#fff',
                            font: {{
                                weight: 'bold',
                                size: 16
                            }},
                            formatter: (value, context) => {{
                                const data = context.chart.data.datasets[0].data;
                                const total = data.reduce((a, b) => a + b, 0);
                                if (total === 0) return '';

                                const percent = (value / total * 100);
                                const label = context.chart.data.labels[context.dataIndex];
                                const display = percent.toFixed(1);

                                if (value === total) {{
                                    return label === 'Passed' ? '100% Passed' : '100% Failed';
                                }}
                                if (value === 0) {{
                                    return '';
                                }}
                                return `${{display}}% ${{label}}`;
                            }}
                        }}
                    }}
                }},
                plugins: [ChartDataLabels]
            }});
        }})();
    </script>
    <details>
        <summary class='positive-summary'>
        Positive Scenarios &mdash; Total: {positiveTotal}, Passed: {positivePassed}, Failed: {positiveFailed}
        </summary>
        <table>
        <tr>
            <th>Scenarios</th>
            <th>Description</th>
            <th>Adaptors Covered</th>
            <th>Overall Status</th>
            <th>Passed Components</th>
            <th>Failed Components</th>
            <th>Failure Reason</th>
            <th>Exception</th>
        </tr>");
            foreach (var r in positiveScenarios)
            {
                var status = r.Passed ? "Passed" : "Failed";
                var error = r.ErrorMessage !=null ? r.ErrorMessage.ToString() : "";
                var descriptionId = $"desc_{Guid.NewGuid()}";
                var passedComponents = r.PassedComponents != null && r.PassedComponents.Any()
                    ? string.Join(", ", r.PassedComponents)
                    : "";
                var failedComponents = r.FailedComponents != null && r.FailedComponents.Any()
                    ? string.Join(", ", r.FailedComponents)
                    : "";

                var rowClass = r.Passed ? "passed" : "failed";
                var exceptionId = $"exception_{Guid.NewGuid()}";
                var errorId = $"error_{Guid.NewGuid()}";
                var exceptionText = System.Net.WebUtility.HtmlEncode(r.ExceptionMessage);
                var errorText = System.Net.WebUtility.HtmlEncode(error);

                string exceptionCell = "";
                if (!string.IsNullOrEmpty(r.ExceptionMessage))
                {
                    exceptionCell = $@"
<a class='exception-link' onclick=""toggleException('{exceptionId}')"">View Exception</a>
<div id='{exceptionId}' class='exception-details'>{exceptionText}</div>";
                }

                string errorCell = "";
                if (!string.IsNullOrEmpty(error))
                {
                    errorCell = $@"
<a class='error-link' onclick=""toggleError('{errorId}')"">View Details</a>
<div id='{errorId}' class='error-details'>{errorText}</div>";
                }

                html.AppendLine(
                    $"<tr class='{rowClass}'>" +
                    $"<td>{System.Net.WebUtility.HtmlEncode(r.ScenarioName)}</td>" +
                    // Description as clickable link
                    $"<td><a href='javascript:void(0);' onclick=\"toggleDescription('{descriptionId}')\">View Description</a>" +
                    $"<div id='{descriptionId}' style='display:none; margin-top:4px; background:#f5f5f5; padding:6px; border-radius:4px; border:1px solid #ccc;'>{System.Net.WebUtility.HtmlEncode(r.ScenarioDescription)}</div></td>" +
                    $"<td>{System.Net.WebUtility.HtmlEncode(r.AdaptorsCovered)}</td>" +
                    $"<td>{status}</td>" +
                    $"<td>{System.Net.WebUtility.HtmlEncode(passedComponents)}</td>" +
                    $"<td>{System.Net.WebUtility.HtmlEncode(failedComponents)}</td>" +
                    $"<td>{errorCell}</td>" +
                    $"<td>{exceptionCell}</td></tr>");
            }
            html.AppendLine("</table></details>");

            if (negativeTotal > 0)
            {
                html.AppendLine($@"
    <details>
        <summary class='negative-summary'>
        Negative Scenarios &mdash; Total: {negativeTotal}, Passed: {negativePassed}, Failed: {negativeFailed}
        </summary>
        <table>
        <tr>
            <th>Scenarios</th>
            <th>Description</th>
            <th>Adaptors Covered</th>
            <th>TestResult</th>
            <th>Passed Components</th>
            <th>Failed Components</th>
            <th>Failure Reason</th>
            <th>Exception</th>
        </tr>");
                foreach (var r in negativeScenarios)
                {
                    var status = r.Passed ? "Passed" : "Failed";
                    var error = r.ErrorMessage != null ? r.ErrorMessage.ToString() : "";
                    var descriptionId = $"desc_{Guid.NewGuid()}";
                    var passedComponents = r.PassedComponents != null && r.PassedComponents.Any()
                        ? string.Join(", ", r.PassedComponents)
                        : "";
                    var failedComponents = r.FailedComponents != null && r.FailedComponents.Any()
                        ? string.Join(", ", r.FailedComponents)
                        : "";

                    var rowClass = r.Passed ? "passed" : "failed";
                    var exceptionId = $"exception_{Guid.NewGuid()}";
                    var errorId = $"error_{Guid.NewGuid()}";
                    var exceptionText = System.Net.WebUtility.HtmlEncode(r.ExceptionMessage);
                    var errorText = System.Net.WebUtility.HtmlEncode(error);

                    string exceptionCell = "";
                    if (!string.IsNullOrEmpty(r.ExceptionMessage))
                    {
                        exceptionCell = $@"
<a class='exception-link' onclick=""toggleException('{exceptionId}')"">View Exception</a>
<div id='{exceptionId}' class='exception-details'>{exceptionText}</div>";
                    }

                    string errorCell = "";
                    if (!string.IsNullOrEmpty(error))
                    {
                        errorCell = $@"
<a class='error-link' onclick=""toggleError('{errorId}')"">View Details</a>
<div id='{errorId}' class='error-details'>{errorText}</div>";
                    }
                    html.AppendLine(
                        $"<tr class='{rowClass}'>" +
                        $"<td>{System.Net.WebUtility.HtmlEncode(r.ScenarioName)}</td>" +
                        // Description as clickable link
                        $"<td><a href='javascript:void(0);' onclick=\"toggleDescription('{descriptionId}')\">View Description</a>" +
                        $"<div id='{descriptionId}' style='display:none; margin-top:4px; background:#f5f5f5; padding:6px; border-radius:4px; border:1px solid #ccc;'>{System.Net.WebUtility.HtmlEncode(r.ScenarioDescription)}</div></td>" +
                        $"<td>{System.Net.WebUtility.HtmlEncode(r.AdaptorsCovered)}</td>" +
                        $"<td>{status}</td>" +
                        $"<td>{System.Net.WebUtility.HtmlEncode(passedComponents)}</td>" +
                        $"<td>{System.Net.WebUtility.HtmlEncode(failedComponents)}</td>" + 
                        $"<td>{errorCell}</td>" +
                        $"<td>{exceptionCell}</td></tr>");
                }
                html.AppendLine("</table></details>");
            }
            html.AppendLine("</div></details>");
            chartIndex++;
        }

        html.AppendLine("</body></html>");

        FileHelper.WriteLine(reportPath + $"HtmlTestReport_{DateTime.Now.Date:yyyy-MM-dd}.html", html.ToString());
    }
}