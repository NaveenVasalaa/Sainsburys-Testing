using Xunit;

namespace LogisticsPlatform.Testing.Extensions.Extensions
{
    public class TestContextExtensions
    {
        public static string GetScenarioName()
        {
            return (string)((Xunit.v3.XunitTest)TestContext.Current.Test).TestMethodArguments[0];
        }
    }
}
