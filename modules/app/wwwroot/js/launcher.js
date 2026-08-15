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
        this.refreshPanel();
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
     * Render the Launch App tab panel.
     * @param {HTMLElement} container - The container element to render into.
     */
    renderLauncherPanel(container) {
        container.innerHTML = '';
        container.className = 'tab-panel active launcher-panel';

        // Search bar
        const searchBar = document.createElement('div');
        searchBar.className = 'launcher-search-bar';

        const searchInput = document.createElement('input');
        searchInput.type = 'text';
        searchInput.className = 'launcher-search';
        searchInput.placeholder = 'Search applications...';
        searchInput.value = this.searchQuery;
        searchInput.addEventListener('input', (e) => {
            this.searchQuery = e.target.value;
            this.renderAppGrid(grid);
        });
        searchBar.appendChild(searchInput);

        container.appendChild(searchBar);

        // App grid
        const grid = document.createElement('div');
        grid.className = 'launcher-grid';
        grid.id = 'launcherGrid';

        this.renderAppGrid(grid);
        container.appendChild(grid);
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
                : 'No registered applications. Add apps to ~/.cosmos/launch-apps.json.';
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
        card.title = app.path;

        // Icon
        const icon = document.createElement('div');
        icon.className = 'launcher-card-icon';
        icon.textContent = '💻';
        card.appendChild(icon);

        // Name
        const name = document.createElement('div');
        name.className = 'launcher-card-name';
        name.textContent = app.name;
        card.appendChild(name);

        // Path (truncated)
        const path = document.createElement('div');
        path.className = 'launcher-card-path';
        path.textContent = app.path;
        card.appendChild(path);

        // Click to launch
        card.addEventListener('click', () => {
            this.launchApp(app.name);
        });

        return card;
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
     * Sends a message to the C# backend to start the process.
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
     * Refresh the Launcher panel if the tab is currently visible.
     */
    refreshPanel() {
        if (this.launcherTabId && Tabs.activeTabId === this.launcherTabId) {
            const contentArea = document.getElementById('tabContent');
            if (contentArea && contentArea.firstChild) {
                this.renderLauncherPanel(contentArea.firstChild);
            }
        }
    }
};
