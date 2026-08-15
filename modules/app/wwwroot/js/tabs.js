/**
 * tabs.js - Tab management and drag-and-drop reordering.
 * Uses mousedown/mousemove/mouseup for custom drag with visual ghost and drop indicator.
 */

const Tabs = {
    // Tab collection
    tabs: [],
    activeTabId: null,

    // Drag state
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

    // Minimum pixel distance before drag starts
    dragThreshold: 5,

    // Scroll shadow observer and listener (initialized once)
    _scrollShadowInitialized: false,

    /**
     * Initialize the tabs module.
     */
    init() {
        this.setupGlobalDragListeners();
        this.setupWheelScroll();
    },

    /**
     * Add a new tab.
     * @param {object} tabData - Tab data (title, contentType, icon).
     * @returns {object} The created tab.
     */
    addTab(tabData) {
        const tab = {
            id: this.generateId(),
            title: tabData.title || 'Untitled',
            contentType: tabData.contentType || 'Document',
            icon: tabData.icon || null
        };

        this.tabs.push(tab);
        this.renderTabs();
        this.selectTab(tab.id);

        return tab;
    },

    /**
     * Close a tab by ID.
     * @param {string} tabId - The tab ID to close.
     */
    closeTab(tabId) {
        const index = this.tabs.findIndex(t => t.id === tabId);
        if (index === -1) return;

        const closedTab = this.tabs[index];
        this.tabs.splice(index, 1);
        this.renderTabs();

        // Clear cached Script panel if the closed tab was a Script tab
        if (closedTab.contentType === 'Script' && ScriptPlayground._cachedPanel) {
            ScriptPlayground._cachedPanel.remove();
            ScriptPlayground._cachedPanel = null;
        }

        // If the closed tab was active, select the nearest tab
        if (this.activeTabId === tabId) {
            if (this.tabs.length > 0) {
                const newIndex = Math.min(index, this.tabs.length - 1);
                this.selectTab(this.tabs[newIndex].id);
            } else {
                this.activeTabId = null;
                this.renderTabContent();
            }
        }
    },

    /**
     * Select a tab by ID.
     * @param {string} tabId - The tab ID to select.
     */
    selectTab(tabId) {
        this.activeTabId = tabId;
        this.renderTabs();
        this.renderTabContent();
    },

    /**
     * Move a tab from one index to another.
     * @param {number} fromIndex - Source index.
     * @param {number} toIndex - Target insertion index (can be tabs.length for end).
     */
    moveTab(fromIndex, toIndex) {
        if (fromIndex < 0 || fromIndex >= this.tabs.length ||
            toIndex < 0 || toIndex > this.tabs.length) {
            return;
        }

        // No-op if dropping next to itself
        if (toIndex === fromIndex || toIndex === fromIndex + 1) return;

        const [tab] = this.tabs.splice(fromIndex, 1);
        const adjustedIndex = toIndex > fromIndex ? toIndex - 1 : toIndex;
        this.tabs.splice(adjustedIndex, 0, tab);
        this.renderTabs();
    },

    /**
     * Generate a unique ID for a tab.
     * @returns {string} A unique ID.
     */
    generateId() {
        return 'tab_' + Date.now() + '_' + Math.random().toString(36).substr(2, 9);
    },

    /**
     * Render all tabs in the tab strip.
     */
    renderTabs() {
        const tabList = document.getElementById('tabList');
        tabList.innerHTML = '';

        this.tabs.forEach((tab, index) => {
            const tabElement = this.createTabElement(tab, index);
            tabList.appendChild(tabElement);
        });

        // Update scroll shadows after rendering
        this.updateScrollShadows();
    },

    /**
     * Update scroll shadow indicators for top-position tabs.
     * Shows gradient fades on left/right edges when tabs overflow.
     * Sets up listeners only once; subsequent calls just re-check overflow.
     */
    updateScrollShadows() {
        const tabList = document.getElementById('tabList');
        const wrapper = document.getElementById('tabListWrapper');
        if (!tabList || !wrapper) return;

        // Only apply scroll shadows in top position
        const tabStrip = tabList.closest('.tab-strip');
        if (tabStrip && (tabStrip.classList.contains('left') || tabStrip.classList.contains('right'))) {
            wrapper.classList.remove('scroll-left', 'scroll-right');
            return;
        }

        const checkOverflow = () => {
            const scrollLeft = tabList.scrollLeft;
            const scrollWidth = tabList.scrollWidth;
            const clientWidth = tabList.clientWidth;

            // Show left shadow if scrolled right
            if (scrollLeft > 0) {
                wrapper.classList.add('scroll-left');
            } else {
                wrapper.classList.remove('scroll-left');
            }

            // Show right shadow if more content to the right
            if (scrollLeft + clientWidth < scrollWidth - 1) {
                wrapper.classList.add('scroll-right');
            } else {
                wrapper.classList.remove('scroll-right');
            }
        };

        // Set up listeners only once
        if (!this._scrollShadowInitialized) {
            tabList.addEventListener('scroll', checkOverflow);
            window.addEventListener('resize', checkOverflow);
            this._scrollShadowInitialized = true;
        }

        // Always check immediately after render
        checkOverflow();
    },

    /**
     * Create a single tab element.
     * @param {object} tab - Tab data.
     * @param {number} index - Tab index.
     * @returns {HTMLElement} The tab element.
     */
    createTabElement(tab, index) {
        const tabElement = document.createElement('div');
        tabElement.className = 'tab-item' + (tab.id === this.activeTabId ? ' active' : '');
        tabElement.dataset.tabId = tab.id;
        tabElement.dataset.index = index;

        // Tab icon (if any)
        if (tab.icon) {
            const iconSpan = document.createElement('span');
            iconSpan.className = 'tab-icon';
            iconSpan.textContent = tab.icon;
            tabElement.appendChild(iconSpan);
        }

        // Tab title
        const titleSpan = document.createElement('span');
        titleSpan.className = 'tab-title';
        titleSpan.textContent = tab.title;
        tabElement.appendChild(titleSpan);

        // Close button
        const closeBtn = document.createElement('button');
        closeBtn.className = 'tab-close';
        closeBtn.innerHTML = '&times;';
        closeBtn.title = 'Close';
        closeBtn.addEventListener('click', (e) => {
            e.stopPropagation();
            this.closeTab(tab.id);
        });
        tabElement.appendChild(closeBtn);

        // Click to select
        tabElement.addEventListener('click', () => {
            this.selectTab(tab.id);
        });

        // Mouse down for drag
        tabElement.addEventListener('mousedown', (e) => {
            if (e.button !== 0) return;
            if (e.target.classList.contains('tab-close')) return;
            this.onMouseDown(e, index, tabElement);
        });

        return tabElement;
    },

    /**
     * Escape HTML to prevent XSS.
     * @param {string} str - The string to escape.
     * @returns {string} The escaped string.
     */
    escapeHtml(str) {
        const div = document.createElement('div');
        div.textContent = str;
        return div.innerHTML;
    },

    /**
     * Render the Start page panel with time, greeting, and quick-action cards.
     * @param {HTMLElement} panel - The container element to render into.
     */
    renderStartPanel(panel) {
        // Get username from settings (empty string if not set)
        const username = (Settings.settings && Settings.settings.username) || '';

        // Build greeting text
        const greetingText = username
            ? 'Welcome back, ' + this.escapeHtml(username)
            : 'Welcome to Cosmos';

        // Time format from StartConfig
        const timeFormat = StartConfig.config.timeFormat || '24';
        const use12h = timeFormat === '12';

        // Current time and date
        const now = new Date();
        let hours = now.getHours();
        const minutes = now.getMinutes();
        const seconds = now.getSeconds();
        let ampm = '';

        if (use12h) {
            ampm = hours >= 12 ? 'PM' : 'AM';
            hours = hours % 12 || 12;
        }

        const hh = String(hours).padStart(2, '0');
        const mm = String(minutes).padStart(2, '0');
        const ss = String(seconds).padStart(2, '0');

        // Date string
        const dateStr = now.toLocaleDateString('en-US', {
            weekday: 'long', year: 'numeric', month: 'long', day: 'numeric'
        });

        // Quick-action card definitions
        const cards = [
            { icon: '\u{1F4DC}', title: 'Log',        desc: 'View system logs',         action: 'newLog' },
            { icon: '⏰',   title: 'Scheduler',  desc: 'Manage scheduled tasks',   action: 'newScheduler' },
            { icon: '\u{1F4BB}', title: 'Script',     desc: 'Open script playground',   action: 'newScript' },
            { icon: '\u{1F511}', title: 'Passwords',  desc: 'Password manager',         action: 'newPasswordManager' },
            { icon: '🔔',   title: 'Ringtone',  desc: 'Active ringtones',        action: 'newRingtone' },
            { icon: '🚀',   title: 'Launch App', desc: 'Launch applications',    action: 'newLauncher' },
            { icon: '⚙️', title: 'Settings', desc: 'Configure application',   action: 'openSettings' },
        ];

        // Build card HTML
        const cardsHtml = cards.map(card =>
            '<div class="start-card" data-action="' + card.action + '">' +
                '<div class="start-card-icon">' + card.icon + '</div>' +
                '<div class="start-card-title">' + card.title + '</div>' +
                '<div class="start-card-desc">' + card.desc + '</div>' +
            '</div>'
        ).join('');

        // Build time secondary line (date + seconds)
        const timeSecondary = dateStr + ' · ' + ss;

        // Toggle label shows current format number
        const toggleLabel = timeFormat + 'h';

        // Assemble the full Start page
        panel.innerHTML =
            '<div class="start-page">' +
                '<div class="start-time-block">' +
                    '<div class="start-time-main">' +
                        '<span class="start-time-hhmm">' + hh + ':' + mm + '</span>' +
                        (use12h ? '<span class="start-time-ampm">' + ampm + '</span>' : '') +
                        '<span class="start-time-toggle" title="Switch format">' + toggleLabel + '</span>' +
                    '</div>' +
                    '<div class="start-time-secondary">' + timeSecondary + '</div>' +
                '</div>' +
                '<div class="start-greeting">' +
                    '<div class="start-greeting-title">' + greetingText + '</div>' +
                    '<div class="start-greeting-sub">What would you like to do?</div>' +
                '</div>' +
                '<div class="start-cards-scroll">' +
                    '<div class="start-cards">' + cardsHtml + '</div>' +
                '</div>' +
            '</div>';

        // Time format toggle click handler
        const toggleBtn = panel.querySelector('.start-time-toggle');
        if (toggleBtn) {
            toggleBtn.addEventListener('click', () => {
                const newFormat = use12h ? '24' : '12';
                StartConfig.config.timeFormat = newFormat;
                StartConfig.save();
                this.renderTabContent();
            });
        }

        // Attach click handlers to cards
        panel.querySelectorAll('.start-card').forEach(card => {
            card.addEventListener('click', () => {
                const action = card.dataset.action;
                if (action === 'openSettings') {
                    Settings.show();
                } else {
                    // Trigger the corresponding menu action
                    const menuItem = document.querySelector(
                        '.menu-dropdown-item[data-action="' + action + '"]'
                    );
                    if (menuItem) menuItem.click();
                }
            });
        });
    },

    /**
     * Render the content area for the active tab.
     * The Script panel is cached and reused (hidden via CSS class, not destroyed)
     * so that the input field preserves its value across tab switches.
     */
    renderTabContent() {
        const contentArea = document.getElementById('tabContent');
        const activeTab = this.tabs.find(t => t.id === this.activeTabId);

        // Remove non-cached panels only (preserve Script panel in DOM)
        const toRemove = [];
        for (const child of contentArea.children) {
            if (child !== ScriptPlayground._cachedPanel) {
                toRemove.push(child);
            }
        }
        toRemove.forEach(el => el.remove());

        // Hide cached Script panel when switching to a non-Script tab
        if (ScriptPlayground._cachedPanel) {
            if (activeTab && activeTab.contentType === 'Script') {
                ScriptPlayground._cachedPanel.classList.remove('script-panel-hidden');
            } else {
                ScriptPlayground._cachedPanel.classList.add('script-panel-hidden');
            }
        }

        if (!activeTab) {
            const empty = document.createElement('div');
            empty.className = 'tab-panel-start';
            empty.textContent = 'No tabs open';
            contentArea.appendChild(empty);
            return;
        }

        if (activeTab.contentType === 'Ringtone') {
            const panel = document.createElement('div');
            panel.className = 'tab-panel active ringtone-panel';
            Ringtone.renderRingtonePanel(panel);
            contentArea.appendChild(panel);
            return;
        }

        if (activeTab.contentType === 'Launcher') {
            const panel = document.createElement('div');
            panel.className = 'tab-panel active launcher-panel';
            Launcher.renderLauncherPanel(panel);
            contentArea.appendChild(panel);
            return;
        }

        if (activeTab.contentType === 'PasswordManager') {
            const panel = document.createElement('div');
            panel.className = 'tab-panel active password-panel';
            PasswordManager.renderPasswordManagerPanel(panel);
            contentArea.appendChild(panel);
            return;
        }

        if (activeTab.contentType === 'Script') {
            // Reuse cached Script panel (preserves input value)
            if (!ScriptPlayground._cachedPanel) {
                const panel = document.createElement('div');
                panel.className = 'tab-panel active script-panel';
                ScriptPlayground.renderScriptPanel(panel);
                ScriptPlayground._cachedPanel = panel;
                contentArea.appendChild(panel);
            }
            return;
        }

        const panel = document.createElement('div');
        panel.className = 'tab-panel active';

        if (activeTab.contentType === 'Document') {
            this.renderStartPanel(panel);
        } else if (activeTab.contentType === 'Settings') {
            panel.innerHTML = '<div class="tab-panel-start">Settings</div>';
        } else if (activeTab.contentType === 'Log') {
            LogStore.renderLogPanel(panel);
        } else if (activeTab.contentType === 'Scheduler') {
            Scheduler.renderSchedulerPanel(panel);
        } else {
            panel.innerHTML = '<div class="tab-panel-start">' +
                this.escapeHtml(activeTab.contentType) + '</div>';
        }

        contentArea.appendChild(panel);
    },

    /**
     * Set up global drag listeners (mousemove, mouseup).
     */
    setupGlobalDragListeners() {
        document.addEventListener('mousemove', (e) => this.onMouseMove(e));
        document.addEventListener('mouseup', (e) => this.onMouseUp(e));
    },

    /**
     * Set up horizontal wheel scrolling for top-position tabs.
     */
    setupWheelScroll() {
        const wrapper = document.getElementById('tabListWrapper');
        if (!wrapper) return;

        wrapper.addEventListener('wheel', (e) => {
            const tabList = document.getElementById('tabList');
            if (!tabList) return;

            // Only handle horizontal scrolling when tabs are at top
            const tabStrip = tabList.closest('.tab-strip');
            if (tabStrip && (tabStrip.classList.contains('left') || tabStrip.classList.contains('right'))) {
                return; // Let vertical scroll work naturally
            }

            if (Math.abs(e.deltaY) > Math.abs(e.deltaX)) {
                e.preventDefault();
                tabList.scrollLeft += e.deltaY;
            }
        }, { passive: false });
    },

    /**
     * Handle mousedown - start drag tracking.
     */
    onMouseDown(e, index, tabElement) {
        const ds = this.dragState;
        ds.dragIndex = index;
        ds.sourceElement = tabElement;
        ds.startX = e.clientX;
        ds.startY = e.clientY;
    },

    /**
     * Handle mousemove - update drag position or start drag.
     */
    onMouseMove(e) {
        const ds = this.dragState;
        if (ds.dragIndex === -1) return;

        if (!ds.isDragging) {
            // Check if we've exceeded the drag threshold
            const dx = e.clientX - ds.startX;
            const dy = e.clientY - ds.startY;
            if (Math.abs(dx) < this.dragThreshold && Math.abs(dy) < this.dragThreshold) {
                return;
            }

            // Start dragging
            ds.isDragging = true;

            // Create ghost element
            if (ds.sourceElement) {
                ds.sourceElement.classList.add('dragging');

                const rect = ds.sourceElement.getBoundingClientRect();
                ds.ghostOffsetX = ds.startX - rect.left;
                ds.ghostOffsetY = ds.startY - rect.top;

                ds.ghostElement = ds.sourceElement.cloneNode(true);
                ds.ghostElement.className = 'tab-ghost';
                ds.ghostElement.style.position = 'fixed';
                ds.ghostElement.style.left = rect.left + 'px';
                ds.ghostElement.style.top = rect.top + 'px';
                ds.ghostElement.style.width = rect.width + 'px';
                ds.ghostElement.style.pointerEvents = 'none';
                ds.ghostElement.style.zIndex = '10000';
                document.body.appendChild(ds.ghostElement);

                // Determine tab orientation: vertical bar for top tabs, horizontal bar for side tabs
                const tabStrip = ds.sourceElement.closest('.tab-strip');
                const isSide = tabStrip && (tabStrip.classList.contains('left') || tabStrip.classList.contains('right'));

                // Create drop indicator with the correct orientation class
                ds.dropIndicator = document.createElement('div');
                ds.dropIndicator.className = 'drop-indicator ' +
                    (isSide ? 'drop-indicator-horizontal' : 'drop-indicator-vertical');
                ds.dropIndicator.style.position = 'fixed';
                ds.dropIndicator.style.pointerEvents = 'none';
                ds.dropIndicator.style.zIndex = '10001';
                ds.dropIndicator.style.display = 'none';
                document.body.appendChild(ds.dropIndicator);
            }
        }

        if (ds.isDragging) {
            // Update ghost position
            if (ds.ghostElement) {
                ds.ghostElement.style.left = (e.clientX - ds.ghostOffsetX) + 'px';
                ds.ghostElement.style.top = (e.clientY - ds.ghostOffsetY) + 'px';
            }

            // Update drop indicator
            this.updateDropIndicator(e.clientX, e.clientY);
        }
    },

    /**
     * Update the drop indicator position based on cursor location.
     */
    updateDropIndicator(clientX, clientY) {
        const ds = this.dragState;
        const tabList = document.getElementById('tabList');
        if (!tabList || !ds.dropIndicator) return;

        const stripRect = tabList.getBoundingClientRect();
        const isSide = tabList.closest('.tab-strip')?.classList.contains('left') ||
                       tabList.closest('.tab-strip')?.classList.contains('right');

        // Check if cursor is outside the tab strip
        if (isSide) {
            if (clientY < stripRect.top || clientY > stripRect.bottom) {
                ds.dropIndicator.style.display = 'none';
                ds.insertIndex = -1;
                return;
            }
        } else {
            if (clientX < stripRect.left || clientX > stripRect.right) {
                ds.dropIndicator.style.display = 'none';
                ds.insertIndex = -1;
                return;
            }
        }

        // Get all visible tab elements
        const tabElements = Array.from(tabList.querySelectorAll('.tab-item'));

        if (tabElements.length === 0) {
            ds.dropIndicator.style.display = 'none';
            ds.insertIndex = -1;
            return;
        }

        let insertIndex = tabElements.length;

        if (isSide) {
            // Side tabs: determine insertion position based on cursor Y relative to tab centers
            let indicatorY = 0;

            for (let i = 0; i < tabElements.length; i++) {
                const tabRect = tabElements[i].getBoundingClientRect();
                const tabCenter = tabRect.top + tabRect.height / 2;

                if (clientY < tabCenter) {
                    insertIndex = i;
                    indicatorY = tabRect.top;
                    break;
                }
            }

            // If inserting at end, place indicator after the last tab
            if (insertIndex === tabElements.length) {
                const lastRect = tabElements[tabElements.length - 1].getBoundingClientRect();
                indicatorY = lastRect.bottom;
            }

            // Don't show indicator for no-op positions (adjacent to source)
            if (insertIndex === ds.dragIndex || insertIndex === ds.dragIndex + 1) {
                ds.dropIndicator.style.display = 'none';
                ds.insertIndex = -1;
                return;
            }

            // Show and position the horizontal blue indicator bar
            ds.insertIndex = insertIndex;
            ds.dropIndicator.style.display = 'block';
            ds.dropIndicator.style.left = stripRect.left + 'px';
            ds.dropIndicator.style.top = (indicatorY - 2) + 'px';
            ds.dropIndicator.style.width = stripRect.width + 'px';
            ds.dropIndicator.style.height = '';
        } else {
            // Top tabs: determine insertion position based on cursor X relative to tab centers
            let indicatorX = 0;

            for (let i = 0; i < tabElements.length; i++) {
                const tabRect = tabElements[i].getBoundingClientRect();
                const tabCenter = tabRect.left + tabRect.width / 2;

                if (clientX < tabCenter) {
                    insertIndex = i;
                    indicatorX = tabRect.left;
                    break;
                }
            }

            // If inserting at end, place indicator after the last tab
            if (insertIndex === tabElements.length) {
                const lastRect = tabElements[tabElements.length - 1].getBoundingClientRect();
                indicatorX = lastRect.right;
            }

            // Don't show indicator for no-op positions (adjacent to source)
            if (insertIndex === ds.dragIndex || insertIndex === ds.dragIndex + 1) {
                ds.dropIndicator.style.display = 'none';
                ds.insertIndex = -1;
                return;
            }

            // Show and position the vertical blue indicator bar
            ds.insertIndex = insertIndex;
            ds.dropIndicator.style.display = 'block';
            ds.dropIndicator.style.left = (indicatorX - 2) + 'px';
            ds.dropIndicator.style.top = stripRect.top + 'px';
            ds.dropIndicator.style.width = '';
            ds.dropIndicator.style.height = stripRect.height + 'px';
        }
    },

    /**
     * Handle mouseup - complete or cancel the drag operation.
     * @param {MouseEvent} e - The mouse event.
     */
    onMouseUp(e) {
        const ds = this.dragState;

        if (!ds.isDragging) {
            // Reset pending state even if drag never started
            ds.dragIndex = -1;
            ds.sourceElement = null;
            return;
        }

        // Perform the tab move if we have a valid insertion point
        if (ds.insertIndex >= 0) {
            this.moveTab(ds.dragIndex, ds.insertIndex);
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

        // Restore source tab opacity
        if (ds.sourceElement) {
            ds.sourceElement.classList.remove('dragging');
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
    }
};
