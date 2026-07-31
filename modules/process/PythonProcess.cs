using System.Diagnostics;
using System.Text;
using bridge;
using cosmos_error;

namespace process;

public class PythonProcess
{
    private string python;
    private string script;
    private List<string> args;
    private IServer server;

    public PythonProcess(string python, string script, List<string> args, IServer server)
    {
        this.python = python;
        this.script = script;
        this.args = args;
        this.server = server;
    }

    public async Task Execute()
    {
        // Validate Python interpreter exists
        if (!File.Exists(python))
        {
            throw new ProcessException(
                ErrorCode.PythonNotFound,
                $"Python interpreter not found: {python}");
        }

        // Validate script file exists
        if (!File.Exists(script))
        {
            throw new ProcessException(
                ErrorCode.ScriptNotFound,
                $"Python script file not found: {script}");
        }

        var process = new Process();

        StringBuilder arguments = new StringBuilder();
        arguments.Append(script);
        foreach (var item in args)
        {
            arguments.Append(' ');
            arguments.Append(item);
        }

        process.StartInfo = new ProcessStartInfo()
        {
            FileName = python,
            Arguments = arguments.ToString(),

            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,

            UseShellExecute = false,
            CreateNoWindow = true
        };

        // Process start failure
        try
        {
            process.Start();
        }
        catch (Exception ex)
        {
            throw new ProcessException(
                ErrorCode.ProcessCommunicationError,
                $"Failed to start Python process: {ex.Message}");
        }

        try
        {
            while (true)
            {
                string? request = null;
                try
                {
                    request = await process.StandardOutput.ReadLineAsync();
                }
                catch (Exception ex)
                {
                    throw new ProcessException(
                        ErrorCode.ProcessCommunicationError,
                        $"Failed to read from Python process: {ex.Message}");
                }

                if (request is null)
                    break;

                string reply;
                try
                {
                    reply = server.Execute(request);
                }
                catch (Exception ex)
                {
                    throw new ProcessException(
                        ErrorCode.ProcessCommunicationError,
                        $"Server failed to handle request: {ex.Message}");
                }

                try
                {
                    await process.StandardInput.WriteLineAsync(reply);
                    await process.StandardInput.FlushAsync();
                }
                catch (Exception ex)
                {
                    throw new ProcessException(
                        ErrorCode.ProcessCommunicationError,
                        $"Failed to write to Python process: {ex.Message}");
                }
            }
        }
        finally
        {
            if (!process.HasExited)
            {
                await process.WaitForExitAsync();
            }
        }

        // Python internal error: process exited with non-zero code
        if (process.ExitCode != 0)
        {
            string stderr = await process.StandardError.ReadToEndAsync();
            throw new PythonRuntimeException(
                ErrorCode.PythonRuntimeError,
                $"Python process exited abnormally (exit code: {process.ExitCode}): {stderr}",
                process.ExitCode);
        }
    }
}
