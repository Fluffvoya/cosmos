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

        // Add default welcome tab
        Tabs.addTab({
            title: 'Welcome',
            contentType: 'Document',
            icon: null
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
                LogStore.addEntry(data.level || 'Error', 'program', data.message || '');
                break;
            case 'settingsLoaded':
                Settings.loadSettings(data.settings);
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

        // Client messages arrive via IServer.Execute – mark as user info.
        LogStore.addEntry('Info', 'user', content);

        // Respond immediately
        this.sendResponse(requestId, '');
    },

    /**
     * Handle GetUserName request.
     * @param {string} requestId - The request correlation ID.
     */
    handleGetUserName(requestId) {
        // Return a placeholder username
        this.sendResponse(requestId, 'User');
    },

    /**
     * Send a response back to the C# backend.
     * @param {string} requestId - The request correlation ID.
     * @param {string} data - The response data.
     */
    sendResponse(requestId, data) {
        if (this.isWebViewReady) {
            window.chrome.webview.postMessage(JSON.stringify({
                type: 'response',
                requestId: requestId,
                data: data
            }));
        }
    },

    /**
     * Send settings changed notification to the backend.
     * @param {object} settings - The new settings.
     */
    sendSettingsChanged(settings) {
        if (this.isWebViewReady) {
            window.chrome.webview.postMessage(JSON.stringify({
                type: 'settingsChanged',
                settings: settings
            }));
        }
    },

    /**
     * Set up menu item handlers.
     */
    setupMenuHandlers() {
        // Menu items with dropdowns
        const menuItems = document.querySelectorAll('.menu-item[data-menu]');
        menuItems.forEach(item => {
            item.addEventListener('click', (e) => {
                const menu = item.dataset.menu;
                
                // Handle Settings menu specially - open settings panel
                if (menu === 'settings') {
                    Settings.show();
                    return;
                }
                
                // Toggle dropdown for other items
                const dropdown = item.querySelector('.menu-dropdown');
                if (dropdown) {
                    // Close other dropdowns
                    document.querySelectorAll('.menu-dropdown').forEach(d => {
                        if (d !== dropdown) d.style.display = 'none';
                    });
                    
                    // Toggle this dropdown
                    dropdown.style.display = dropdown.style.display === 'block' ? 'none' : 'block';
                }
            });
        });

        // Menu dropdown items
        const dropdownItems = document.querySelectorAll('.menu-dropdown-item');
        dropdownItems.forEach(item => {
            item.addEventListener('click', (e) => {
                e.stopPropagation();
                const action = item.dataset.action;
                
                // Close the dropdown
                const dropdown = item.closest('.menu-dropdown');
                if (dropdown) dropdown.style.display = 'none';
                
                // Handle the action
                this.handleMenuAction(action);
            });
        });

        // Close dropdowns when clicking outside
        document.addEventListener('click', (e) => {
            if (!e.target.closest('.menu-item')) {
                document.querySelectorAll('.menu-dropdown').forEach(d => {
                    d.style.display = 'none';
                });
            }
        });
    },

    /**
     * Handle a menu action.
     * @param {string} action - The action to perform.
     */
    handleMenuAction(action) {
        switch (action) {
            case 'newWelcome':
                Tabs.addTab({
                    title: 'Welcome',
                    contentType: 'Document',
                    icon: null
                });
                break;
            case 'newLog':
                Tabs.addTab({
                    title: 'Log',
                    contentType: 'Log',
                    icon: null
                });
                break;
            case 'about':
                this.showAbout();
                break;
        }
    },

    /**
     * Set up modal dialog handlers.
     */
    setupModalHandlers() {
        const modalClose = document.getElementById('modalClose');
        const modalOk = document.getElementById('modalOk');
        const modalOverlay = document.getElementById('modalOverlay');

        if (modalClose) {
            modalClose.addEventListener('click', () => this.closeModal());
        }

        if (modalOk) {
            modalOk.addEventListener('click', () => this.closeModal());
        }

        if (modalOverlay) {
            modalOverlay.addEventListener('click', (e) => {
                if (e.target === modalOverlay) {
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
        overlay.style.display = 'none';

        // Send response if this was a ShowWindow request
        const requestId = overlay.dataset.requestId;
        if (requestId) {
            this.sendResponse(requestId, 'ok');
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

// Initialize when DOM is ready
document.addEventListener('DOMContentLoaded', () => {
    App.init();
});
