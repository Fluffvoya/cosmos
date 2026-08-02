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

        this.tabs.splice(index, 1);
        this.renderTabs();

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
            this._scrollShadowInitialized = true;

            tabList.addEventListener('scroll', checkOverflow);

            const resizeObserver = new ResizeObserver(checkOverflow);
            resizeObserver.observe(tabList);
        }

        // Always re-check overflow state
        checkOverflow();
    },

    /**
     * Create a tab element.
     * @param {object} tab - The tab data.
     * @param {number} index - The tab index.
     * @returns {HTMLElement} The tab element.
     */
    createTabElement(tab, index) {
        const div = document.createElement('div');
        div.className = 'tab-item' + (tab.id === this.activeTabId ? ' active' : '');
        div.dataset.tabId = tab.id;
        div.dataset.index = index;

        // Icon (if provided)
        if (tab.icon) {
            const icon = document.createElement('span');
            icon.className = 'tab-icon';
            icon.textContent = tab.icon;
            div.appendChild(icon);
        }

        // Title
        const title = document.createElement('span');
        title.className = 'tab-title';
        title.textContent = tab.title;
        div.appendChild(title);

        // Close button
        const closeBtn = document.createElement('button');
        closeBtn.className = 'tab-close';
        closeBtn.textContent = '✕';
        closeBtn.addEventListener('click', (e) => {
            e.stopPropagation();
            this.closeTab(tab.id);
        });
        div.appendChild(closeBtn);

        // Click to select (only when not dragging)
        div.addEventListener('click', () => {
            if (!this.dragState.isDragging) {
                this.selectTab(tab.id);
            }
        });

        // Mousedown to start potential drag
        div.addEventListener('mousedown', (e) => this.onMouseDown(e, index));

        return div;
    },

    /**
     * Render the content area for the active tab.
     */
    renderTabContent() {
        const contentArea = document.getElementById('tabContent');
        contentArea.innerHTML = '';

        const activeTab = this.tabs.find(t => t.id === this.activeTabId);
        if (!activeTab) {
            // No tabs - show empty state
            contentArea.innerHTML = '<div class="tab-panel-welcome">No tabs open</div>';
            return;
        }

        const panel = document.createElement('div');
        panel.className = 'tab-panel active';

        if (activeTab.contentType === 'Document') {
            panel.innerHTML = '<div class="tab-panel-welcome">' +
                this.escapeHtml(activeTab.title) + '</div>';
        } else if (activeTab.contentType === 'Settings') {
            // Settings is handled by the settings overlay
            panel.innerHTML = '<div class="tab-panel-welcome">Settings</div>';
        } else if (activeTab.contentType === 'Log') {
            // Log panel is rendered by LogStore
            LogStore.renderLogPanel(panel);
        } else {
            panel.innerHTML = '<div class="tab-panel-welcome">' +
                this.escapeHtml(activeTab.contentType) + '</div>';
        }

        contentArea.appendChild(panel);
    },

    /**
     * Escape HTML special characters.
     * @param {string} text - The text to escape.
     * @returns {string} The escaped text.
     */
    escapeHtml(text) {
        const div = document.createElement('div');
        div.textContent = text;
        return div.innerHTML;
    },

    // ── Drag & Drop ─────────────────────────────────────────────

    /**
     * Set up global mousemove and mouseup listeners for drag.
     */
    setupGlobalDragListeners() {
        document.addEventListener('mousemove', (e) => this.onMouseMove(e));
        document.addEventListener('mouseup', (e) => this.onMouseUp(e));
    },

    /**
     * Set up mouse wheel scrolling for horizontal tab list (top position).
     * Converts vertical wheel events to horizontal scroll on the tab list.
     */
    setupWheelScroll() {
        const tabStrip = document.querySelector('.tab-strip');
        if (!tabStrip) return;

        tabStrip.addEventListener('wheel', (e) => {
            // Only handle horizontal scrolling when tab strip is at top
            if (tabStrip.classList.contains('left') || tabStrip.classList.contains('right')) {
                return;
            }

            const tabList = document.getElementById('tabList');
            if (!tabList) return;

            // Convert vertical scroll to horizontal
            if (Math.abs(e.deltaY) > Math.abs(e.deltaX)) {
                e.preventDefault();
                tabList.scrollLeft += e.deltaY;
            }
        }, { passive: false });
    },

    /**
     * Handle mousedown on a tab element.
     * @param {MouseEvent} e - The mouse event.
     * @param {number} index - The index of the clicked tab.
     */
    onMouseDown(e, index) {
        // Only handle left mouse button
        if (e.button !== 0) return;

        // Don't start drag from close button
        if (e.target.classList.contains('tab-close')) return;

        this.dragState.startX = e.clientX;
        this.dragState.startY = e.clientY;
        this.dragState.dragIndex = index;
        this.dragState.sourceElement = e.currentTarget;

        // Prevent text selection during drag
        e.preventDefault();
    },

    /**
     * Handle mousemove - starts drag after threshold, updates ghost and indicator.
     * @param {MouseEvent} e - The mouse event.
     */
    onMouseMove(e) {
        const ds = this.dragState;

        // No pending drag
        if (ds.dragIndex === -1) return;

        // Check if threshold exceeded to start drag
        if (!ds.isDragging) {
            const dx = e.clientX - ds.startX;
            const dy = e.clientY - ds.startY;
            if (Math.abs(dx) < this.dragThreshold && Math.abs(dy) < this.dragThreshold) {
                return;
            }
            this.startDrag(e);
        }

        // Update ghost position to follow cursor
        if (ds.ghostElement) {
            ds.ghostElement.style.left = (e.clientX - ds.ghostOffsetX) + 'px';
            ds.ghostElement.style.top = (e.clientY - ds.ghostOffsetY) + 'px';
        }

        // Update drop indicator position
        this.updateDropIndicator(e.clientX, e.clientY);
    },

    /**
     * Start the drag operation - create ghost element and drop indicator.
     * @param {MouseEvent} e - The mouse event that triggered the drag start.
     */
    startDrag(e) {
        const ds = this.dragState;
        ds.isDragging = true;

        const sourceEl = ds.sourceElement;
        const rect = sourceEl.getBoundingClientRect();

        // Create ghost element (visual clone that follows the cursor)
        const ghost = sourceEl.cloneNode(true);
        ghost.className = 'tab-item tab-ghost';
        ghost.style.width = rect.width + 'px';
        document.body.appendChild(ghost);
        ds.ghostElement = ghost;

        // Offset so the ghost doesn't jump to cursor center
        ds.ghostOffsetX = e.clientX - rect.left;
        ds.ghostOffsetY = e.clientY - rect.top;
        ghost.style.left = (e.clientX - ds.ghostOffsetX) + 'px';
        ghost.style.top = (e.clientY - ds.ghostOffsetY) + 'px';

        // Dim the source tab
        sourceEl.classList.add('dragging');

        // Create drop indicator element
        const indicator = document.createElement('div');
        const isSide = ds.sourceElement.closest('.tab-strip').classList.contains('left') ||
                       ds.sourceElement.closest('.tab-strip').classList.contains('right');
        indicator.className = 'drop-indicator ' + (isSide ? 'drop-indicator-horizontal' : 'drop-indicator-vertical');
        indicator.style.display = 'none';
        document.body.appendChild(indicator);
        ds.dropIndicator = indicator;
    },

    /**
     * Calculate insertion index and position the drop indicator.
     * @param {number} clientX - Cursor X position.
     * @param {number} clientY - Cursor Y position.
     */
    updateDropIndicator(clientX, clientY) {
        const ds = this.dragState;
        const tabList = document.getElementById('tabList');
        const tabStrip = tabList.closest('.tab-strip');
        const stripRect = tabStrip.getBoundingClientRect();
        const isSide = tabStrip.classList.contains('left') || tabStrip.classList.contains('right');

        if (isSide) {
            // Side tabs: hide indicator if cursor is outside the tab strip horizontally
            if (clientX < stripRect.left || clientX > stripRect.right) {
                ds.dropIndicator.style.display = 'none';
                ds.insertIndex = -1;
                return;
            }
        } else {
            // Top tabs: hide indicator if cursor is outside the tab strip vertically
            if (clientY < stripRect.top || clientY > stripRect.bottom) {
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
            ds.dropIndicator.style.top = (indicatorY - 1.5) + 'px';
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
            ds.dropIndicator.style.left = (indicatorX - 1.5) + 'px';
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
