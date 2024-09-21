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
        private const string Server1 = "http://ivsoftware.net";     // Reachable
        private const string Server2 = "http://unreachable.bad";    // Unreachable
        private static MethodInfo[] _skippedTestMethods = new MethodInfo[0];
        private static readonly HttpClient httpClient = new HttpClient();
        private static Thread? messageBoxThread = null;
        [ClassInitialize]
        public static async Task ClassInit(TestContext context)
        {
            List<string> _reachableServers = new List<string>();
            List<string> _unreachableServers = new List<string>();
            foreach (var url in new[] { Server1, Server2 })
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
                // Use reflection to find test methods with a RuntimeRequirement attribute that
                // don't meet the requirement, then display them in a non-blocking message box.
                _skippedTestMethods =
                    typeof(TestConditionals)
                    .GetMethods(BindingFlags.Instance | BindingFlags.Public)
                    .Where(_ =>
                        _.GetCustomAttribute<RuntimeRequirementAttribute>() is RuntimeRequirementAttribute attr &&
                        _unreachableServers.Contains(attr.Requirement))
                    .ToArray();

                // List skipped tests in a popup, but don't actually halt the execution.
                // We'll await the MB in the [ClassCleanup]
                messageBoxThread = new Thread(() =>
                    MessageBox(
                    IntPtr.Zero,
                    string.Join(Environment.NewLine, _skippedTestMethods.Select(_ => _.Name)),
                    "Skipped Tests", 0));
                messageBoxThread.SetApartmentState(ApartmentState.STA);
                messageBoxThread.Start();
            }

            #region L o c a l M e t h o d s
            async Task<bool> localIsServerReachableAsync(string url)
            {
                try
                {
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
        public TestContext? TestContext { get; set; }
        [TestInitialize]
        public void TestInitialize()
        {
            if(_skippedTestMethods.Select(_=>_.Name).Contains(TestContext?.TestName))
            {
                // Test won't run at all.
                // In RUN mode, will show as a skipped test.
                // In DEBUG mode:
                // - WILL: Break on Thrown
                // - UNLESS: AssertInconclusiveExecption is disabled in Exception Settings window.
                Assert.Inconclusive($"Requirement not met for {TestContext?.TestName}");
            }
        }

        [TestMethod, RuntimeRequirement(Server1)]
        public void TestSomethingA()
        {
        }
        [TestMethod, RuntimeRequirement(Server2)]
        public void TestSomethingB()
        {
        }
        [TestMethod]
        public void TestSomethingC()
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

        [ClassCleanup]
        public void ClassCleanup()
        {
            if (messageBoxThread != null && messageBoxThread.IsAlive)
            {
                // Wait for the message box thread to complete
                messageBoxThread.Join();
            }
        }

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