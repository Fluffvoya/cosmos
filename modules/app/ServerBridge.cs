using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Web.WebView2.WinForms;
using app_launcher;
using app_password;
using cosmos_error;
using app_scheduler;
using app_script;
using app_settings;
using bridge;
using public_model;
using server;

namespace app;

/// <summary>
/// Shared JSON serializer options using camelCase to match frontend conventions.
/// </summary>
internal static class ServerBridgeJsonOptions
{
    public static readonly JsonSerializerOptions CamelCase = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };
}

/// <summary>
/// Bridges IServer requests from cm-script to the WebView2 frontend.
/// Parses incoming requests, dispatches them to the frontend via postMessage,
/// and waits for responses back from the frontend.
/// </summary>
public class ServerBridge
{
    private readonly WebView2 _webView;
    private readonly MainWindow _mainWindow;
    private readonly AppPasswordManager _passwordManager;
    private readonly Action<string, string, string> _logToUI; // (level, message, sender)
    private ScheduledTaskRunner? _taskRunner;
    private ScriptRunner? _scriptRunner;

    // Pending requests waiting for frontend responses.
    // Key: request ID (correlation ID), Value: TaskCompletionSource for the response.
    private readonly ConcurrentDictionary<string, TaskCompletionSource<string>> _pendingRequests = new();

    // Counter for generating unique request IDs.
    private long _requestIdCounter;

    // Current authenticated password for encryption/decryption
    private string? _currentPassword;

