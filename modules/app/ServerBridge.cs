using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Web.WebView2.WinForms;
using app_scheduler;
using app_script;
using app_settings;
using bridge;
using public_model;
using server;

namespace app;

/// <summary>
/// Bridges IServer requests from cm-script to the WebView2 frontend.
/// Parses incoming requests, dispatches them to the frontend via postMessage,
/// and waits for responses back from the frontend.
/// </summary>
public class ServerBridge
{
    private readonly WebView2 _webView;
    private readonly MainWindow _mainWindow;
    private readonly Action<string, string, string> _logToUI; // (level, message, sender)
    private ScheduledTaskRunner? _taskRunner;
    private ScriptRunner? _scriptRunner;

    // Pending requests waiting for frontend responses.
    // Key: request ID (correlation ID), Value: TaskCompletionSource for the response.
    private readonly ConcurrentDictionary<string, TaskCompletionSource<string>> _pendingRequests = new();

    // Counter for generating unique request IDs.
    private long _requestIdCounter;

    public ServerBridge(WebView2 webView, MainWindow mainWindow, Action<string, string, string> logToUI)
    {
        _webView = webView;
        _mainWindow = mainWindow;
        _logToUI = logToUI;
    }

    /// <summary>
    /// Set the scheduled task runner instance.
    /// </summary>
    public void SetTaskRunner(ScheduledTaskRunner runner)
    {
        _taskRunner = runner;
    }

    /// <summary>
    /// Set the script runner instance for the Script Playground.
    /// </summary>
    public void SetScriptRunner(ScriptRunner runner)
    {
        _scriptRunner = runner;
    }

    /// <summary>
    /// Sends an internal log message to the frontend for display in the Log panel.
    /// </summary>
    public void SendInternalLog(string level, string message, string sender = "program")
    {
        try
        {
            var json = JsonSerializer.Serialize(new
            {
                type = "internalLog",
                level = level,
                message = message,
                sender = sender
            });

            if (_webView.InvokeRequired)
            {
                _webView.Invoke(() => _webView.CoreWebView2.PostWebMessageAsJson(json));
            }
            else
            {
                _webView.CoreWebView2.PostWebMessageAsJson(json);
            }
        }
        catch
        {
            // WebView not ready yet -- silently drop.
        }
    }

    /// <summary>
    /// Handles a message received from the frontend.
    /// Expected format: { "type": "response", "requestId": "...", "data": { ... } }
    /// </summary>
    public void HandleFrontendMessage(string messageJson)
    {
        try
        {
            // Remove outer quotes if present (WebView2 sometimes adds them)
            if (messageJson.StartsWith("\"") && messageJson.EndsWith("\""))
            {
                messageJson = JsonSerializer.Deserialize<string>(messageJson) ?? messageJson;
            }

            using var doc = JsonDocument.Parse(messageJson);
            var root = doc.RootElement;

            if (root.TryGetProperty("type", out var typeProp))
            {
                var type = typeProp.GetString();

                switch (type)
                {
                    case "response":
                        HandleResponse(root);
                        break;

                    case "settingsChanged":
                        HandleSettingsChanged(root);
                        break;

                    case "validatePythonPath":
                        HandleValidatePythonPath(root);
                        break;

                    case "browsePythonPath":
                        HandleBrowsePythonPath();
                        break;

                    case "browseStartupScriptPath":
                        HandleBrowseStartupScriptPath();
                        break;

                    case "validateStartupScriptPath":
                        HandleValidateStartupScriptPath(root);
                        break;

                    case "schedulerTasksChanged":
                        HandleSchedulerTasksChanged(root);
                        break;

                    case "scriptOutputChanged":
                        HandleScriptOutputChanged(root);
                        break;

                    case "schedulerRunTask":
                        HandleSchedulerRunTask(root);
                        break;

                    case "schedulerBrowseScript":
                        HandleSchedulerBrowseScript(root);
                        break;

                    case "scriptRunSource":
                        HandleScriptRunSource(root);
                        break;

                    default:
                        // Unknown message type -- ignore.
                        break;
                }
            }
        }
        catch (Exception ex)
        {
            _logToUI("Error", $"Failed to handle frontend message: {ex.Message}", "program");
        }
    }

