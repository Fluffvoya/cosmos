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

    /** Drag-and-drop state for card reordering */
    dragState: {
        isDragging: false,
        dragIndex: -1,
        startX: 0,
        startY: 0,
        ghostElement: null,
        dropIndicator: null,
        insertIndex: -1,
        sourceElement: null,
        ghostOffsetX: 0,
        ghostOffsetY: 0
    },

    /** Minimum pixel distance before drag starts */
    dragThreshold: 5,

    /**
     * Initialize the Launcher module — set up global drag listeners once.
     */
    init() {
        document.addEventListener('mousemove', (e) => this.onDragMouseMove(e));
        document.addEventListener('mouseup', (e) => this.onDragMouseUp(e));
    },

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
     * Handle the reorder result response from the backend.
     * @param {object} data - The message data with success, message, and apps.
     */
    handleReorderResult(data) {
        if (data.success) {
            this.registeredApps = data.apps || [];
            this.updateGrid();
        } else {
            console.warn('Failed to reorder apps:', data.message);
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

        filtered.forEach((app, index) => {
            grid.appendChild(this.createAppCard(app, index));
        });
    },

    /**
     * Create a card element for a registered application.
     * @param {object} app - The app object { name, path, arguments }.
     * @param {number} index - The index of the app in the filtered list.
     * @returns {HTMLElement} The card element.
     */
    createAppCard(app, index) {
        const card = document.createElement('div');
        card.className = 'launcher-card';
        card.dataset.appName = app.name;
        card.dataset.appIndex = index;
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

        // Click to launch (only fires if drag did not occur)
        card.addEventListener('click', (e) => {
            if (this.dragState.isDragging || this.dragState.dragIndex >= 0) return;
            this.launchApp(app.name);
        });

        // Mousedown to initiate drag
        card.addEventListener('mousedown', (e) => {
            if (e.button !== 0) return;
            // Don't start drag from delete button
            if (e.target.classList.contains('launcher-card-delete')) return;
            this.onDragMouseDown(e, index, card);
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
    },

    // ── Drag-and-drop reordering ──────────────────────────────────

    /**
     * Handle mousedown on a launcher card — begin tracking potential drag.
     * @param {MouseEvent} e - The mouse event.
     * @param {number} index - The card index in the filtered list.
     * @param {HTMLElement} cardElement - The card DOM element.
     */
    onDragMouseDown(e, index, cardElement) {
        const ds = this.dragState;
        ds.dragIndex = index;
        ds.sourceElement = cardElement;
        ds.startX = e.clientX;
        ds.startY = e.clientY;
    },

    /**
     * Handle mousemove — start drag once threshold is exceeded, then update ghost.
     * @param {MouseEvent} e - The mouse event.
     */
    onDragMouseMove(e) {
        const ds = this.dragState;
        if (ds.dragIndex === -1) return;

        if (!ds.isDragging) {
            // Check if the pointer has moved past the drag threshold
            const dx = e.clientX - ds.startX;
            const dy = e.clientY - ds.startY;
            if (Math.abs(dx) < this.dragThreshold && Math.abs(dy) < this.dragThreshold) {
                return;
            }

            // Start dragging
            ds.isDragging = true;

            // Dim the source card
            if (ds.sourceElement) {
                ds.sourceElement.classList.add('launcher-card-dragging');

                const rect = ds.sourceElement.getBoundingClientRect();
                ds.ghostOffsetX = ds.startX - rect.left;
                ds.ghostOffsetY = ds.startY - rect.top;

                // Create a floating ghost clone
                ds.ghostElement = ds.sourceElement.cloneNode(true);
                ds.ghostElement.className = 'launcher-card-ghost';
                ds.ghostElement.style.position = 'fixed';
                ds.ghostElement.style.left = rect.left + 'px';
                ds.ghostElement.style.top = rect.top + 'px';
                ds.ghostElement.style.width = rect.width + 'px';
                ds.ghostElement.style.height = rect.height + 'px';
                ds.ghostElement.style.pointerEvents = 'none';
                ds.ghostElement.style.zIndex = '10000';
                document.body.appendChild(ds.ghostElement);

                // Create drop indicator (vertical bar)
                ds.dropIndicator = document.createElement('div');
                ds.dropIndicator.className = 'launcher-drop-indicator';
                ds.dropIndicator.style.position = 'fixed';
                ds.dropIndicator.style.pointerEvents = 'none';
                ds.dropIndicator.style.zIndex = '10001';
                ds.dropIndicator.style.display = 'none';
                document.body.appendChild(ds.dropIndicator);
            }
        }

        if (ds.isDragging) {
            // Move the ghost to follow the cursor
            if (ds.ghostElement) {
                ds.ghostElement.style.left = (e.clientX - ds.ghostOffsetX) + 'px';
                ds.ghostElement.style.top = (e.clientY - ds.ghostOffsetY) + 'px';
            }
            this.updateDropIndicator(e.clientX, e.clientY);
        }
    },

    /**
     * Update the drop indicator position based on cursor location over the grid.
     * @param {number} clientX - Cursor X in viewport.
     * @param {number} clientY - Cursor Y in viewport.
     */
    updateDropIndicator(clientX, clientY) {
        const ds = this.dragState;
        const grid = document.getElementById('launcherGrid');
        if (!grid || !ds.dropIndicator) return;

        const gridRect = grid.getBoundingClientRect();

        // Hide indicator if cursor is outside the grid
        if (clientX < gridRect.left || clientX > gridRect.right ||
            clientY < gridRect.top || clientY > gridRect.bottom) {
            ds.dropIndicator.style.display = 'none';
            ds.insertIndex = -1;
            return;
        }

        // Get all visible card elements
        const cards = Array.from(grid.querySelectorAll('.launcher-card'));
        if (cards.length === 0) {
            ds.dropIndicator.style.display = 'none';
            ds.insertIndex = -1;
            return;
        }

        // Find the card the cursor is closest to and determine left/right insertion
        let insertIndex = cards.length;
        let indicatorX = 0;
        let indicatorY = 0;
        let indicatorHeight = 0;

        for (let i = 0; i < cards.length; i++) {
            const rect = cards[i].getBoundingClientRect();
            const centerX = rect.left + rect.width / 2;
            const centerY = rect.top + rect.height / 2;

            // Check if cursor is within the row's vertical range
            if (clientY >= rect.top && clientY <= rect.bottom) {
                if (clientX < centerX) {
                    insertIndex = i;
                    indicatorX = rect.left;
                    indicatorY = rect.top;
                    indicatorHeight = rect.height;
                    break;
                } else if (i === cards.length - 1 ||
                    !(clientY >= cards[i + 1]?.getBoundingClientRect().top &&
                      clientY <= cards[i + 1]?.getBoundingClientRect().bottom)) {
                    // Cursor is in the right half of the last card in this row
                    insertIndex = i + 1;
                    indicatorX = rect.right;
                    indicatorY = rect.top;
                    indicatorHeight = rect.height;
                    break;
                }
            }
        }

        // If cursor is below all cards, insert at end
        if (insertIndex === cards.length) {
            const lastRect = cards[cards.length - 1].getBoundingClientRect();
            indicatorX = lastRect.right;
            indicatorY = lastRect.top;
            indicatorHeight = lastRect.height;
        }

        // Don't show indicator when dropping next to the source card
        if (insertIndex === ds.dragIndex || insertIndex === ds.dragIndex + 1) {
            ds.dropIndicator.style.display = 'none';
            ds.insertIndex = -1;
            return;
        }

        // Show and position the vertical drop indicator bar
        ds.insertIndex = insertIndex;
        ds.dropIndicator.style.display = 'block';
        ds.dropIndicator.style.left = (indicatorX - 2) + 'px';
        ds.dropIndicator.style.top = indicatorY + 'px';
        ds.dropIndicator.style.height = indicatorHeight + 'px';
    },

    /**
     * Handle mouseup — complete or cancel the drag operation.
     * @param {MouseEvent} e - The mouse event.
     */
    onDragMouseUp(e) {
        const ds = this.dragState;

        if (!ds.isDragging) {
            // Reset pending state even if drag never started
            ds.dragIndex = -1;
            ds.sourceElement = null;
            return;
        }

        // Perform the reorder if we have a valid insertion point
        if (ds.insertIndex >= 0) {
            this.sendReorder(ds.dragIndex, ds.insertIndex);
        }

        this.cleanupDrag();
    },

    /**
     * Clean up all drag-related DOM elements and reset state.
     */
    cleanupDrag() {
        const ds = this.dragState;

        // Remove ghost element
        if (ds.ghostElement) {
            ds.ghostElement.remove();
            ds.ghostElement = null;
        }

        // Remove drop indicator
        if (ds.dropIndicator) {
            ds.dropIndicator.remove();
            ds.dropIndicator = null;
        }

        // Restore source card opacity
        if (ds.sourceElement) {
            ds.sourceElement.classList.remove('launcher-card-dragging');
            ds.sourceElement = null;
        }

        // Reset all drag state
        ds.isDragging = false;
        ds.dragIndex = -1;
        ds.insertIndex = -1;
        ds.startX = 0;
        ds.startY = 0;
        ds.ghostOffsetX = 0;
        ds.ghostOffsetY = 0;
    },

    /**
     * Send a reorder request to the C# backend.
     * @param {number} fromIndex - Source index in the filtered list.
     * @param {number} toIndex - Target insertion index.
     */
    sendReorder(fromIndex, toIndex) {
        if (!App.isWebViewReady) return;

        // Map filtered indices to the actual indices in registeredApps
        const filtered = this.filterApps(this.searchQuery);
        if (fromIndex < 0 || fromIndex >= filtered.length || toIndex < 0 || toIndex > filtered.length) return;

        const fromApp = filtered[fromIndex];
        const realFromIndex = this.registeredApps.indexOf(fromApp);

        // Compute the real target index based on the app that would be at the insertion point
        let realToIndex;
        if (toIndex >= filtered.length) {
            // Inserting at the end — find the real index of the last filtered app and add 1
            const lastApp = filtered[filtered.length - 1];
            realToIndex = this.registeredApps.indexOf(lastApp) + 1;
        } else {
            const toApp = filtered[toIndex];
            realToIndex = this.registeredApps.indexOf(toApp);
        }

        if (realFromIndex < 0 || realToIndex < 0) return;
        // Skip no-op moves
        if (realFromIndex === realToIndex || realFromIndex + 1 === realToIndex) return;

        window.chrome.webview.postMessage(JSON.stringify({
            type: 'launcherReorderApps',
            fromIndex: realFromIndex,
            toIndex: realToIndex
        }));
    }
};
