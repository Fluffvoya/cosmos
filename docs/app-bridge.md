# app-bridge Module

## Purpose

Defines interfaces that decouple feature modules (app-scheduler, app-settings) from the concrete WinForms/WebView2 implementation in the `app` module. Allows feature modules to communicate with the UI and script engine without direct dependencies.

## Namespace

`app_bridge`

## Interfaces

### `IScriptRunner`

```csharp
public interface IScriptRunner
{
    Task Run(string source);
}
```

Abstracts the ability to execute a cm-script source string. Implemented by `MainWindow` in the `app` module. Used by `ScheduledTaskRunner` to execute scheduled scripts.

### `IWebViewBridge`

```csharp
public interface IWebViewBridge
{
    void PostMessage(string json);
}
```

Abstracts the ability to post a JSON message to the WebView2 frontend. Implemented by `MainWindow` in the `app` module. Used by `ScheduledTaskRunner` to notify the frontend when a one-time task is auto-disabled after execution.

## Dependencies

None. This is a leaf module.