    private void HandleResponse(JsonElement root)
    {
        if (!root.TryGetProperty("requestId", out var requestIdProp))
            return;

        var requestId = requestIdProp.GetString();
        if (requestId == null)
            return;

        // Extract the response data.
        string responseData = "";
        if (root.TryGetProperty("data", out var dataProp))
        {
            responseData = dataProp.GetRawText();
        }

        // Complete the pending request.
        if (_pendingRequests.TryRemove(requestId, out var tcs))
        {
            tcs.TrySetResult(responseData);
        }
    }

    private void HandleSettingsChanged(JsonElement root)
    {
        if (!root.TryGetProperty("settings", out var settingsProp))
            return;

        try
        {
            var newSettings = JsonSerializer.Deserialize<AppSettings>(settingsProp.GetRawText());
            if (newSettings != null)
            {
                _mainWindow.SettingsManager.Update(newSettings);
            }
        }
        catch (Exception ex)
        {
            _logToUI("Error", $"Failed to save settings: {ex.Message}", "program");
        }
    }

    /// <summary>
    /// Handle scheduled tasks changed from the Scheduler tab.
    /// Persists the updated tasks list to settings.
    /// </summary>
    private void HandleSchedulerTasksChanged(JsonElement root)
    {
        if (!root.TryGetProperty("tasks", out var tasksProp))
            return;

        try
        {
            var tasks = JsonSerializer.Deserialize<System.Collections.Generic.List<ScheduledTask>>(tasksProp.GetRawText());
            if (tasks != null)
            {
                _mainWindow.SettingsManager.Current.ScheduledTasks = tasks;
                _mainWindow.SettingsManager.Save();
            }
        }
        catch (Exception ex)
        {
            _logToUI("Error", $"Failed to save scheduled tasks: {ex.Message}", "program");
        }
    }

    /// <summary>
    /// Handle script output changes from the Script Terminal.
    /// Persists the updated output to settings.
    /// </summary>
    private void HandleScriptOutputChanged(JsonElement root)
    {
        if (!root.TryGetProperty("output", out var outputProp))
            return;

        try
        {
            var output = JsonSerializer.Deserialize<List<ScriptOutputEntry>>(outputProp.GetRawText());
            _mainWindow.SettingsManager.Current.ScriptOutput = output;
            _mainWindow.SettingsManager.Save();
        }
        catch (Exception ex)
        {
            _logToUI("Error", $"Failed to save script output: {ex.Message}", "program");
        }
    }

    /// <summary>
    /// Handle "Run Now" button from Scheduler tab.
    /// Executes the script immediately and reports result back.
    /// </summary>
    private void HandleSchedulerRunTask(JsonElement root)
    {
        if (!root.TryGetProperty("index", out var indexProp) ||
            !root.TryGetProperty("scriptPath", out var pathProp))
            return;

        var index = indexProp.GetInt32();
        var scriptPath = pathProp.GetString() ?? "";

        Task.Run(async () =>
        {
            var (success, message) = _taskRunner != null ? await _taskRunner.RunTaskNow(scriptPath) : (false, "Runner not initialized");

            var responseJson = JsonSerializer.Serialize(new
            {
                type = "schedulerRunResult",
                index = index,
                success = success,
                message = message
            });

            try
            {
                if (_webView.InvokeRequired)
                    _webView.Invoke(() => _webView.CoreWebView2.PostWebMessageAsJson(responseJson));
                else
                    _webView.CoreWebView2.PostWebMessageAsJson(responseJson);
            }
            catch { }
        });
    }

