using System.Text.RegularExpressions;

namespace mstest_conditionals
{
    [TestClass]
    public class TestConditionals
    {
#if INCLUDE_EXTENSIONS
        [TestMethod("Extension.CamelCaseToSpaces")]
        public void Test_CamelCaseToSpaces()
        {
            foreach (var state in Enum.GetValues<StartupState>())
            {
                switch (state)
                {
                    case StartupState.InitializingComponents: localEqualityCompare("Initializing Components"); break;
                    case StartupState.LoadingConfiguration: localEqualityCompare("Loading Configurations"); break;
                    case StartupState.CheckingForUpdates: localEqualityCompare("Checking For Updates"); break;
                    case StartupState.ContactingServer: localEqualityCompare("Contacting Server"); break;
                    case StartupState.FinalizingSetup: localEqualityCompare("Finalizing Setup"); break;
                    default: Assert.Fail($"Unexpected state: {state}"); break;
                }
                void localEqualityCompare(string expected)
                {
                    Assert.AreEqual(
                        expected: expected,
                        actual: $"{state}".CamelCaseToSpaces(),
                        message: "Expecting a single space where pattern is lower case character to upper case character.");
                }
            }
        }
#endif
    }
    static partial class Extensions
    {

        public static string CamelCaseToSpaces(this string @string)
        {
            string pattern = "(?<![A-Z])([A-Z][a-z]|(?<=[a-z])[A-Z])";
            string replacement = " $1";
            return Regex.Replace(@string, pattern, replacement).Trim();
        }
    }
    public enum StartupState
    {
        InitializingComponents,
        LoadingConfiguration,
        CheckingForUpdates,
        ContactingServer,
        FinalizingSetup
    }
}