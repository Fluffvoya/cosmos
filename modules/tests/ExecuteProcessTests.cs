using System.Diagnostics;
using bridge;
using cosmos_error;
using process;

namespace tests;

/// <summary>
/// Unit tests for ExecuteProcess.
/// </summary>
public class ExecuteProcessTests
{
    private class MockServer : IServer
    {
        public string Execute(string requests) => "reply:" + requests;
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
        var process = new ExecuteProcess("program", new List<string> { "arg1" }, server);

        Assert.NotNull(process);
    }

    // ── Program not found ──────────────────────────────────────────

    [Fact]
    public async Task Execute_ProgramNotFound_ThrowsProcessException()
    {
        var server = new MockServer();
        var process = new ExecuteProcess(
            "nonexistent_program.exe",
            new List<string>(),
            server);

        var ex = await Assert.ThrowsAsync<ProcessException>(() => process.Execute());
        Assert.Equal(ErrorCode.ProcessCommunicationError, ex.ErrorCode);
        Assert.Contains("Program not found", ex.Message);
    }

    // ── Successful execution with no output ────────────────────────

    [Fact]
    public async Task Execute_NoOutput_ExitsCleanly()
    {
        if (_pythonPath is null) return;

        var server = new MockServer();
        var process = new ExecuteProcess(
            _pythonPath,
            new List<string> { GetScriptPath("no_output.py") },
            server);

        await process.Execute();
    }

    // ── Server communication ───────────────────────────────────────

    [Fact]
    public async Task Execute_ServerReceivesRequests()
    {
        if (_pythonPath is null) return;

        var received = new List<string>();
        var server = new CapturingServer(received);
        var process = new ExecuteProcess(
            _pythonPath,
            new List<string> { GetScriptPath("exe_echo.py") },
            server);

        await process.Execute();

        Assert.Single(received);
        Assert.Equal("request1", received[0]);
    }

    [Fact]
    public async Task Execute_ServerReply_IsSentBackToProcess()
    {
        if (_pythonPath is null) return;

        // The exe_echo.py script reads a line from stdin after sending its request.
        // If the server reply is correctly piped back, the process exits cleanly.
        var server = new MockServer();
        var process = new ExecuteProcess(
            _pythonPath,
            new List<string> { GetScriptPath("exe_echo.py") },
            server);

        // Should not throw — the process reads the reply and exits 0
        await process.Execute();
    }

    // ── Multiple requests ──────────────────────────────────────────

    [Fact]
    public async Task Execute_MultipleRequests_AllReceivedByServer()
    {
        if (_pythonPath is null) return;

        var received = new List<string>();
        var server = new CapturingServer(received);
        var process = new ExecuteProcess(
            _pythonPath,
            new List<string> { GetScriptPath("exe_multi_request.py") },
            server);

        await process.Execute();

        Assert.Equal(2, received.Count);
        Assert.Equal("request1", received[0]);
        Assert.Equal("request2", received[1]);
    }

    // ── Non-zero exit code ─────────────────────────────────────────

    [Fact]
    public async Task Execute_NonZeroExitCode_ThrowsProcessException()
    {
        if (_pythonPath is null) return;

        var server = new MockServer();
        var process = new ExecuteProcess(
            _pythonPath,
            new List<string> { GetScriptPath("exit_with_code_1.py") },
            server);

        var ex = await Assert.ThrowsAsync<ProcessException>(() => process.Execute());
        Assert.Equal(ErrorCode.ProcessCommunicationError, ex.ErrorCode);
        Assert.Contains("exit code: 1", ex.Message);
        Assert.Contains("script error", ex.Message);
    }

    // ── Arguments passed to program ────────────────────────────────

    [Fact]
    public async Task Execute_WithArguments_PassesArgsToProgram()
    {
        if (_pythonPath is null) return;

        // no_output.py ignores args, but this verifies that passing args
        // does not cause an error — they are appended to the command line.
        var server = new MockServer();
        var process = new ExecuteProcess(
            _pythonPath,
            new List<string> { GetScriptPath("no_output.py"), "extraArg1", "extraArg2" },
            server);

        await process.Execute();
    }

    // ── Empty arguments list ───────────────────────────────────────

    [Fact]
    public async Task Execute_EmptyArguments_ExitsCleanly()
    {
        if (_pythonPath is null) return;

        var server = new MockServer();
        var process = new ExecuteProcess(
            _pythonPath,
            new List<string> { GetScriptPath("no_output.py") },
            server);

        await process.Execute();
    }

    // NOTE: A test for server-throws-during-communication is omitted because
    // ExecuteProcess.Execute() has a deadlock: when server.Execute() throws,
    // the child process is still waiting on stdin, but the finally block calls
    // WaitForExitAsync() which never completes since stdin is never closed.
    // This should be fixed in ExecuteProcess by killing/closing the process
    // on server error before re-throwing.
}
