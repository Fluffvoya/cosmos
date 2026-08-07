using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Web.WebView2.WinForms;
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
            // WebView not ready yet ??silently drop.
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

                    default:
                        // Unknown message type ??ignore.
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

    }

    /// <summary>
    /// Open a native file dialog for browsing Python interpreter paths.
    /// Sends the selected path back to the frontend.
    /// </summary>
    private void HandleBrowsePythonPath()
    {
        // Use WinForms OpenFileDialog since we're already in a WinForms app
        string? selectedPath = null;

        if (_webView.InvokeRequired)
        {
            _webView.Invoke(() =>
            {
                selectedPath = ShowOpenFileDialog();
            });
        }
        else
        {
            selectedPath = ShowOpenFileDialog();
        }

        // Send result back to frontend
        var response = JsonSerializer.Serialize(new
        {
            type = "browseResult",
            selectedPath = selectedPath ?? string.Empty
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
    /// Show the native file dialog for selecting a Python executable.
    /// </summary>
    private string? ShowOpenFileDialog()
    {
        using var dialog = new OpenFileDialog
        {
            Title = "Select Python Interpreter",
            Filter = "Python Executables (python*.exe)|python*.exe|All Executables (*.exe;*.bat;*.cmd)|*.exe;*.bat;*.cmd|All Files (*.*)|*.*",
            FilterIndex = 1,
            CheckFileExists = true
        };

        // Try to set initial directory to common Python install locations
        var commonPaths = new[]
        {
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\Programs\Python",
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles) + @"\Python",
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86) + @"\Python"
        };

        foreach (var path in commonPaths)
        {
            if (Directory.Exists(path))
            {
                dialog.InitialDirectory = path;
                break;
            }
        }

        if (dialog.ShowDialog() == DialogResult.OK)
        {
            return dialog.FileName;
        }

        return null;
    }

    /// <summary>
    /// Validate a Python path from the frontend.
    /// Checks if the path exists and sends the result back.
    /// </summary>
    private void HandleValidatePythonPath(JsonElement root)
    {
        if (!root.TryGetProperty("path", out var pathProp))
            return;

        var pythonPath = pathProp.GetString();
        var isValid = false;
        var errorMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(pythonPath))
        {
            // Empty path is considered valid (user hasn't set one yet)
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
    /// IServer.Execute ??receives a request JSON string from cm-script,
    /// dispatches it to the frontend, and waits for the response.
    /// </summary>
    public string Execute(string requestJson)
    {
        if (string.IsNullOrEmpty(requestJson))
            return string.Empty;

        var request = Request.Deserialize(requestJson);
        if (request == null)
            return string.Empty;

        // Intercept Log/Warning/Error requests and forward as internal logs.
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
            _logToUI(level, message, "script");

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
                // Timeout ??return empty response.
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




