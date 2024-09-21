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
        private static MethodInfo[] _skippedTestMethods = new MethodInfo[0];
        private static readonly HttpClient httpClient = new HttpClient();
        [ClassInitialize]
        public static async Task ClassInit(TestContext context)
        {
            List<string> _reachableServers = new List<string>();
            List<string> _unreachableServers = new List<string>();
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
                _skippedTestMethods =
                    typeof(TestConditionals)
                    .GetMethods(BindingFlags.Instance | BindingFlags.Public)
                    .Where(_ =>
                        _.GetCustomAttribute<RuntimeRequirementAttribute>() is RuntimeRequirementAttribute attr &&
                        _unreachableServers.Contains(attr.Requirement))
                    .ToArray();

                // List skipped tests in a popup, but don't actually halt the execution
                var staThread = new Thread(() =>
                    MessageBox(
                    IntPtr.Zero,
                    string.Join(Environment.NewLine, _skippedTestMethods.Select(_ => _.Name)),
                    "Skipped Tests", 0));
                staThread.SetApartmentState(ApartmentState.STA);
                staThread.Start();
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
#if ASSERT_IN_TEST_INITIALIZE
            if(_skippedTestMethods.Select(_=>_.Name).Contains(TestContext?.TestName))
            {
                // Test won't run at all.
                // In RUN mode, will show as a skipped test.
                // In DEBUG mode will: BREAK ON THROWN. So, if you don't want this, use RUN mode!
                Assert.Inconclusive($"Requirement not met for {TestContext?.TestName}");
            }
#endif
        }

        [TestMethod, RuntimeRequirement(ReachableServer)]
        public async Task TestSomethingA()
        {
#if !ASSERT_IN_TEST_INITIALIZE
            if (_skippedTestMethods.Select(_ => _.Name).Contains(TestContext?.TestName))
            {
                goto skip; // Short circuit the test.
            }
#endif
            skip:;
        }
        [TestMethod, RuntimeRequirement(UnreachableServer)]
        public async Task TestSomethingB()
        {
#if !ASSERT_IN_TEST_INITIALIZE
            // This option allows skipping the body, but the
            // test will show as PASS (because it did not FAIL).
            if (_skippedTestMethods.Select(_ => _.Name).Contains(TestContext?.TestName))
            {
                goto skip; // Short circuit the test.
            }
#endif
            skip:;
        }
        [TestMethod]
        public async Task TestSomethingC()
        {
#if !ASSERT_IN_TEST_INITIALIZE
            // This option allows skipping the body, but the
            // test will show as PASS (because it did not FAIL).
            if (_skippedTestMethods.Select(_ => _.Name).Contains(TestContext?.TestName))
            {
                goto skip; // Short circuit the test.
            }
#endif
            skip:;
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