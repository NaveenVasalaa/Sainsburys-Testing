using System.Text;
namespace LogisticsPlatform.Testing.Extensions.Models;

public static class TestResultLogs
{
    public static List<ScenarioResult> ScenarioResults { get; } = new();
}

public class ScenarioResult
{
    public string Component { get; set; }
    public string ComponentName { get; set; }
    public string ScenarioName { get; set; }
    public string ScenarioType { get; set; }
    public string ScenarioDescription { get; set; }
    public string AdaptorsCovered { get; set; }
    public bool Passed { get; set; }
    public string ExceptionMessage { get; set; }
    public StringBuilder ErrorMessage { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public List<string> PassedComponents { get; set; } = new();
    public List<string> FailedComponents { get; set; } = new();
}