    public ServerBridge(WebView2 webView, MainWindow mainWindow, AppPasswordManager passwordManager, Action<string, string, string> logToUI)
    {
        _webView = webView;
        _mainWindow = mainWindow;
        _passwordManager = passwordManager;
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
            }, ServerBridgeJsonOptions.CamelCase);

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

                    case "startConfigChanged":
                        HandleStartConfigChanged(root);
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

                    case "passwordManagerCheckSetup":
                        HandlePasswordManagerCheckSetup();
                        break;

                    case "passwordManagerSetup":
                        HandlePasswordManagerSetup(root);
                        break;

                    case "passwordManagerAuth":
                        HandlePasswordManagerAuth(root);
                        break;

                    case "passwordManagerChangePassword":
                        HandlePasswordManagerChangePassword(root);
                        break;

                    case "passwordManagerSaveData":
                        HandlePasswordManagerSaveData(root);
                        break;

                    case "launcherLoadApps":
                        HandleLauncherLoadApps();
                        break;

                    case "launcherLaunchApp":
                        HandleLauncherLaunchApp(root);
                        break;

                    case "launcherAddApp":
                        HandleLauncherAddApp(root);
                        break;

                    case "launcherRemoveApp":
                        HandleLauncherRemoveApp(root);
                        break;

                    case "launcherBrowseExecutable":
                        HandleLauncherBrowseExecutable();
                        break;

                    case "launcherGetIcon":
                        HandleLauncherGetIcon(root);
                        break;

                    case "launcherReorderApps":
                        HandleLauncherReorderApps(root);
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
            var newSettings = JsonSerializer.Deserialize<AppSettings>(settingsProp.GetRawText(), ServerBridgeJsonOptions.CamelCase);
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

    private void HandleStartConfigChanged(JsonElement root)
    {
        if (!root.TryGetProperty("config", out var configProp))
            return;

        try
        {
            var config = JsonSerializer.Deserialize<StartConfig>(configProp.GetRawText(), ServerBridgeJsonOptions.CamelCase);
            if (config != null)
            {
                DataStore.Save("start-config.json", config);
            }
        }
        catch (Exception ex)
        {
            _logToUI("Error", $"Failed to save start config: {ex.Message}", "program");
        }
    }

    /// <summary>
    /// Handle scheduled tasks changed from the Scheduler tab.
    /// Persists the updated tasks list to tasks.json.
    /// </summary>
    private void HandleSchedulerTasksChanged(JsonElement root)
    {
        if (!root.TryGetProperty("tasks", out var tasksProp))
            return;

        try
        {
            var tasks = JsonSerializer.Deserialize<List<ScheduledTask>>(tasksProp.GetRawText(), ServerBridgeJsonOptions.CamelCase);
            if (tasks != null)
            {
                DataStore.Save("tasks.json", tasks);
            }
        }
        catch (Exception ex)
        {
            _logToUI("Error", $"Failed to save scheduled tasks: {ex.Message}", "program");
        }
    }

    /// <summary>
    /// Handle script output changes from the Script Terminal.
    /// Persists the updated output to script-output.json.
    /// </summary>
    private void HandleScriptOutputChanged(JsonElement root)
    {
        if (!root.TryGetProperty("output", out var outputProp))
            return;

        try
        {
            var output = JsonSerializer.Deserialize<List<ScriptOutputEntry>>(outputProp.GetRawText(), ServerBridgeJsonOptions.CamelCase);
            DataStore.Save("script-output.json", output);
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
            }, ServerBridgeJsonOptions.CamelCase);

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
            }, ServerBridgeJsonOptions.CamelCase);

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
            }, ServerBridgeJsonOptions.CamelCase);

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
        }, ServerBridgeJsonOptions.CamelCase);

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
            }, ServerBridgeJsonOptions.CamelCase);

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
        }, ServerBridgeJsonOptions.CamelCase);

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

        // Intercept MessageBox requests and display a native dialog.
        if (request.request == "MessageBox" && request.args.Count >= 2)
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
                }, ServerBridgeJsonOptions.CamelCase);

                if (_webView.InvokeRequired)
                    _webView.Invoke(() => _webView.CoreWebView2.PostWebMessageAsJson(scriptLogJson));
                else
                    _webView.CoreWebView2.PostWebMessageAsJson(scriptLogJson);
            }
            catch { }

            var logResponse = new Response(request.request, "");
            return Server.CreateResponse(logResponse);
        }

        // Intercept MessageBar — forward to frontend as a non-blocking toast.
        if (request.request == "MessageBar" && request.args.Count >= 2)
        {
            var message = request.args[0];
            var level = request.args[1];

            try
            {
                var toastJson = JsonSerializer.Serialize(new
                {
                    type = "messageBar",
                    message = message,
                    level = level
                }, ServerBridgeJsonOptions.CamelCase);

                if (_webView.InvokeRequired)
                    _webView.Invoke(() => _webView.CoreWebView2.PostWebMessageAsJson(toastJson));
                else
                    _webView.CoreWebView2.PostWebMessageAsJson(toastJson);
            }
            catch { }

            var barResponse = new Response(request.request, "");
            return Server.CreateResponse(barResponse);
        }

        // Intercept PlayRingtone — forward audio file to frontend as base64 data URL.
        // (file:/// URIs are blocked from https:// pages by WebView2's cross-origin policy.)
        if (request.request == "PlayRingtone" && request.args.Count >= 1)
        {
            var audioPath = Environment.ExpandEnvironmentVariables(request.args[0]);

            try
            {
                var audioBytes = File.ReadAllBytes(audioPath);
                var mimeType = GetMimeType(audioPath);
                var base64 = Convert.ToBase64String(audioBytes);
                var audioUri = $"data:{mimeType};base64,{base64}";

                var ringtoneJson = JsonSerializer.Serialize(new
                {
                    type = "ringtonePlay",
                    filePath = audioPath,
                    audioUrl = audioUri
                }, ServerBridgeJsonOptions.CamelCase);

                if (_webView.InvokeRequired)
                    _webView.Invoke(() => _webView.CoreWebView2.PostWebMessageAsJson(ringtoneJson));
                else
                    _webView.CoreWebView2.PostWebMessageAsJson(ringtoneJson);
            }
            catch (Exception ex)
            {
                _logToUI("Error", $"PlayRingtone failed: {ex.Message}", "program");
            }

            var ringtoneResponse = new Response(request.request, "");
            return Server.CreateResponse(ringtoneResponse);
        }

        // Intercept OpenRegisteredApp — look up a registered app by name and launch it.
        if (request.request == "OpenRegisteredApp" && request.args.Count >= 1)
        {
            var appName = request.args[0];
            var launchResult = HandleOpenRegisteredApp(appName);
            var launchResponse = new Response(request.request, launchResult);
            return Server.CreateResponse(launchResponse);
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
            }, ServerBridgeJsonOptions.CamelCase);

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

    /// <summary>
    /// Handle password manager setup check request.
    /// Sends response indicating whether master password is set up.
    /// </summary>
    private void HandlePasswordManagerCheckSetup()
    {
        var isSetup = _passwordManager.IsSetup();

        var responseJson = JsonSerializer.Serialize(new
        {
            type = "passwordManagerSetupCheck",
            isSetup = isSetup
        }, ServerBridgeJsonOptions.CamelCase);

        try
        {
            if (_webView.InvokeRequired)
                _webView.Invoke(() => _webView.CoreWebView2.PostWebMessageAsJson(responseJson));
            else
                _webView.CoreWebView2.PostWebMessageAsJson(responseJson);
        }
        catch { }
    }

    /// <summary>
    /// Handle password manager initial setup.
    /// Creates master password hash and initializes empty data.
    /// </summary>
    private void HandlePasswordManagerSetup(JsonElement root)
    {
        if (!root.TryGetProperty("password", out var passwordProp))
            return;

        var password = passwordProp.GetString();
        if (string.IsNullOrEmpty(password))
            return;

        var success = _passwordManager.Setup(password);

        if (success)
        {
            _currentPassword = password;

            var responseJson = JsonSerializer.Serialize(new
            {
                type = "passwordManagerSetupSuccess"
            }, ServerBridgeJsonOptions.CamelCase);

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
    /// Handle password manager authentication.
    /// Verifies password and returns decrypted data on success.
    /// </summary>
    private void HandlePasswordManagerAuth(JsonElement root)
    {
        if (!root.TryGetProperty("password", out var passwordProp))
            return;

        var password = passwordProp.GetString();
        if (string.IsNullOrEmpty(password))
            return;

        var isValid = _passwordManager.VerifyPassword(password);

        if (isValid)
        {
            _currentPassword = password;
            var platforms = _passwordManager.LoadData(password);

            var responseJson = JsonSerializer.Serialize(new
            {
                type = "passwordManagerAuthSuccess",
                platforms = platforms
            }, ServerBridgeJsonOptions.CamelCase);

            try
            {
                if (_webView.InvokeRequired)
                    _webView.Invoke(() => _webView.CoreWebView2.PostWebMessageAsJson(responseJson));
                else
                    _webView.CoreWebView2.PostWebMessageAsJson(responseJson);
            }
            catch { }
        }
        else
        {
            var responseJson = JsonSerializer.Serialize(new
            {
                type = "passwordManagerAuthFailure",
                message = "Incorrect password."
            }, ServerBridgeJsonOptions.CamelCase);

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
    /// Handle password manager password change request.
    /// Verifies old password and updates to new password.
    /// </summary>
    private void HandlePasswordManagerChangePassword(JsonElement root)
    {
        if (!root.TryGetProperty("currentPassword", out var currentProp) ||
            !root.TryGetProperty("newPassword", out var newProp))
            return;

        var currentPassword = currentProp.GetString();
        var newPassword = newProp.GetString();

        if (string.IsNullOrEmpty(currentPassword) || string.IsNullOrEmpty(newPassword))
            return;

        var success = _passwordManager.ChangePassword(currentPassword, newPassword);

        if (success)
        {
            _currentPassword = newPassword;

            var responseJson = JsonSerializer.Serialize(new
            {
                type = "passwordManagerChangePasswordSuccess"
            }, ServerBridgeJsonOptions.CamelCase);

            try
            {
                if (_webView.InvokeRequired)
                    _webView.Invoke(() => _webView.CoreWebView2.PostWebMessageAsJson(responseJson));
                else
                    _webView.CoreWebView2.PostWebMessageAsJson(responseJson);
            }
            catch { }
        }
        else
        {
            var responseJson = JsonSerializer.Serialize(new
            {
                type = "passwordManagerChangePasswordFailure",
                message = "Failed to change password. Please verify your current password."
            }, ServerBridgeJsonOptions.CamelCase);

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
    /// Handle password manager save data request.
    /// Encrypts and saves platform data.
    /// </summary>
    private void HandlePasswordManagerSaveData(JsonElement root)
    {
        if (_currentPassword == null)
        {
            _logToUI("Error", "Cannot save password data: not authenticated", "program");
            return;
        }

        if (!root.TryGetProperty("platforms", out var platformsProp))
            return;

        try
        {
            var platforms = JsonSerializer.Deserialize<PlatformData[]>(platformsProp.GetRawText(), ServerBridgeJsonOptions.CamelCase);
            if (platforms != null)
            {
                _passwordManager.SaveData(_currentPassword, platforms);
            }
        }
        catch (Exception ex)
        {
            _logToUI("Error", $"Failed to save password data: {ex.Message}", "program");
        }
    }

    private static string GetMimeType(string filePath)
    {
        var ext = Path.GetExtension(filePath).ToLowerInvariant();
        return ext switch
        {
            ".mp3" => "audio/mpeg",
            ".wav" => "audio/wav",
            ".ogg" => "audio/ogg",
            ".flac" => "audio/flac",
            ".aac" => "audio/aac",
            ".m4a" => "audio/mp4",
            ".wma" => "audio/x-ms-wma",
            ".opus" => "audio/opus",
            ".webm" => "audio/webm",
            _ => "audio/mpeg" // fallback
        };
    }

    /// <summary>
    /// Handle request to load the list of registered apps.
    /// Sends the full list back to the frontend.
    /// </summary>
    private void HandleLauncherLoadApps()
    {
        try
        {
            var apps = AppRegistry.GetAll();
            var responseJson = JsonSerializer.Serialize(new
            {
                type = "launcherAppsLoaded",
                apps = apps
            }, ServerBridgeJsonOptions.CamelCase);

            if (_webView.InvokeRequired)
                _webView.Invoke(() => _webView.CoreWebView2.PostWebMessageAsJson(responseJson));
            else
                _webView.CoreWebView2.PostWebMessageAsJson(responseJson);
        }
        catch (Exception ex)
        {
            _logToUI("Error", $"Failed to load registered apps: {ex.Message}", "program");
        }
    }

    /// <summary>
    /// Handle request to launch a registered app from the frontend.
    /// </summary>
    private void HandleLauncherLaunchApp(JsonElement root)
    {
        if (!root.TryGetProperty("appName", out var appNameProp))
            return;

        var appName = appNameProp.GetString();
        if (string.IsNullOrEmpty(appName))
            return;

        var result = HandleOpenRegisteredApp(appName);
        var success = result == "ok";

        var responseJson = JsonSerializer.Serialize(new
        {
            type = "launcherLaunchResult",
            appName = appName,
            success = success,
            message = success ? "" : result
        }, ServerBridgeJsonOptions.CamelCase);

        try
        {
            if (_webView.InvokeRequired)
                _webView.Invoke(() => _webView.CoreWebView2.PostWebMessageAsJson(responseJson));
            else
                _webView.CoreWebView2.PostWebMessageAsJson(responseJson);
        }
        catch { }
    }

    /// <summary>
    /// Handle request to add a new registered app from the frontend.
    /// </summary>
    private void HandleLauncherAddApp(JsonElement root)
    {
        if (!root.TryGetProperty("name", out var nameProp) ||
            !root.TryGetProperty("path", out var pathProp))
            return;

        var name = nameProp.GetString();
        var path = pathProp.GetString();
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(path))
        {
            SendLauncherAddResult(false, "Name and path are required.");
            return;
        }

        string? arguments = null;
        if (root.TryGetProperty("arguments", out var argsProp) && argsProp.ValueKind == JsonValueKind.String)
        {
            arguments = argsProp.GetString();
        }

        try
        {
            var app = RegisteredApp.Create(name, path, arguments);
            AppRegistry.Add(app);
            SendLauncherAddResult(true, null);
        }
        catch (LauncherException ex)
        {
            SendLauncherAddResult(false, ex.Message);
        }
        catch (Exception ex)
        {
            SendLauncherAddResult(false, ex.Message);
        }
    }

    /// <summary>
    /// Handle request to remove a registered app from the frontend.
    /// </summary>
    private void HandleLauncherRemoveApp(JsonElement root)
    {
        if (!root.TryGetProperty("appName", out var appNameProp))
            return;

        var appName = appNameProp.GetString();
        if (string.IsNullOrWhiteSpace(appName))
            return;

        try
        {
            AppRegistry.Remove(appName);
            SendLauncherRemoveResult(true, null);
        }
        catch (LauncherException ex)
        {
            SendLauncherRemoveResult(false, ex.Message);
        }
        catch (Exception ex)
        {
            SendLauncherRemoveResult(false, ex.Message);
        }
    }

    /// <summary>
    /// Open a native file dialog for browsing executables.
    /// Sends the selected path back to the Launcher tab.
    /// </summary>
    private void HandleLauncherBrowseExecutable()
    {
        string? selectedPath = null;

        if (_webView.InvokeRequired)
        {
            _webView.Invoke(() => { selectedPath = ShowExecutableFileDialog(); });
        }
        else
        {
            selectedPath = ShowExecutableFileDialog();
        }

        if (!string.IsNullOrEmpty(selectedPath))
        {
            var responseJson = JsonSerializer.Serialize(new
            {
                type = "launcherBrowseResult",
                selectedPath = selectedPath
            }, ServerBridgeJsonOptions.CamelCase);

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

    private string? ShowExecutableFileDialog()
    {
        // Run the file dialog on a dedicated STA thread to avoid COM/Shell
        // conflicts with the WebView2 control on the UI thread. When browsing
        // .exe files, Windows Shell loads icons and metadata which can trigger
        // shell extensions that clash with WebView2's COM state and crash the
        // host process (STATUS_BREAKPOINT 0x80000003).
        string? selectedPath = null;

        var thread = new Thread(() =>
        {
            try
            {
                using var dialog = new OpenFileDialog
                {
                    Title = "Select Application",
                    Filter = "Executable files (*.exe)|*.exe|All files (*.*)|*.*",
                    CheckFileExists = true
                };

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    selectedPath = dialog.FileName;
                }
            }
            catch (Exception ex)
            {
                _logToUI("Error", $"Executable file dialog failed: {ex.Message}", "program");
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        return selectedPath;
    }

    /// <summary>
    /// Extract the icon from an executable file and send it to the frontend as base64.
    /// </summary>
    private void HandleLauncherGetIcon(JsonElement root)
    {
        if (!root.TryGetProperty("appName", out var appNameProp) ||
            !root.TryGetProperty("path", out var pathProp))
            return;

        var appName = appNameProp.GetString() ?? "";
        var appPath = pathProp.GetString() ?? "";

        string? iconBase64 = null;

        try
        {
            var expandedPath = Environment.ExpandEnvironmentVariables(appPath);
            if (File.Exists(expandedPath))
            {
                using var icon = Icon.ExtractAssociatedIcon(expandedPath);
                if (icon != null)
                {
                    using var bitmap = icon.ToBitmap();
                    using var ms = new MemoryStream();
                    bitmap.Save(ms, ImageFormat.Png);
                    iconBase64 = Convert.ToBase64String(ms.ToArray());
                }
            }
        }
        catch
        {
            // Icon extraction can fail for various reasons (non-PE files, etc.)
            // Silently fall back to no icon.
        }

        var responseJson = JsonSerializer.Serialize(new
        {
            type = "launcherIconLoaded",
            appName = appName,
            iconData = iconBase64
        }, ServerBridgeJsonOptions.CamelCase);

        try
        {
            if (_webView.InvokeRequired)
                _webView.Invoke(() => _webView.CoreWebView2.PostWebMessageAsJson(responseJson));
            else
                _webView.CoreWebView2.PostWebMessageAsJson(responseJson);
        }
        catch { }
    }

    /// <summary>
    /// Handle request to reorder registered apps from the frontend drag-and-drop.
    /// </summary>
    private void HandleLauncherReorderApps(JsonElement root)
    {
        if (!root.TryGetProperty("fromIndex", out var fromProp) ||
            !root.TryGetProperty("toIndex", out var toProp))
            return;

        var fromIndex = fromProp.GetInt32();
        var toIndex = toProp.GetInt32();

        try
        {
            AppRegistry.Reorder(fromIndex, toIndex);
            var apps = AppRegistry.GetAll();
            var responseJson = JsonSerializer.Serialize(new
            {
                type = "launcherReorderResult",
                success = true,
                apps = apps
            }, ServerBridgeJsonOptions.CamelCase);

            if (_webView.InvokeRequired)
                _webView.Invoke(() => _webView.CoreWebView2.PostWebMessageAsJson(responseJson));
            else
                _webView.CoreWebView2.PostWebMessageAsJson(responseJson);
        }
        catch (Exception ex)
        {
            _logToUI("Error", $"Failed to reorder apps: {ex.Message}", "program");
            var responseJson = JsonSerializer.Serialize(new
            {
                type = "launcherReorderResult",
                success = false,
                message = ex.Message
            }, ServerBridgeJsonOptions.CamelCase);

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

    private void SendLauncherAddResult(bool success, string? message)
    {
        var apps = AppRegistry.GetAll();
        var responseJson = JsonSerializer.Serialize(new
        {
            type = "launcherAddResult",
            success = success,
            message = message ?? "",
            apps = apps
        }, ServerBridgeJsonOptions.CamelCase);

        try
        {
            if (_webView.InvokeRequired)
                _webView.Invoke(() => _webView.CoreWebView2.PostWebMessageAsJson(responseJson));
            else
                _webView.CoreWebView2.PostWebMessageAsJson(responseJson);
        }
        catch { }
    }

    private void SendLauncherRemoveResult(bool success, string? message)
    {
        var apps = AppRegistry.GetAll();
        var responseJson = JsonSerializer.Serialize(new
        {
            type = "launcherRemoveResult",
            success = success,
            message = message ?? "",
            apps = apps
        }, ServerBridgeJsonOptions.CamelCase);

        try
        {
            if (_webView.InvokeRequired)
                _webView.Invoke(() => _webView.CoreWebView2.PostWebMessageAsJson(responseJson));
            else
                _webView.CoreWebView2.PostWebMessageAsJson(responseJson);
        }
        catch { }
    }

    /// <summary>
    /// Handle an OpenRegisteredApp request by looking up the app and launching it.
    /// Returns "ok" on success, or an error message on failure.
    /// </summary>
    private string HandleOpenRegisteredApp(string appName)
    {
        try
        {
            var app = AppRegistry.GetByName(appName);
            if (app == null)
            {
                _logToUI("Warning", $"OpenRegisteredApp: '{appName}' not found", "program");
                return "error:not_found";
            }

            var expandedPath = Environment.ExpandEnvironmentVariables(app.Path);
            if (!File.Exists(expandedPath))
            {
                _logToUI("Error", $"OpenRegisteredApp: path does not exist: {expandedPath}", "program");
                return "error:path_invalid";
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = expandedPath,
                UseShellExecute = true
            };

            if (!string.IsNullOrWhiteSpace(app.Arguments))
            {
                startInfo.Arguments = app.Arguments;
            }

            Process.Start(startInfo);
            return "ok";
        }
        catch (Exception ex)
        {
            _logToUI("Error", $"OpenRegisteredApp failed: {ex.Message}", "program");
            return $"error:{ex.Message}";
        }
    }
}