    /// <summary>
    /// Open a native file dialog for browsing script files.
    /// Sends the selected path back to the Scheduler tab.
    /// </summary>
    private void HandleSchedulerBrowseScript(JsonElement root)
    {
        if (!root.TryGetProperty("index", out var indexProp))
            return;

        var index = indexProp.GetInt32();
        string? selectedPath = null;

        if (_webView.InvokeRequired)
        {
            _webView.Invoke(() => { selectedPath = ShowScriptFileDialog(); });
        }
        else
        {
            selectedPath = ShowScriptFileDialog();
        }

        if (!string.IsNullOrEmpty(selectedPath))
        {
            var responseJson = JsonSerializer.Serialize(new
            {
                type = "schedulerBrowseResult",
                index = index,
                selectedPath = selectedPath
            });

            try
            {
                if (_webView.InvokeRequired)
                    _webView.Invoke(() => _webView.CoreWebView2.PostWebMessageAsJson(responseJson));
                else
                    _webView.CoreWebView2.PostWebMessageAsJson(responseJson);
            }
            catch { }
        }
    }

    /// <summary>
    /// Handle script execution request from the Script Playground tab.
    /// Delegates to ScriptRunner which runs the source and posts the result.
    /// </summary>
    private void HandleScriptRunSource(JsonElement root)
    {
        if (!root.TryGetProperty("source", out var sourceProp))
            return;

        var source = sourceProp.GetString() ?? "";

        if (_scriptRunner == null)
        {
            _logToUI("Error", "ScriptRunner not initialized.", "program");
            return;
        }

        _scriptRunner.RunSource(source);
    }

    private string? ShowScriptFileDialog()
    {
        using var dialog = new OpenFileDialog
        {
            Title = "Select cm-script File",
            Filter = "cm-script files (*.cms)|*.cms|All files (*.*)|*.*",
            CheckFileExists = true
        };

        return dialog.ShowDialog() == DialogResult.OK ? dialog.FileName : null;
    }

    /// <summary>
    /// Open a native file dialog for browsing startup script paths.
    /// Sends the selected path back to the frontend.
    /// </summary>
    private void HandleBrowseStartupScriptPath()
    {
        string? selectedPath = null;

        if (_webView.InvokeRequired)
        {
            _webView.Invoke(() => { selectedPath = ShowStartupScriptFileDialog(); });
        }
        else
        {
            selectedPath = ShowStartupScriptFileDialog();
        }

        if (!string.IsNullOrEmpty(selectedPath))
        {
            var responseJson = JsonSerializer.Serialize(new
            {
                type = "startupScriptBrowseResult",
                selectedPath = selectedPath
            });

            try
            {
                if (_webView.InvokeRequired)
                    _webView.Invoke(() => _webView.CoreWebView2.PostWebMessageAsJson(responseJson));
                else
                    _webView.CoreWebView2.PostWebMessageAsJson(responseJson);
            }
            catch { }
        }
    }

    private string? ShowStartupScriptFileDialog()
    {
        using var dialog = new OpenFileDialog
        {
            Title = "Select Startup Script",
            Filter = "cm-script files (*.cms)|*.cms|All files (*.*)|*.*",
            CheckFileExists = true
        };

        return dialog.ShowDialog() == DialogResult.OK ? dialog.FileName : null;
    }

