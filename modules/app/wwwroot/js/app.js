/**
 * app.js - Main application logic and WebView2 message bridge.
 * Handles communication between the frontend and C# backend.
 */

// Global app state
const App = {
    // WebView2 message bridge
    isWebViewReady: false,
    pendingCallbacks: {},

    /**
     * Initialize the application.
     */
    init() {
        this.setupWebViewBridge();
        this.setupMenuHandlers();
        this.setupModalHandlers();
        this.setupWindowControls();

        // Initialize tabs module
        Tabs.init();

        // Initialize settings module
        Settings.init();

        // Initialize splitter module
        Splitter.init();

        // Initialize launcher drag-and-drop
        Launcher.init();

        // Add default start tab
        Tabs.addTab({
            title: 'Start',
            contentType: 'Document',
            icon: '🏠'
        });
    },

    /**
     * Set up the WebView2 message bridge.
     */
    setupWebViewBridge() {
        if (window.chrome && window.chrome.webview) {
            // WebView2 is available
            window.chrome.webview.addEventListener('message', (event) => {
                this.handleBackendMessage(event.data);
            });
            this.isWebViewReady = true;
            console.log('WebView2 bridge initialized');
        } else {
            // Running in a regular browser (for development)
            console.warn('WebView2 not available - running in standalone mode');
            this.isWebViewReady = false;
        }
    },

    /**
     * Handle a message from the C# backend.
     * @param {string|object} message - The message from the backend.
     */
    handleBackendMessage(message) {
        let data;
        if (typeof message === 'string') {
            try {
                data = JSON.parse(message);
            } catch (e) {
                console.error('Failed to parse backend message:', e);
                return;
            }
        } else {
            data = message;
        }

        console.log('Backend message:', data);

        switch (data.type) {
            case 'request':
                this.handleBackendRequest(data);
                break;
            case 'internalLog':
                LogStore.addEntry(data.level || 'Error', data.sender || 'program', data.message || '');
                break;
            case 'settingsLoaded':
                Settings.loadSettings(data.settings);
                if (Array.isArray(data.scheduledTasks)) {
                    Scheduler.loadTasks(data.scheduledTasks);
                }
                if (Array.isArray(data.scriptOutput)) {
                    ScriptPlayground.loadFromSettings(data.scriptOutput);
                }
                if (data.startConfig) {
                    StartConfig.load(data.startConfig);
                }
                break;
            case 'pythonPathValidation':
                Settings.handleValidationResponse(data);
                break;
            case 'browseResult':
                Settings.handleBrowseResult(data);
                break;
            case 'startupScriptPathValidation':
                Settings.handleStartupScriptPathValidationResponse(data);
                break;
            case 'startupScriptBrowseResult':
                Settings.handleStartupScriptBrowseResult(data);
                break;
            case 'schedulerRunResult':
                Scheduler.handleRunResult(data);
                break;
            case 'schedulerBrowseResult':
                Scheduler.handleBrowseResult(data);
                break;
            case 'schedulerTaskAutoDisabled':
                Scheduler.handleAutoDisabled(data);
                break;
            case 'scriptRunResult':
                ScriptPlayground.handleRunResult(data);
                break;
            case 'scriptLog':
                ScriptPlayground.handleScriptLog(data);
                break;
            case 'messageBar':
                MessageBar.show(data.message, data.level);
                break;
            case 'passwordManagerSetupCheck':
                PasswordManager.handleSetupCheck(data.isSetup);
                break;
            case 'passwordManagerSetupSuccess':
                PasswordManager.handleSetupSuccess();
                break;
            case 'passwordManagerAuthSuccess':
                PasswordManager.handleAuthSuccess(data.platforms);
                break;
            case 'passwordManagerAuthFailure':
                PasswordManager.handleAuthFailure(data.message);
                break;
            case 'passwordManagerChangePasswordSuccess':
                PasswordManager.handleChangePasswordSuccess();
                break;
            case 'passwordManagerChangePasswordFailure':
                PasswordManager.handleChangePasswordFailure(data.message);
                break;
            case 'ringtonePlay':
                Ringtone.handlePlayRingtone(data);
                break;
            case 'launcherAppsLoaded':
                Launcher.handleAppsLoaded(data);
                break;
            case 'launcherLaunchResult':
                Launcher.handleLaunchResult(data);
                break;
            case 'launcherAddResult':
                Launcher.handleAddResult(data);
                break;
            case 'launcherEditResult':
                Launcher.handleEditResult(data);
                break;
            case 'launcherRemoveResult':
                Launcher.handleRemoveResult(data);
                break;
            case 'launcherBrowseResult':
                Launcher.handleBrowseResult(data);
                break;
            case 'launcherIconLoaded':
                Launcher.handleIconLoaded(data);
                break;
            case 'launcherReorderResult':
                Launcher.handleReorderResult(data);
                break;
            case 'launcherCategoriesLoaded':
                Launcher.handleCategoriesLoaded(data);
                break;
            default:
                console.warn('Unknown backend message type:', data.type);
        }
    },

    /**
     * Handle a request from the C# backend (from cm-script via IServer).
     * @param {object} data - The request data with requestId, requestName, and args.
     */
    handleBackendRequest(data) {
        const { requestId, requestName, args } = data;

        switch (requestName) {
            case 'ShowWindow':
                this.handleShowWindow(requestId, args);
                break;
            case 'Log':
                this.handleLog(requestId, args);
                break;
            case 'GetUserName':
                this.handleGetUserName(requestId);
                break;
            default:
                console.warn('Unknown request:', requestName);
                this.sendResponse(requestId, '');
                break;
        }
    },

    /**
     * Handle ShowWindow request - show a modal dialog.
     * @param {string} requestId - The request correlation ID.
     * @param {string[]} args - [name, message]
     */
    handleShowWindow(requestId, args) {
        const name = args && args.length > 0 ? args[0] : 'Message';
        const message = args && args.length > 1 ? args[1] : '';

        // Show the modal dialog
        const overlay = document.getElementById('modalOverlay');
        const title = document.getElementById('modalTitle');
        const body = document.getElementById('modalMessage');

        title.textContent = name;
        body.textContent = message;
        overlay.style.display = 'flex';

        // Store the requestId so we can respond when the dialog is closed
        overlay.dataset.requestId = requestId;
    },

    /**
     * Handle Log request - parse and store a log entry.
     * The client only sends content; level and sender are inferred here.
     * @param {string} requestId - The request correlation ID.
     * @param {string[]} args - [content]
     */
    handleLog(requestId, args) {
        const content = args && args.length > 0 ? args[0] : '';

        // Parse level and sender from the content (if formatted as "[level] message" or "[level][sender] message")
        let level = 'Info';
        let sender = 'script';
        let message = content;

        const match = content.match(/^\[(\w+)\](?:\[(\w+)\])?\s*(.*)/);
        if (match) {
            level = match[1] || 'Info';
            sender = match[2] || 'script';
            message = match[3] || content;
        }

        LogStore.addEntry(level, sender, message);

        // Respond to the backend
        this.sendResponse(requestId, '');
    },

    /**
     * Handle GetUserName request - show a prompt for the user's name.
     * @param {string} requestId - The request correlation ID.
     */
    handleGetUserName(requestId) {
        // For now, return a default name
        // In a real app, this would show an input dialog
        this.sendResponse(requestId, 'User');
    },

    /**
     * Send a response back to the C# backend.
     * @param {string} requestId - The request correlation ID.
     * @param {string} data - The response data.
     */
    sendResponse(requestId, data) {
        if (!this.isWebViewReady) return;

        const message = {
            type: 'response',
            requestId: requestId,
            data: data
        };

        window.chrome.webview.postMessage(JSON.stringify(message));
    },

    /**
     * Send settings changed notification to the C# backend.
     * @param {object} settings - The new settings.
     */
    sendSettingsChanged(settings) {
        if (!this.isWebViewReady) return;

        const message = {
            type: 'settingsChanged',
            settings: settings
        };

        window.chrome.webview.postMessage(JSON.stringify(message));
    },

    /**
     * Send start config changed notification to the C# backend.
     * @param {object} config - The new start config.
     */
    sendStartConfigChanged(config) {
        if (!this.isWebViewReady) return;

        const message = {
            type: 'startConfigChanged',
            config: config
        };

        window.chrome.webview.postMessage(JSON.stringify(message));
    },

    /**
     * Set up menu handlers.
     */
    setupMenuHandlers() {
        const menuItems = document.querySelectorAll('.menu-dropdown-item[data-action]');
        menuItems.forEach(item => {
            item.addEventListener('click', () => {
                const action = item.dataset.action;
                switch (action) {
                    case 'newStart':
                        Tabs.addTab({
                            title: 'Start',
                            contentType: 'Document',
                            icon: '🏠'
                        });
                        break;
                    case 'newLog':
                        LogStore.openLogTab();
                        break;
                    case 'newScheduler':
                        Scheduler.openSchedulerTab();
                        break;
                    case 'newScript':
                        ScriptPlayground.openScriptTab();
                        break;
                    case 'newPasswordManager':
                        PasswordManager.openPasswordManagerTab();
                        break;
                    case 'newRingtone':
                        Ringtone.openRingtoneTab();
                        break;
                    case 'newLauncher':
                        Launcher.openLauncherTab();
                        break;
                    case 'about':
                        this.showAbout();
                        break;
                }
            });
        });

        // Settings menu item (no dropdown, opens settings overlay)
        const settingsMenuItem = document.querySelector('.menu-item[data-menu="settings"]');
        if (settingsMenuItem) {
            settingsMenuItem.addEventListener('click', () => {
                Settings.show();
            });
        }
    },

    /**
     * Set up modal dialog handlers.
     */
    setupModalHandlers() {
        const overlay = document.getElementById('modalOverlay');
        const closeBtn = document.getElementById('modalClose');
        const okBtn = document.getElementById('modalOk');

        if (closeBtn) {
            closeBtn.addEventListener('click', () => this.closeModal());
        }

        if (okBtn) {
            okBtn.addEventListener('click', () => this.closeModal());
        }

        // Close on overlay click
        if (overlay) {
            overlay.addEventListener('click', (e) => {
                if (e.target === overlay) {
                    this.closeModal();
                }
            });
        }
    },

    /**
     * Close the modal dialog and send response if needed.
     */
    closeModal() {
        const overlay = document.getElementById('modalOverlay');
        const requestId = overlay.dataset.requestId;

        overlay.style.display = 'none';

        // If this modal was from a ShowWindow request, send an empty response
        if (requestId) {
            this.sendResponse(requestId, '');
            overlay.dataset.requestId = '';
        }
    },

    /**
     * Set up window control handlers.
     */
    setupWindowControls() {
        const webview = window.chrome?.webview;

        // ── Window control buttons ───────────────────────────────
        const btnMinimize = document.getElementById('btnMinimize');
        const btnMaximize = document.getElementById('btnMaximize');
        const btnClose = document.getElementById('btnClose');

        if (btnMinimize) {
            btnMinimize.addEventListener('click', () => {
                webview?.postMessage('window:minimize');
            });
        }

        if (btnMaximize) {
            btnMaximize.addEventListener('click', () => {
                webview?.postMessage('window:maximize');
            });
        }

        if (btnClose) {
            btnClose.addEventListener('click', () => {
                webview?.postMessage('window:close');
            });
        }

        // ── Drag area (menu bar spacer & menu bar background) ────
        // Delay native drag until mouse actually moves, so that
        // double-click (two rapid mousedowns without movement) fires
        // the native dblclick event and toggles maximize.
        const DRAG_THRESHOLD = 3;

        function startDragDetection(e) {
            if (e.button !== 0) return;
            const startX = e.screenX, startY = e.screenY;
            let dragging = false;

            const onMove = (ev) => {
                if (dragging) return;
                if (Math.abs(ev.screenX - startX) > DRAG_THRESHOLD ||
                    Math.abs(ev.screenY - startY) > DRAG_THRESHOLD) {
                    dragging = true;
                    cleanup();
                    webview?.postMessage('window:drag');
                }
            };
            const onUp = () => cleanup();
            const cleanup = () => {
                document.removeEventListener('mousemove', onMove);
                document.removeEventListener('mouseup', onUp);
            };
            document.addEventListener('mousemove', onMove);
            document.addEventListener('mouseup', onUp);
        }

        const dragSpacers = document.querySelectorAll('.menu-drag-spacer');
        dragSpacers.forEach(spacer => {
            spacer.addEventListener('mousedown', startDragDetection);
            // Double-click to toggle maximize (like native title bar)
            spacer.addEventListener('dblclick', (e) => {
                if (e.button === 0) {
                    webview?.postMessage('window:dblclick-maximize');
                }
            });
        });

        // Also allow dragging from the menu bar background (padding areas)
        const menuBar = document.querySelector('.menu-bar');
        if (menuBar) {
            menuBar.addEventListener('mousedown', (e) => {
                if (e.target === menuBar) startDragDetection(e);
            });
            // Double-click on menu bar background to toggle maximize
            menuBar.addEventListener('dblclick', (e) => {
                if (e.target === menuBar && e.button === 0) {
                    webview?.postMessage('window:dblclick-maximize');
                }
            });
        }

        // ── Window state change – update maximize icon ───────────
        if (webview) {
            webview.addEventListener('message', (event) => {
                if (event.data?.type === 'windowStateChanged') {
                    this.updateMaximizeIcon(event.data.maximized);
                }
            });
        }
    },

    /**
     * Update the maximize button icon between maximize and restore states.
     * @param {boolean} maximized - Whether the window is currently maximized.
     */
    updateMaximizeIcon(maximized) {
        const btn = document.getElementById('btnMaximize');
        if (!btn) return;
        const iconMax = btn.querySelector('.icon-maximize');
        const iconRestore = btn.querySelector('.icon-restore');
        if (iconMax && iconRestore) {
            iconMax.style.display = maximized ? 'none' : '';
            iconRestore.style.display = maximized ? '' : 'none';
        }
        btn.title = maximized ? 'Restore' : 'Maximize';
    },

    /**
     * Show the About dialog.
     */
    showAbout() {
        const overlay = document.getElementById('modalOverlay');
        const title = document.getElementById('modalTitle');
        const body = document.getElementById('modalMessage');

        title.textContent = 'About';
        body.textContent = 'Cosmos Application\nVersion 1.0';
        overlay.style.display = 'flex';

        // No requestId needed for About dialog (not from script)
        overlay.dataset.requestId = '';
    }
};

