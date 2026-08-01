using System.Diagnostics;
using System.Text;
using bridge;
using cosmos_error;

namespace process;

public class ExecuteProcess
{
    private string _program;
    private List<string> _args;
    private IServer _server;

    public ExecuteProcess(string program, List<string> args, IServer server)
    {
        _program = program;
        _args = args;
        _server = server;
    }

    public async Task Execute()
    {
        // Validate program exists
        if (!File.Exists(_program))
        {
            throw new ProcessException(
                ErrorCode.ProcessCommunicationError,
                $"Program not found: {_program}");
        }

        var process = new Process();

        StringBuilder arguments = new StringBuilder();
        foreach (var item in _args)
        {
            if (arguments.Length > 0)
                arguments.Append(' ');
            arguments.Append(item);
        }

        process.StartInfo = new ProcessStartInfo()
        {
            FileName = _program,
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
                $"Failed to start process: {ex.Message}");
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
                        $"Failed to read from process: {ex.Message}");
                }

                if (request is null)
                    break;

                string reply;
                try
                {
                    reply = _server.Execute(request);
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
                        $"Failed to write to process: {ex.Message}");
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

        // Process exited with non-zero code
        if (process.ExitCode != 0)
        {
            string stderr = await process.StandardError.ReadToEndAsync();
            throw new ProcessException(
                ErrorCode.ProcessCommunicationError,
                $"Process exited abnormally (exit code: {process.ExitCode}): {stderr}");
        }
    }
}