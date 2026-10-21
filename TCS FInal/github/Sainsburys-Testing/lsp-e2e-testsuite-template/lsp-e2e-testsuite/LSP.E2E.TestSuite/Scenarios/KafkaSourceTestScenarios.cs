namespace LSP.E2E.TestSuite.Scenarios;

public class KafkaSourcePositiveTestScenarios : TheoryData<string, string, string, string>
{
    public KafkaSourcePositiveTestScenarios()
    {
        //TODO: Update Scenario Name and description and Add more scenarios as needed
        Add("TC_001", "Scenario Description", "AdaptorName1,AdaptorName2", "Positive");
        Add("TC_002", "Scenario Description", "AdaptorName1", "Positive");
    }
}

public class KafkaSourceNegativeTestScenarios : TheoryData<string, string, string, string>
{
    public KafkaSourceNegativeTestScenarios()
    {
        //TODO: Update Scenario Name and description and Add more scenarios as needed
        Add("TC_001", "Scenario Description", "AdaptorName1,AdaptorName2", "Negative");
        Add("TC_002", "Scenario Description", "AdaptorName1", "Negative");
    }
}