/**
 * StartConfig - Start page configuration (time format, etc.).
 * Persisted separately from main settings in start-config.json.
 */
const StartConfig = {
    config: {
        timeFormat: '24',
    },

    /**
     * Load start config from the backend.
     * @param {object} config - The config to load.
     */
    load(config) {
        if (config) {
            this.config = { ...this.config, ...config };
        }
    },

    /**
     * Save start config to the backend.
     */
    save() {
        App.sendStartConfigChanged(this.config);
    },
};

/**
 * MessageBar - Non-blocking toast notification system.
 * Displays temporary messages at the top-center of the window.
 */
const MessageBar = {
    /** Duration in ms before a toast auto-dismisses. */
    DISPLAY_DURATION: 10000,

    /**
     * Show a toast notification.
     * @param {string} message - The message text to display.
     * @param {string} level - Severity level: "info", "warning", or "error".
     */
    show(message, level) {
        const container = document.getElementById('messageBarContainer');
        if (!container) return;

        const toast = document.createElement('div');
        toast.className = `message-bar-toast message-bar-${level || 'info'}`;

        const text = document.createElement('span');
        text.className = 'message-bar-text';
        text.textContent = message || '';

        const closeBtn = document.createElement('button');
        closeBtn.className = 'message-bar-close';
        closeBtn.innerHTML = '&times;';
        closeBtn.addEventListener('click', () => this.dismiss(toast));

        toast.appendChild(text);
        toast.appendChild(closeBtn);
        container.appendChild(toast);

        // Trigger reflow before adding the visible class for the CSS transition
        toast.offsetHeight;
        toast.classList.add('message-bar-visible');

        // Auto-dismiss after the configured duration
        setTimeout(() => this.dismiss(toast), this.DISPLAY_DURATION);
    },

    /**
     * Dismiss a toast with a fade-out animation, then remove from DOM.
     * @param {HTMLElement} toast - The toast element to dismiss.
     */
    dismiss(toast) {
        if (!toast || toast.classList.contains('message-bar-dismissed')) return;
        toast.classList.add('message-bar-dismissed');
        toast.addEventListener('transitionend', () => toast.remove(), { once: true });
    }
};

// Initialize when DOM is ready
document.addEventListener('DOMContentLoaded', () => {
    App.init();
});

