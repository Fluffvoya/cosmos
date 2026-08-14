/**
 * log.js - Log store and Log tab rendering.
 * Stores log entries emitted by scripts and the program,
 * and renders them in a filterable log panel.
 */

const LogStore = {
    // All log entries
    entries: [],

    // ID of the Log tab (if open)
    logTabId: null,

    // Current filter: 'all', 'error', 'warning', 'info'
    filter: 'all',

    /**
     * Add a log entry and update the Log tab if it's open.
     * @param {string} level - 'Error', 'Warning', or 'Info'.
     * @param {string} sender - The source that emitted the log (e.g. 'program', script name).
     * @param {string} content - The log message.
     */
    addEntry(level, sender, content) {
        const entry = {
            id: this.entries.length,
            timestamp: new Date(),
            level: level || 'Info',
            sender: sender || 'unknown',
            content: content || ''
        };

        this.entries.push(entry);

        // If the Log tab is currently visible, append the row
        this.appendRowIfVisible(entry);
    },

    /**
     * Open (or focus) the Log tab.
     */
    openLogTab() {
        if (this.logTabId) {
            // Tab already open — just select it
            const existing = Tabs.tabs.find(t => t.id === this.logTabId);
            if (existing) {
                Tabs.selectTab(this.logTabId);
                return;
            }
            // Tab was closed externally — reset
            this.logTabId = null;
        }

        const tab = Tabs.addTab({
            title: 'Log',
            contentType: 'Log',
            icon: '\u{1F4CB}'  // clipboard emoji
        });
        this.logTabId = tab.id;
    },

    /**
     * Render the full Log tab content.
     * Called by Tabs.renderTabContent when contentType === 'Log'.
     * @param {HTMLElement} container - The panel element to render into.
     */
    renderLogPanel(container) {
        container.innerHTML = '';
        container.className = 'tab-panel active log-panel';

        // Toolbar
        const toolbar = document.createElement('div');
        toolbar.className = 'log-toolbar';

        // Filter buttons
        const filters = ['all', 'error', 'warning', 'info'];
        filters.forEach(f => {
            const btn = document.createElement('button');
            btn.className = 'log-filter-btn' + (this.filter === f ? ' active' : '');
            btn.textContent = f.charAt(0).toUpperCase() + f.slice(1);
            btn.addEventListener('click', () => {
                this.filter = f;
                this.renderLogPanel(container);
            });
            toolbar.appendChild(btn);
        });

        // Clear button
        const clearBtn = document.createElement('button');
        clearBtn.className = 'log-filter-btn log-clear-btn';
        clearBtn.textContent = 'Clear';
        clearBtn.addEventListener('click', () => {
            this.entries = [];
            this.renderLogPanel(container);
        });
        toolbar.appendChild(clearBtn);

        container.appendChild(toolbar);

        // Table
        const tableWrapper = document.createElement('div');
        tableWrapper.className = 'log-table-wrapper';

        const table = document.createElement('table');
        table.className = 'log-table';

        // Header
        const thead = document.createElement('thead');
        thead.innerHTML = '<tr>' +
            '<th class="log-col-time">Time</th>' +
            '<th class="log-col-level">Level</th>' +
            '<th class="log-col-sender">Sender</th>' +
            '<th class="log-col-message">Message</th>' +
            '<th class="log-col-action">Action</th>' +
            '</tr>';
        table.appendChild(thead);

        // Body
        const tbody = document.createElement('tbody');
        tbody.id = 'logTableBody';

        const filtered = this.getFilteredEntries();
        if (filtered.length === 0) {
            const tr = document.createElement('tr');
            tr.className = 'log-empty-row';
            tr.innerHTML = '<td colspan="5">No log entries</td>';
            tbody.appendChild(tr);
        } else {
            filtered.forEach(entry => {
                tbody.appendChild(this.createRowElement(entry));
            });
        }

        table.appendChild(tbody);
        tableWrapper.appendChild(table);
        container.appendChild(tableWrapper);

        // Auto-scroll to bottom
        requestAnimationFrame(() => {
            tableWrapper.scrollTop = tableWrapper.scrollHeight;
        });
    },

    /**
     * Append a single row to the log table if the Log tab is visible.
     * @param {object} entry - The log entry.
     */
    appendRowIfVisible(entry) {
        if (!this.logTabId || Tabs.activeTabId !== this.logTabId) return;

        const tbody = document.getElementById('logTableBody');
        if (!tbody) return;

        // Remove "No log entries" placeholder if present
        const emptyRow = tbody.querySelector('.log-empty-row');
        if (emptyRow) emptyRow.remove();

        // Check filter
        if (this.filter !== 'all' && entry.level.toLowerCase() !== this.filter) return;

        tbody.appendChild(this.createRowElement(entry));

        // Auto-scroll
        const wrapper = tbody.closest('.log-table-wrapper');
        if (wrapper) {
            requestAnimationFrame(() => {
                wrapper.scrollTop = wrapper.scrollHeight;
            });
        }
    },

    /**
     * Create a table row element for a log entry.
     * @param {object} entry - The log entry.
     * @returns {HTMLTableRowElement} The row element.
     */
    createRowElement(entry) {
        const tr = document.createElement('tr');
        tr.className = 'log-row log-level-' + entry.level.toLowerCase();

        // Time
        const tdTime = document.createElement('td');
        tdTime.className = 'log-col-time';
        tdTime.textContent = this.formatTime(entry.timestamp);
        tr.appendChild(tdTime);

        // Level
        const tdLevel = document.createElement('td');
        tdLevel.className = 'log-col-level';
        const levelBadge = document.createElement('span');
        levelBadge.className = 'log-level-badge log-level-' + entry.level.toLowerCase();
        levelBadge.textContent = entry.level;
        tdLevel.appendChild(levelBadge);
        tr.appendChild(tdLevel);

        // Sender
        const tdSender = document.createElement('td');
        tdSender.className = 'log-col-sender';
        tdSender.textContent = entry.sender;
        tr.appendChild(tdSender);

        // Message
        const tdMessage = document.createElement('td');
        tdMessage.className = 'log-col-message';
        tdMessage.textContent = entry.content;
        tr.appendChild(tdMessage);

        // Action (copy button)
        const tdAction = document.createElement('td');
        tdAction.className = 'log-col-action';
        const copyBtn = document.createElement('button');
        copyBtn.className = 'log-copy-btn';
        copyBtn.title = 'Copy to clipboard';
        copyBtn.textContent = '📋';  // clipboard emoji
        copyBtn.addEventListener('click', (e) => {
            e.stopPropagation();
            this.copyLogEntry(entry, copyBtn);
        });
        tdAction.appendChild(copyBtn);
        tr.appendChild(tdAction);

        return tr;
    },

    /**
     * Get entries filtered by the current filter level.
     * @returns {object[]} Filtered entries.
     */
    getFilteredEntries() {
        if (this.filter === 'all') return this.entries;
        return this.entries.filter(e => e.level.toLowerCase() === this.filter);
    },

    /**
     * Format a Date object as HH:MM:SS.mmm.
     * @param {Date} date - The date to format.
     * @returns {string} Formatted time string.
     */
    formatTime(date) {
        const h = String(date.getHours()).padStart(2, '0');
        const m = String(date.getMinutes()).padStart(2, '0');
        const s = String(date.getSeconds()).padStart(2, '0');
        const ms = String(date.getMilliseconds()).padStart(3, '0');
        return `${h}:${m}:${s}.${ms}`;
    },

    /**
     * Format a log entry as a single line and copy to clipboard.
     * @param {object} entry - The log entry.
     * @param {HTMLElement} btn - The copy button element (for visual feedback).
     */
    copyLogEntry(entry, btn) {
        const time = this.formatTime(entry.timestamp);
        const text = `[${time}] [${entry.level}] [${entry.sender}] ${entry.content}`;

        navigator.clipboard.writeText(text).then(() => {
            // Brief visual confirmation
            btn.textContent = '✓';
            btn.classList.add('copied');
            setTimeout(() => {
                btn.textContent = '📋';
                btn.classList.remove('copied');
            }, 1000);
        }).catch(() => {
            // Fallback: select and copy via textarea
            const textarea = document.createElement('textarea');
            textarea.value = text;
            textarea.style.position = 'fixed';
            textarea.style.opacity = '0';
            document.body.appendChild(textarea);
            textarea.select();
            document.execCommand('copy');
            document.body.removeChild(textarea);

            btn.textContent = '✓';
            btn.classList.add('copied');
            setTimeout(() => {
                btn.textContent = '📋';
                btn.classList.remove('copied');
            }, 1000);
        });
    }
};
