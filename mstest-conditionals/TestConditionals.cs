using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace mstest_conditionals
{
    [TestClass]
    public class TestConditionals
    {

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int MessageBox(IntPtr hWnd, String text, String caption, long type);

        private const string ReachableServer = "http://ivsoftware.net";
        private const string UnreachableServer = "http://unreachable.bad";
        static List<string> _reachableServers = new List<string>();
        static List<string> _unreachableServers = new List<string>();
        private static readonly HttpClient httpClient = new HttpClient();

        [ClassInitialize]
        public static async Task ClassInit(TestContext context)
        {
            foreach (var url in new[] { ReachableServer, UnreachableServer })
            {
                if (await localIsServerReachableAsync(url))
                {
                    _reachableServers.Add(url);
                }
                else
                {
                    _unreachableServers.Add(url);
                }
            }
            if (_unreachableServers.Any())
            {
                // List reflect attribute to find test methods that rely on
                // requirement, then pop up an async message box listing them
                var skippedTestMethods =
                    typeof(TestConditionals)
                    .GetMethods(BindingFlags.Instance | BindingFlags.Public)
                    .Where(_ =>
                        _.GetCustomAttribute<RuntimeRequirementAttribute>() is RuntimeRequirementAttribute attr &&
                        _unreachableServers.Contains(attr.Requirement))
                    .ToArray();
                MessageBox(
                    IntPtr.Zero, 
                    string.Join(Environment.NewLine, skippedTestMethods.Select(_=>_.Name)), "Skipped Tests", 0);
            }

            #region L o c a l M e t h o d s
            async Task<bool> localIsServerReachableAsync(string url)
            {
                try
                {
                    // Make a HEAD request using HttpClient
                    using (var request = new HttpRequestMessage(HttpMethod.Head, url))
                    using (var response = await httpClient.SendAsync(request))
                    {
                        return response.IsSuccessStatusCode;
                    }
                }
                catch (HttpRequestException)
                {
                    return false;
                }
            }
            #endregion L o c a l M e t h o d s
        }
        [TestMethod, RuntimeRequirement(ReachableServer)]
        public async Task TestSomethingA()
        {
            // For now, exercise class initialize by await forever here
            await new SemaphoreSlim(0, 1).WaitAsync();
        }
        [TestMethod, RuntimeRequirement(UnreachableServer)]
        public async Task TestSomethingB()
        {

        }
#if INCLUDE_EXTENSIONS
        [TestMethod("Extension.CamelCaseToSpaces")]
        public void Test_CamelCaseToSpaces()
        {
            foreach (var state in Enum.GetValues<StartupState>())
            {
                switch (state)
                {
                    case StartupState.InitializingComponents: 
                        localEqualityCompare("Initializing Components"); break;
                    case StartupState.LoadingConfiguration:
                        localEqualityCompare("Loading Configuration"); break;
                    case StartupState.CheckingForUpdates: 
                        localEqualityCompare("Checking For Updates"); break;
                    case StartupState.ContactingServer:
                        localEqualityCompare("Contacting Server"); break;
                    case StartupState.FinalizingSetup: localEqualityCompare("Finalizing Setup"); break;
                    default: Assert.Fail($"Unexpected state: {state}"); break;
                }
                void localEqualityCompare(string expected)
                {
                    Assert.AreEqual(
                        expected: expected,
                        actual: $"{state}".CamelCaseToSpaces(),
                        message: "Expecting a single space where pattern is lower case character to upper case character.");
#if VERBOSE
                    Console.WriteLine($"{state}->{state.ToString().CamelCaseToSpaces()}");
#endif
                }
            }
        }
#endif
    }

    class RuntimeRequirementAttribute : Attribute
    {
        public RuntimeRequirementAttribute(string requirement)
        {
            Requirement = requirement;
        }
        public string Requirement { get; }
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