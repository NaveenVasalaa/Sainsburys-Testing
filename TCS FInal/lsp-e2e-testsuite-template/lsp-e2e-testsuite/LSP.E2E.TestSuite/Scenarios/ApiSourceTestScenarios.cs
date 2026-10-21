namespace LSP.E2E.TestSuite.Scenarios;

public class ApiSourcePositiveTestScenarios : TheoryData<string, string, string, string>
{
    public ApiSourcePositiveTestScenarios()
    {
        //TODO: Update Scenario Name and description and Add more scenarios as needed
        Add("TC_001", "Scenario Description","AdaptorName1,AdaptorName2","Positive");
        Add("TC_002", "Scenario Description", "AdaptorName1", "Positive");
    }
}

public class ApiSourceNegativeTestScenarios : TheoryData<string, string, string, string>
{
    public ApiSourceNegativeTestScenarios()
    {
        //TODO: Update Scenario Name and description and Add more scenarios as needed
        Add("TC_001", "Scenario Description", "NA", "Negative");
        Add("TC_002", "Scenario Description", "NA", "Negative");
    }
}
