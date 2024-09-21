My understanding is that the goal is to ignore certain tests based on missing system prerequisites. In similar situations, what has worked for me is the approach shown in the code example below. Feel free to run this sample from my GitHub repo to see if it provides the behavior you're looking for. [Clone](https://github.com/IVSoftware/mstest-conditionals.git)

___

As far as what the issue might be, without seeing more code, your comment states:

> "System prerequisites not met" is discovered at runtime.

But you also make reference to the `[Ignore]` attribute and also mention `#if`:

> I could try comment or #ifdeffing the TestMethod attribute to sidestep the issue, rather than adding the Ignored attribute. 

Both alternatives — using `#if` or applying the `Ignore` attribute — are at odds with the runtime discovery of system prerequisites. Attributes like `[Ignore]` are evaluated at compile time and can't be dynamically altered once the code is running. Conditional compilation (`#if`) is also a compile-time feature. This, too, is inherently static and does not allow for changes during runtime. In terms of using `#if` effectively, the image below shows the immediate effect of unchecking a custom conditional compile symbol from the project's properties window.

[![disabled by preprocessor directive][1]][1]

___

#### How To Skip Based on Runtime Checking

In contrast, to accommodate the scenario where _**"System prerequisites not met" is discovered at runtime**_ the condition can be inspected within the `[ClassInitialize]` block and subsequently filtered in the `[TestInitialize]` block. Asserting the test as `Inconclusive` will have the desired behavior of skipping the test.

[![test skipped by assert in test initialized][2]][2]

```
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
```

##### Example:

Suppose we decorate three tests with a custom attribute named `[RuntimeRequirement]`

```
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
```

Here, the custom attribute is defined in the same file:

```
class RuntimeRequirementAttribute : Attribute
{
    public RuntimeRequirementAttribute(string requirement)
    {
        Requirement = requirement;
    }
    public string Requirement { get; }
}
```

___

##### SETUP: In [ClassInitialize] check the condition.

In this example, the server reachability (OR system capability OR whatever...) is tested. A non-blocking `MessageBox` is displayed, listing the tests that will be skipped but allowing the test execution to continue without operator input.

```
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
```

  [1]: https://i.sstatic.net/AJMdlfj8.png
  [2]: https://i.sstatic.net/82L6StfT.png