    /// <summary>
    /// Validate a startup script path and send result back to the frontend.
    /// Only .cms files are considered valid.
    /// </summary>
    private void HandleValidateStartupScriptPath(JsonElement root)
    {
        if (!root.TryGetProperty("path", out var pathProp))
            return;

        var scriptPath = pathProp.GetString();
        var isValid = false;
        var errorMessage = "";

        if (string.IsNullOrWhiteSpace(scriptPath))
        {
            // Empty path is considered valid (startup script is optional)
            isValid = true;
        }
        else
        {
            try
            {
                // Expand environment variables like %USERPROFILE%
                var expandedPath = Environment.ExpandEnvironmentVariables(scriptPath);

                // Check if the file exists
                if (File.Exists(expandedPath))
                {
                    // Only .cms files are valid for startup scripts
                    if (expandedPath.EndsWith(".cms", StringComparison.OrdinalIgnoreCase))
                    {
                        isValid = true;
                    }
                    else
                    {
                        errorMessage = "Only .cms (cm-script) files are supported.";
                    }
                }
                else
                {
                    errorMessage = "File does not exist.";
                }
            }
            catch (Exception ex)
            {
                errorMessage = $"Invalid path: {ex.Message}";
            }
        }

        // Send validation result back to the frontend
        var response = JsonSerializer.Serialize(new
        {
            type = "startupScriptPathValidation",
            path = scriptPath,
            isValid = isValid,
            error = errorMessage
        });

        if (_webView.InvokeRequired)
        {
            _webView.Invoke(() => _webView.CoreWebView2.PostWebMessageAsJson(response));
        }
        else
        {
            _webView.CoreWebView2.PostWebMessageAsJson(response);
        }
    }

    /// <summary>
    /// Open a native file dialog for browsing Python interpreter paths.
    /// Sends the selected path back to the frontend.
    /// </summary>
    private void HandleBrowsePythonPath()
    {
        string? selectedPath = null;

        if (_webView.InvokeRequired)
        {
            _webView.Invoke(() => { selectedPath = ShowPythonFileDialog(); });
        }
        else
        {
            selectedPath = ShowPythonFileDialog();
        }

        if (!string.IsNullOrEmpty(selectedPath))
        {
            var responseJson = JsonSerializer.Serialize(new
            {
                type = "browseResult",
                selectedPath = selectedPath
            });

            try
            {
                if (_webView.InvokeRequired)
                    _webView.Invoke(() => _webView.CoreWebView2.PostWebMessageAsJson(responseJson));
                else
                    _webView.CoreWebView2.PostWebMessageAsJson(responseJson);
            }
            catch { }
        }
    }

    private string? ShowPythonFileDialog()
    {
        using var dialog = new OpenFileDialog
        {
            Title = "Select Python Interpreter",
            Filter = "Python (python.exe)|python.exe|All files (*.*)|*.*",
            CheckFileExists = true
        };

        return dialog.ShowDialog() == DialogResult.OK ? dialog.FileName : null;
    }

    /// <summary>
    /// Validate a Python path and send result back to the frontend.
    /// </summary>
    private void HandleValidatePythonPath(JsonElement root)
    {
        if (!root.TryGetProperty("path", out var pathProp))
            return;

        var pythonPath = pathProp.GetString();
        var isValid = false;
        var errorMessage = "";

        if (string.IsNullOrWhiteSpace(pythonPath))
        {
            // Empty path is considered valid (optional field)
            isValid = true;
        }
        else
        {
            try
            {
                // Expand environment variables like %USERPROFILE%
                var expandedPath = Environment.ExpandEnvironmentVariables(pythonPath);

                // Check if the file exists
                if (File.Exists(expandedPath))
                {
                    // Additional check: ensure it looks like a Python executable
                    var fileName = Path.GetFileName(expandedPath).ToLowerInvariant();
                    if (fileName.StartsWith("python") && (fileName.EndsWith(".exe") || fileName.EndsWith(".bat") || fileName.EndsWith(".cmd")))
                    {
                        isValid = true;
                    }
                    else
                    {
                        errorMessage = "Path does not point to a Python executable.";
                    }
                }
                else
                {
                    errorMessage = "File does not exist.";
                }
            }
            catch (Exception ex)
            {
                errorMessage = $"Invalid path: {ex.Message}";
            }
        }

        // Send validation result back to the frontend
        var response = JsonSerializer.Serialize(new
        {
            type = "pythonPathValidation",
            path = pythonPath,
            isValid = isValid,
            error = errorMessage
        });

        if (_webView.InvokeRequired)
        {
            _webView.Invoke(() => _webView.CoreWebView2.PostWebMessageAsJson(response));
        }
        else
        {
            _webView.CoreWebView2.PostWebMessageAsJson(response);
        }
    }

