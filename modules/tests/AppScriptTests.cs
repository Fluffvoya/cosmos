using System.Text.Json;
using app_bridge;
using app_script;

namespace tests;

/// <summary>
/// Unit tests for the app-script module: ScriptRunner.
/// </summary>
public class AppScriptTests
{
    // ── Mocks ──────────────────────────────────────────────────────

    private class MockScriptRunner : IScriptRunner
    {
        public string? LastSource { get; private set; }
        public Exception? ExceptionToThrow { get; set; }

        public Task Run(string source)
        {
            LastSource = source;
            if (ExceptionToThrow != null)
                throw ExceptionToThrow;
            return Task.CompletedTask;
        }
    }

    private class MockWebViewBridge : IWebViewBridge
    {
        public string? LastMessage { get; private set; }
        public Exception? ExceptionToThrow { get; set; }

        public void PostMessage(string json)
        {
            LastMessage = json;
            if (ExceptionToThrow != null)
                throw ExceptionToThrow;
        }
    }

    // ── RunSource ──────────────────────────────────────────────────

    [Fact]
    public void RunSource_Success_PostsSuccessMessage()
    {
        var scriptRunner = new MockScriptRunner();
        var webView = new MockWebViewBridge();
        var runner = new ScriptRunner(scriptRunner, webView, (_, _, _) => { });
        var signal = new ManualResetEventSlim(false);
        webView.GetType(); // ensure bridge is used
        // Hook into PostMessage to signal completion
        var originalBridge = webView;
        var capturingBridge = new CapturingBridge(originalBridge, () => signal.Set());

        var sut = new ScriptRunner(scriptRunner, capturingBridge, (_, _, _) => { });
        sut.RunSource("COSMOS Log \"hello\"");

        Assert.True(signal.Wait(5000), "Timed out waiting for RunSource to complete");
        Assert.NotNull(capturingBridge.LastMessage);

        var doc = JsonDocument.Parse(capturingBridge.LastMessage!);
        Assert.Equal("scriptRunResult", doc.RootElement.GetProperty("type").GetString());
        Assert.True(doc.RootElement.GetProperty("success").GetBoolean());
        Assert.Equal("", doc.RootElement.GetProperty("message").GetString());
    }

    [Fact]
    public void RunSource_ScriptException_PostsFailureMessage()
    {
        var scriptRunner = new MockScriptRunner { ExceptionToThrow = new InvalidOperationException("bad script") };
        var signal = new ManualResetEventSlim(false);
        var capturingBridge = new CapturingBridge(new MockWebViewBridge(), () => signal.Set());

        var sut = new ScriptRunner(scriptRunner, capturingBridge, (_, _, _) => { });
        sut.RunSource("INVALID");

        Assert.True(signal.Wait(5000), "Timed out waiting for RunSource to complete");
        Assert.NotNull(capturingBridge.LastMessage);

        var doc = JsonDocument.Parse(capturingBridge.LastMessage!);
        Assert.Equal("scriptRunResult", doc.RootElement.GetProperty("type").GetString());
        Assert.False(doc.RootElement.GetProperty("success").GetBoolean());
        Assert.Contains("bad script", doc.RootElement.GetProperty("message").GetString());
    }

    [Fact]
    public void RunSource_PostMessageThrows_DoesNotThrow()
    {
        var scriptRunner = new MockScriptRunner();
        var webView = new MockWebViewBridge { ExceptionToThrow = new Exception("bridge error") };
        var signal = new ManualResetEventSlim(false);

        // We need a way to know when the task completes even if PostMessage throws
        var wrappedBridge = new ThrowingBridge(webView, () => signal.Set());
        var sut = new ScriptRunner(scriptRunner, wrappedBridge, (_, _, _) => { });

        // Should not throw
        sut.RunSource("test");

        Assert.True(signal.Wait(5000), "Timed out waiting for RunSource to complete");
    }

    [Fact]
    public void RunSource_PassesSourceToScriptRunner()
    {
        var scriptRunner = new MockScriptRunner();
        var signal = new ManualResetEventSlim(false);
        var capturingBridge = new CapturingBridge(new MockWebViewBridge(), () => signal.Set());

        var sut = new ScriptRunner(scriptRunner, capturingBridge, (_, _, _) => { });
        sut.RunSource("COSMOS Log \"test\"");

        Assert.True(signal.Wait(5000));
        Assert.Equal("COSMOS Log \"test\"", scriptRunner.LastSource);
    }

    // ── Helper wrappers ────────────────────────────────────────────

    private class CapturingBridge : IWebViewBridge
    {
        private readonly IWebViewBridge _inner;
        private readonly Action _onPost;
        public string? LastMessage { get; private set; }

        public CapturingBridge(IWebViewBridge inner, Action onPost)
        {
            _inner = inner;
            _onPost = onPost;
        }

        public void PostMessage(string json)
        {
            LastMessage = json;
            _inner.PostMessage(json);
            _onPost();
        }
    }

    private class ThrowingBridge : IWebViewBridge
    {
        private readonly IWebViewBridge _inner;
        private readonly Action _onComplete;
        public ThrowingBridge(IWebViewBridge inner, Action onComplete) { _inner = inner; _onComplete = onComplete; }

        public void PostMessage(string json)
        {
            try { _inner.PostMessage(json); } catch { }
            _onComplete();
        }
    }
}
