/**
 * launcher.js - Launch App tab logic.
 * Manages registered applications and allows launching them.
 * Apps are persisted in ~/.cosmos/launch-apps.json via the C# backend.
 */

const Launcher = {
    /** ID of the Launcher tab (if open) */
    launcherTabId: null,

    /** Registered applications: array of { name, path, arguments } */
    registeredApps: [],

    /** Current search filter text */
    searchQuery: '',

    /** Icon cache: Map<appName, base64DataUrl> */
    iconCache: new Map(),

    /**
     * Open (or focus) the Launch App tab.
     */
    openLauncherTab() {
        if (this.launcherTabId) {
            const existing = Tabs.tabs.find(t => t.id === this.launcherTabId);
            if (existing) {
                Tabs.selectTab(this.launcherTabId);
                return;
            }
            this.launcherTabId = null;
        }

        const tab = Tabs.addTab({
            title: 'Launch App',
            contentType: 'Launcher',
            icon: '🚀'
        });
        this.launcherTabId = tab.id;

        // Request the app list from the backend
        this.loadApps();
    },

    /**
     * Request the registered apps list from the C# backend.
     */
    loadApps() {
        if (!App.isWebViewReady) return;

        window.chrome.webview.postMessage(JSON.stringify({
            type: 'launcherLoadApps'
        }));
    },

    /**
     * Handle the apps loaded response from the backend.
     * @param {object} data - The message data with an apps array.
     */
    handleAppsLoaded(data) {
        this.registeredApps = data.apps || [];
        this.updateGrid();
    },

    /**
     * Handle the launch result response from the backend.
     * @param {object} data - The message data with appName, success, and message.
     */
    handleLaunchResult(data) {
        if (!data.success) {
            console.warn('Failed to launch app:', data.appName, data.message);
        }
    },

    /**
     * Handle the add app result response from the backend.
     * @param {object} data - The message data with success, message, and apps.
     */
    handleAddResult(data) {
        if (data.success) {
            this.registeredApps = data.apps || [];
            this.closeAddDialog();
            this.updateGrid();
        } else {
            this.showAddError(data.message || 'Failed to add application.');
        }
    },

    /**
     * Handle the remove app result response from the backend.
     * @param {object} data - The message data with success, message, and apps.
     */
    handleRemoveResult(data) {
        if (data.success) {
            this.registeredApps = data.apps || [];
            this.updateGrid();
        }
    },

    /**
     * Handle the browse result from the backend for selecting an executable.
     * @param {object} data - The message data with selectedPath.
     */
    handleBrowseResult(data) {
        const pathInput = document.getElementById('launcherAddPath');
        if (pathInput && data.selectedPath) {
            pathInput.value = data.selectedPath;
        }
    },

    /**
     * Handle the icon data response from the backend.
     * @param {object} data - The message data with appName and iconData (base64).
     */
    handleIconLoaded(data) {
        const appName = data.appName;
        const iconData = data.iconData;

        if (iconData) {
            this.iconCache.set(appName, 'data:image/png;base64,' + iconData);
        } else {
            this.iconCache.set(appName, null);
        }

        // Update the specific card's icon in the DOM
        const card = document.querySelector('.launcher-card[data-app-name="' + CSS.escape(appName) + '"]');
        if (card) {
            const iconEl = card.querySelector('.launcher-card-icon');
            if (iconEl) {
                if (iconData) {
                    iconEl.innerHTML = '';
                    const img = document.createElement('img');
                    img.src = 'data:image/png;base64,' + iconData;
                    img.className = 'launcher-card-icon-img';
                    img.draggable = false;
                    iconEl.appendChild(img);
                } else {
                    iconEl.textContent = '💻';
                }
            }
        }
    },

    /**
     * Render the Launch App tab panel.
     * @param {HTMLElement} container - The container element to render into.
     */
    renderLauncherPanel(container) {
        container.innerHTML = '';
        container.className = 'tab-panel active launcher-panel';

        // Toolbar
        const toolbar = document.createElement('div');
        toolbar.className = 'launcher-toolbar';

        const title = document.createElement('span');
        title.className = 'launcher-toolbar-title';
        title.textContent = 'Registered Applications';
        toolbar.appendChild(title);

        // Search input
        const searchInput = document.createElement('input');
        searchInput.type = 'text';
        searchInput.className = 'launcher-search';
        searchInput.placeholder = 'Search applications...';
        searchInput.value = this.searchQuery;
        searchInput.addEventListener('input', (e) => {
            this.searchQuery = e.target.value;
            this.renderAppGrid(grid);
        });
        toolbar.appendChild(searchInput);

        // Add button
        const addBtn = document.createElement('button');
        addBtn.className = 'btn btn-primary launcher-add-btn';
        addBtn.textContent = '+ Add App';
        addBtn.addEventListener('click', () => {
            this.showAddDialog();
        });
        toolbar.appendChild(addBtn);

        container.appendChild(toolbar);

        // App grid
        const grid = document.createElement('div');
        grid.className = 'launcher-grid';
        grid.id = 'launcherGrid';

        this.renderAppGrid(grid);
        container.appendChild(grid);
    },

    /**
     * Update the grid in the currently visible Launcher panel.
     * Uses querySelector to reliably find the panel even if other
     * cached panels (e.g. Script) exist in the content area.
     */
    updateGrid() {
        const grid = document.getElementById('launcherGrid');
        if (grid) {
            this.renderAppGrid(grid);
        }
    },

    /**
     * Render the app grid content based on the current search query.
     * @param {HTMLElement} grid - The grid container element.
     */
    renderAppGrid(grid) {
        grid.innerHTML = '';

        const filtered = this.filterApps(this.searchQuery);

        if (filtered.length === 0) {
            const empty = document.createElement('div');
            empty.className = 'launcher-empty';
            empty.textContent = this.searchQuery
                ? 'No applications match your search.'
                : 'No registered applications. Click "+ Add App" to register one.';
            grid.appendChild(empty);
            return;
        }

        filtered.forEach(app => {
            grid.appendChild(this.createAppCard(app));
        });
    },

    /**
     * Create a card element for a registered application.
     * @param {object} app - The app object { name, path, arguments }.
     * @returns {HTMLElement} The card element.
     */
    createAppCard(app) {
        const card = document.createElement('div');
        card.className = 'launcher-card';
        card.dataset.appName = app.name;
        card.title = app.path;

        // Delete button (top-right corner)
        const deleteBtn = document.createElement('button');
        deleteBtn.className = 'launcher-card-delete';
        deleteBtn.innerHTML = '&times;';
        deleteBtn.title = 'Remove application';
        deleteBtn.addEventListener('click', (e) => {
            e.stopPropagation();
            this.removeApp(app.name);
        });
        card.appendChild(deleteBtn);

        // Icon
        const icon = document.createElement('div');
        icon.className = 'launcher-card-icon';

        const cachedIcon = this.iconCache.get(app.name);
        if (cachedIcon === undefined) {
            // Not yet requested — show placeholder and request icon
            icon.textContent = '💻';
            this.requestIcon(app.name, app.path);
        } else if (cachedIcon === null) {
            // Requested but no icon available
            icon.textContent = '💻';
        } else {
            // Cached icon
            const img = document.createElement('img');
            img.src = cachedIcon;
            img.className = 'launcher-card-icon-img';
            img.draggable = false;
            icon.appendChild(img);
        }
        card.appendChild(icon);

        // Name
        const name = document.createElement('div');
        name.className = 'launcher-card-name';
        name.textContent = app.name;
        card.appendChild(name);

        // Click to launch
        card.addEventListener('click', () => {
            this.launchApp(app.name);
        });

        return card;
    },

    /**
     * Request the exe icon for an app from the backend.
     * @param {string} appName - The application name.
     * @param {string} appPath - The executable path.
     */
    requestIcon(appName, appPath) {
        if (!App.isWebViewReady) return;

        // Mark as requested so we don't request again
        this.iconCache.set(appName, null);

        window.chrome.webview.postMessage(JSON.stringify({
            type: 'launcherGetIcon',
            appName: appName,
            path: appPath
        }));
    },

    /**
     * Filter registered apps by name (case-insensitive substring match).
     * @param {string} query - The search query.
     * @returns {Array} Filtered apps.
     */
    filterApps(query) {
        if (!query || !query.trim()) {
            return this.registeredApps;
        }

        const lowerQuery = query.toLowerCase();
        return this.registeredApps.filter(app =>
            app.name.toLowerCase().includes(lowerQuery)
        );
    },

    /**
     * Launch a registered application by name.
     * @param {string} appName - The name of the app to launch.
     */
    launchApp(appName) {
        if (!App.isWebViewReady) return;

        window.chrome.webview.postMessage(JSON.stringify({
            type: 'launcherLaunchApp',
            appName: appName
        }));
    },

    /**
     * Send a remove request to the backend.
     * @param {string} appName - The name of the app to remove.
     */
    removeApp(appName) {
        if (!App.isWebViewReady) return;

        window.chrome.webview.postMessage(JSON.stringify({
            type: 'launcherRemoveApp',
            appName: appName
        }));
    },

    /**
     * Show the Add Application dialog.
     */
    showAddDialog() {
        // Remove existing dialog if any
        this.closeAddDialog();

        const overlay = document.createElement('div');
        overlay.className = 'password-dialog-overlay launcher-dialog-overlay';
        overlay.id = 'launcherAddDialog';

        const dialog = document.createElement('div');
        dialog.className = 'password-dialog password-dialog-wide';

        // Header
        const header = document.createElement('div');
        header.className = 'password-dialog-header';

        const title = document.createElement('span');
        title.className = 'password-dialog-title';
        title.textContent = 'Add Application';
        header.appendChild(title);

        const closeBtn = document.createElement('button');
        closeBtn.className = 'password-dialog-close';
        closeBtn.innerHTML = '&times;';
        closeBtn.addEventListener('click', () => this.closeAddDialog());
        header.appendChild(closeBtn);

        dialog.appendChild(header);

        // Name input
        const nameGroup = document.createElement('div');
        nameGroup.className = 'password-input-group';
        const nameLabel = document.createElement('label');
        nameLabel.textContent = 'Application Name';
        nameGroup.appendChild(nameLabel);
        const nameInput = document.createElement('input');
        nameInput.type = 'text';
        nameInput.id = 'launcherAddName';
        nameInput.placeholder = 'e.g. Notepad';
        nameGroup.appendChild(nameInput);
        dialog.appendChild(nameGroup);

        // Path input with browse button
        const pathGroup = document.createElement('div');
        pathGroup.className = 'password-input-group';
        const pathLabel = document.createElement('label');
        pathLabel.textContent = 'Executable Path';
        pathGroup.appendChild(pathLabel);
        const pathRow = document.createElement('div');
        pathRow.className = 'password-input-row';
        const pathInput = document.createElement('input');
        pathInput.type = 'text';
        pathInput.id = 'launcherAddPath';
        pathInput.placeholder = 'e.g. C:\\Windows\\notepad.exe';
        pathRow.appendChild(pathInput);
        const browseBtn = document.createElement('button');
        browseBtn.className = 'btn btn-secondary';
        browseBtn.textContent = 'Browse...';
        browseBtn.addEventListener('click', () => this.browseExecutable());
        pathRow.appendChild(browseBtn);
        pathGroup.appendChild(pathRow);
        dialog.appendChild(pathGroup);

        // Arguments input
        const argsGroup = document.createElement('div');
        argsGroup.className = 'password-input-group';
        const argsLabel = document.createElement('label');
        argsLabel.textContent = 'Arguments (optional)';
        argsGroup.appendChild(argsLabel);
        const argsInput = document.createElement('input');
        argsInput.type = 'text';
        argsInput.id = 'launcherAddArgs';
        argsInput.placeholder = 'e.g. --flag value';
        argsGroup.appendChild(argsInput);
        dialog.appendChild(argsGroup);

        // Error message
        const errorDiv = document.createElement('div');
        errorDiv.className = 'password-error';
        errorDiv.id = 'launcherAddError';
        errorDiv.style.display = 'none';
        dialog.appendChild(errorDiv);

        // Buttons
        const buttons = document.createElement('div');
        buttons.className = 'password-dialog-buttons';

        const cancelBtn = document.createElement('button');
        cancelBtn.className = 'btn btn-secondary';
        cancelBtn.textContent = 'Cancel';
        cancelBtn.addEventListener('click', () => this.closeAddDialog());
        buttons.appendChild(cancelBtn);

        const saveBtn = document.createElement('button');
        saveBtn.className = 'btn btn-primary';
        saveBtn.textContent = 'Add';
        saveBtn.addEventListener('click', () => this.submitAddDialog());
        buttons.appendChild(saveBtn);

        dialog.appendChild(buttons);
        overlay.appendChild(dialog);

        // Close on overlay click
        overlay.addEventListener('click', (e) => {
            if (e.target === overlay) this.closeAddDialog();
        });

        document.body.appendChild(overlay);

        // Focus the name input
        nameInput.focus();
    },

    /**
     * Close the Add Application dialog.
     */
    closeAddDialog() {
        const dialog = document.getElementById('launcherAddDialog');
        if (dialog) dialog.remove();
    },

    /**
     * Show an error message in the Add dialog.
     * @param {string} message - The error message.
     */
    showAddError(message) {
        const errorDiv = document.getElementById('launcherAddError');
        if (errorDiv) {
            errorDiv.textContent = message;
            errorDiv.style.display = 'block';
        }
    },

    /**
     * Submit the Add Application dialog.
     */
    submitAddDialog() {
        const nameInput = document.getElementById('launcherAddName');
        const pathInput = document.getElementById('launcherAddPath');
        const argsInput = document.getElementById('launcherAddArgs');

        const name = nameInput ? nameInput.value.trim() : '';
        const path = pathInput ? pathInput.value.trim() : '';
        const args = argsInput ? argsInput.value.trim() : '';

        if (!name) {
            this.showAddError('Application name is required.');
            return;
        }
        if (!path) {
            this.showAddError('Executable path is required.');
            return;
        }

        if (!App.isWebViewReady) return;

        window.chrome.webview.postMessage(JSON.stringify({
            type: 'launcherAddApp',
            name: name,
            path: path,
            arguments: args || null
        }));
    },

    /**
     * Request the backend to open a file browser for selecting an executable.
     */
    browseExecutable() {
        if (!App.isWebViewReady) return;

        window.chrome.webview.postMessage(JSON.stringify({
            type: 'launcherBrowseExecutable'
        }));
    }
};