    /// <summary>
    /// IServer.Execute -- receives a request JSON string from cm-script,
    /// dispatches it to the frontend, and waits for the response.
    /// </summary>
    public string Execute(string requestJson)
    {
        if (string.IsNullOrEmpty(requestJson))
            return string.Empty;

        var request = Request.Deserialize(requestJson);
        if (request == null)
            return string.Empty;

        // Intercept ShowMessage requests and display a MessageBox.
        if (request.request == "ShowMessage" && request.args.Count >= 2)
        {
            var title = request.args[0];
            var message = request.args[1];
            
            // Show MessageBox on the UI thread
            if (_mainWindow.InvokeRequired)
            {
                _mainWindow.Invoke(() => MessageBox.Show(_mainWindow, message, title, MessageBoxButtons.OK, MessageBoxIcon.Information));
            }
            else
            {
                MessageBox.Show(_mainWindow, message, title, MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            
            var msgResponse = new Response(request.request, "");
            return Server.CreateResponse(msgResponse);
        }
        
        // Intercept Log/Warning/Error requests from script execution.
        // Only forward to the Script Terminal — do NOT route to the Log tab.
        if (request.request is "Log" or "Warning" or "Error" && request.args.Count > 0)
        {
            var level = request.request switch
                {
                    "Log" => "info",
                    "Warning" => "warning",
                    "Error" => "error",
                    _ => request.request
                };
            var message = request.args[0];

            // Forward to the Script Terminal only
            try
            {
                var scriptLogJson = JsonSerializer.Serialize(new
                {
                    type = "scriptLog",
                    level = level,
                    message = message
                });

                if (_webView.InvokeRequired)
                    _webView.Invoke(() => _webView.CoreWebView2.PostWebMessageAsJson(scriptLogJson));
                else
                    _webView.CoreWebView2.PostWebMessageAsJson(scriptLogJson);
            }
            catch { }

            var logResponse = new Response(request.request, "");
            return Server.CreateResponse(logResponse);
        }

        // Generate a unique request ID for correlation.
        var requestId = Interlocked.Increment(ref _requestIdCounter).ToString();

        // Create a TaskCompletionSource to wait for the frontend response.
        var tcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        _pendingRequests[requestId] = tcs;

        try
        {
            // Build the message to send to the frontend.
            var message = JsonSerializer.Serialize(new
            {
                type = "request",
                requestId = requestId,
                requestName = request.request,
                args = request.args
            });

            // Post message to frontend on the UI thread.
            if (_webView.InvokeRequired)
            {
                _webView.Invoke(() => _webView.CoreWebView2.PostWebMessageAsJson(message));
            }
            else
            {
                _webView.CoreWebView2.PostWebMessageAsJson(message);
            }

            // Wait for the response from the frontend (with timeout).
            if (tcs.Task.Wait(TimeSpan.FromSeconds(30)))
            {
                var responseData = tcs.Task.Result;

                // Build the Response object and serialize it.
                var response = new Response(request.request, responseData);
                return Server.CreateResponse(response);
            }
            else
            {
                // Timeout -- return empty response.
                _logToUI("Warning", $"Request '{request.request}' timed out after 30s", "program");
                var response = new Response(request.request, "");
                return Server.CreateResponse(response);
            }
        }
        catch (Exception ex)
        {
            _logToUI("Error", $"Execute failed: {ex.Message}", "program");
            var response = new Response(request.request, "");
            return Server.CreateResponse(response);
        }
        finally
        {
            _pendingRequests.TryRemove(requestId, out _);
        }
    }
}




