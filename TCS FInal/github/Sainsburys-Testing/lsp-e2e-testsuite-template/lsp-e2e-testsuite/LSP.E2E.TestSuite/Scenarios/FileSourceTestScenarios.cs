namespace LSP.E2E.TestSuite.Scenarios;

public class FileSourcePositiveTestScenarios : TheoryData<string, string, string, string>
{
    public FileSourcePositiveTestScenarios()
    {
        //TODO: Update Scenario Name and description and Add more scenarios as needed
        Add("TC_001", "Scenario Description", "AdaptorName1,AdaptorName2", "Positive");
        Add("TC_002", "Scenario Description", "AdaptorName1", "Positive");
    }
}
public class FileSourceNegativeTestScenarios : TheoryData<string, string, string, string>
{
    public FileSourceNegativeTestScenarios()
    {
        //TODO: Update Scenario Name and description and Add more scenarios as needed
        Add("TC_001", "Scenario Description", "NA", "Negative");
        Add("TC_002", "Scenario Description", "NA", "Negative");
    }
}
