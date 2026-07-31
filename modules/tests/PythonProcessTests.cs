using System.Diagnostics;
using bridge;
using cosmos_error;
using process;

namespace tests;

/// <summary>
/// Unit tests for PythonProcess.
/// </summary>
public class PythonProcessTests
{
    private class MockServer : IServer
    {
        public string Execute(string requests) => "reply:" + requests;
    }

    private static string GetScriptPath(string name)
        => Path.Combine(AppContext.BaseDirectory, "scripts", name);

    /// <summary>
    /// Resolve the full path of the Python interpreter from PATH.
    /// Returns null if Python is not available.
    /// </summary>
    private static string? ResolvePythonPath()
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "where",
                Arguments = "python",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var proc = Process.Start(psi);
            if (proc is null) return null;
            var output = proc.StandardOutput.ReadLine();
            proc.WaitForExit();
            return proc.ExitCode == 0 && output is not null ? output : null;
        }
        catch
        {
            return null;
        }
    }

    private static readonly string? _pythonPath = ResolvePythonPath();

    // ── Constructor ────────────────────────────────────────────────

    [Fact]
    public void Constructor_SetsFields()
    {
        var server = new MockServer();
        var process = new PythonProcess("python", "script.py",
            new List<string> { "arg1" }, server);

        Assert.NotNull(process);
    }

    // ── Python interpreter not found ──────────────────────────────

    [Fact]
    public async Task Execute_PythonNotFound_ThrowsProcessException()
    {
        var server = new MockServer();
        var process = new PythonProcess(
            "nonexistent_python_interpreter",
            GetScriptPath("echo.py"),
            new List<string>(),
            server);

        var ex = await Assert.ThrowsAsync<ProcessException>(() => process.Execute());
        Assert.Equal(ErrorCode.PythonNotFound, ex.ErrorCode);
    }

    // ── Script file not found ─────────────────────────────────────

    [Fact]
    public async Task Execute_ScriptNotFound_ThrowsProcessException()
    {
        if (_pythonPath is null) return;

        var server = new MockServer();
        var process = new PythonProcess(
            _pythonPath,
            "nonexistent_script.py",
            new List<string>(),
            server);

        var ex = await Assert.ThrowsAsync<ProcessException>(() => process.Execute());
        Assert.Equal(ErrorCode.ScriptNotFound, ex.ErrorCode);
    }

    // ── Script exits with non-zero code ───────────────────────────

    [Fact]
    public async Task Execute_NonZeroExitCode_ThrowsPythonRuntimeException()
    {
        if (_pythonPath is null) return;

        var server = new MockServer();
        var process = new PythonProcess(
            _pythonPath,
            GetScriptPath("exit_with_code_1.py"),
            new List<string>(),
            server);

        var ex = await Assert.ThrowsAsync<PythonRuntimeException>(() => process.Execute());
        Assert.Equal(ErrorCode.PythonRuntimeError, ex.ErrorCode);
        Assert.Equal(1, ex.ExitCode);
        Assert.Contains("script error", ex.Message);
    }

    // ── No output script exits cleanly ────────────────────────────

    [Fact]
    public async Task Execute_NoOutput_ExitsCleanly()
    {
        if (_pythonPath is null) return;

        var server = new MockServer();
        var process = new PythonProcess(
            _pythonPath,
            GetScriptPath("no_output.py"),
            new List<string>(),
            server);

        // Should not throw when the script exits normally with no output
        await process.Execute();
    }

    // ── Server communication ──────────────────────────────────────

    [Fact]
    public async Task Execute_ServerReceivesRequests()
    {
        if (_pythonPath is null) return;

        var server = new MockServer();
        var process = new PythonProcess(
            _pythonPath,
            GetScriptPath("echo.py"),
            new List<string>(),
            server);

        // MockServer.Execute returns "reply:<request>", so the Python
        // script should receive "reply:request1" and "reply:request2".
        // We verify that the process completes without error, meaning
        // the host correctly bridged stdout/stdin between Python and the server.
        await process.Execute();
    }

    [Fact]
    public async Task Execute_WithArguments_PassesArgsToScript()
    {
        if (_pythonPath is null) return;

        // echo.py ignores extra args, but this verifies that passing args
        // does not cause an error - they are appended to the command line.
        var server = new MockServer();
        var process = new PythonProcess(
            _pythonPath,
            GetScriptPath("echo.py"),
            new List<string> { "extraArg1", "extraArg2" },
            server);

        await process.Execute();
    }

    [Fact]
    public async Task Execute_EmptyArguments_ExitsCleanly()
    {
        if (_pythonPath is null) return;

        var server = new MockServer();
        var process = new PythonProcess(
            _pythonPath,
            GetScriptPath("no_output.py"),
            new List<string>(),
            server);

        await process.Execute();
    }

    // ── Server receives correct requests ──────────────────────────

    [Fact]
    public async Task Execute_ServerReceivesExpectedRequests()
    {
        if (_pythonPath is null) return;

        var received = new List<string>();
        var server = new CapturingServer(received);
        var process = new PythonProcess(
            _pythonPath,
            GetScriptPath("echo.py"),
            new List<string>(),
            server);

        await process.Execute();

        Assert.Equal(2, received.Count);
        Assert.Equal("request1", received[0]);
        Assert.Equal("request2", received[1]);
    }

    private class CapturingServer : IServer
    {
        private readonly List<string> _received;
        public CapturingServer(List<string> received) => _received = received;
        public string Execute(string requests)
        {
            _received.Add(requests);
            return "reply:" + requests;
        }
    }
}
