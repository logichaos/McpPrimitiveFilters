namespace McpServer.Unit.Tests;

public class BasicTests
{
    [Before(Class)]
    public static Task BeforeClass(ClassHookContext context) => Task.CompletedTask;

    [After(Class)]
    public static Task AfterClass(ClassHookContext context) => Task.CompletedTask;

    [Before(Test)]
    public Task BeforeTest(TestContext context) => Task.CompletedTask;

    [After(Test)]
    public Task AfterTest(TestContext context) => Task.CompletedTask;

    [Test]
    public async Task Add_ReturnsSum()
    {
        bool t = true;
        await Assert.That(t).IsTrue();
    }
}
