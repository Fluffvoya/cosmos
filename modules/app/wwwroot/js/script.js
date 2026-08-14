/**
 * script.js - Terminal-style cm-script runner tab.
 * Each line entered is executed individually, similar to a terminal REPL.
 * Script logs (Log/Warning/Error) are displayed inline in the output area.
 * Output is persisted via the C# backend (settings file) and restored on startup.
 * The Script panel is cached in the DOM so input survives tab switches.
 */

const ScriptPlayground = {
    /** ID of the Script tab (if open) */
    scriptTabId: null,

    /** Command history for up/down arrow navigation */
    history: [],
    historyIndex: -1,

    /** In-memory output entries (synced to backend) */
    outputEntries: [],

    /** Cached Script panel DOM element (preserved across tab switches) */
    _cachedPanel: null,

    /**
     * Load persisted output from backend settings.
     * Called on startup when settingsLoaded message arrives.
     * @param {Array<{text: string, level: string}>} entries
     */
    loadFromSettings(entries) {
        if (!Array.isArray(entries)) return;
        this.outputEntries = entries;

        // If the output area is already rendered, refresh it
        const outputWrapper = document.getElementById('scriptOutput');
        if (outputWrapper) {
            this.renderEntries(outputWrapper);
        }
    },

    /**
     * Open (or focus) the Script tab.
     */
    openScriptTab() {
        if (this.scriptTabId) {
            const existing = Tabs.tabs.find(t => t.id === this.scriptTabId);
            if (existing) {
                Tabs.selectTab(this.scriptTabId);
                return;
            }
            // Tab was closed externally — reset
            this.scriptTabId = null;
        }

        const tab = Tabs.addTab({
            title: 'Script',
            contentType: 'Script',
            icon: '\u{1F4DC}'  // scroll emoji
        });
        this.scriptTabId = tab.id;
    },

    /**
     * Render the Script panel in terminal style.
     * Called by Tabs.renderTabContent when contentType === 'Script'.
     * @param {HTMLElement} container - The panel element to render into.
     */
    renderScriptPanel(container) {
        container.innerHTML = '';
        container.className = 'tab-panel active script-panel';

        // Toolbar with title and clear button
        const toolbar = document.createElement('div');
        toolbar.className = 'script-toolbar';

        const title = document.createElement('span');
        title.className = 'script-toolbar-title';
        title.textContent = 'cm-script Terminal';
        toolbar.appendChild(title);

        // Clear output button
        const clearBtn = document.createElement('button');
        clearBtn.className = 'btn btn-secondary script-clear-btn';
        clearBtn.textContent = 'Clear';
        clearBtn.addEventListener('click', () => this.clearOutput());
        toolbar.appendChild(clearBtn);

        container.appendChild(toolbar);

        // Output area (scrollable, fills remaining space)
        const outputWrapper = document.createElement('div');
        outputWrapper.className = 'script-terminal-output';
        outputWrapper.id = 'scriptOutput';
        container.appendChild(outputWrapper);

        // Input area at the bottom (fixed position via CSS)
        const inputArea = document.createElement('div');
        inputArea.className = 'script-terminal-input-area';

        const promptPrefix = document.createElement('span');
        promptPrefix.className = 'script-terminal-prompt';
        promptPrefix.textContent = '>';
        inputArea.appendChild(promptPrefix);

        const input = document.createElement('input');
        input.type = 'text';
        input.className = 'script-terminal-input';
        input.id = 'scriptTerminalInput';
        input.placeholder = 'Enter cm-script command...';
        input.spellcheck = false;
        input.autocomplete = 'off';
        input.addEventListener('keydown', (e) => this.handleInputKeydown(e));
        inputArea.appendChild(input);

        container.appendChild(inputArea);

        // Restore persisted output from memory (loaded from settings on startup)
        this.renderEntries(outputWrapper);

        // Focus the input
        requestAnimationFrame(() => input.focus());
    },

    /**
     * Render all entries into the output wrapper.
     * @param {HTMLElement} outputWrapper
     */
    renderEntries(outputWrapper) {
        outputWrapper.innerHTML = '';
        for (const entry of this.outputEntries) {
            const line = document.createElement('div');
            line.className = 'script-output-line' + (entry.level ? ' script-output-' + entry.level : '');

            const span = document.createElement('span');
            span.className = 'script-output-text';
            span.textContent = entry.text;
            line.appendChild(span);

            outputWrapper.appendChild(line);
        }

        // Scroll to bottom after rendering
        requestAnimationFrame(() => {
            outputWrapper.scrollTop = outputWrapper.scrollHeight;
        });
    },

    /**
     * Handle keydown events in the terminal input.
     * Enter: execute the command. Up/Down: navigate history.
     * @param {KeyboardEvent} e
     */
    handleInputKeydown(e) {
        if (e.key === 'Enter') {
            e.preventDefault();
            this.executeInput();
        } else if (e.key === 'ArrowUp') {
            e.preventDefault();
            this.navigateHistory(-1);
        } else if (e.key === 'ArrowDown') {
            e.preventDefault();
            this.navigateHistory(1);
        }
    },

    /**
     * Execute the current input value as a cm-script line.
     */
    executeInput() {
        const input = document.getElementById('scriptTerminalInput');
        if (!input) return;

        const source = input.value;
        if (!source.trim()) return;

        // Add to history (avoid consecutive duplicates)
        if (this.history.length === 0 || this.history[this.history.length - 1] !== source) {
            this.history.push(source);
        }
        this.historyIndex = -1;

        // Echo the command to the output
        this.appendLine('> ' + source, 'command');

        // Clear input
        input.value = '';

        // Send to backend for execution
        if (App.isWebViewReady) {
            window.chrome.webview.postMessage(JSON.stringify({
                type: 'scriptRunSource',
                source: source
            }));
        } else {
            this.appendLine('WebView2 not available. Cannot run script.', 'error');
        }
    },

    /**
     * Navigate command history with up/down arrows.
     * @param {number} direction - -1 for up (older), +1 for down (newer).
     */
    navigateHistory(direction) {
        const input = document.getElementById('scriptTerminalInput');
        if (!input || this.history.length === 0) return;

        if (direction === -1) {
            // Up: go to older entry
            if (this.historyIndex === -1) {
                this.historyIndex = this.history.length - 1;
            } else if (this.historyIndex > 0) {
                this.historyIndex--;
            }
        } else {
            // Down: go to newer entry
            if (this.historyIndex === -1) return;
            if (this.historyIndex < this.history.length - 1) {
                this.historyIndex++;
            } else {
                this.historyIndex = -1;
                input.value = '';
                return;
            }
        }

        input.value = this.history[this.historyIndex] || '';
    },

    /**
     * Handle script execution result from the backend.
     * @param {object} data - {type, success, message}
     */
    handleRunResult(data) {
        if (data.success) {
            if (data.message) {
                this.appendLine(data.message, 'success');
            }
        } else {
            this.appendLine(data.message || 'Execution failed.', 'error');
        }
    },

    /**
     * Handle a script log message forwarded from the backend.
     * Displays in log copy format: [HH:mm:ss.fff] [LEVEL] message
     * @param {object} data - {type, level, message}
     */
    handleScriptLog(data) {
        const level = (data.level || 'info').toUpperCase();
        const message = data.message || '';

        // Format matching the Log tab copy format
        const now = new Date();
        const h = String(now.getHours()).padStart(2, '0');
        const m = String(now.getMinutes()).padStart(2, '0');
        const s = String(now.getSeconds()).padStart(2, '0');
        const ms = String(now.getMilliseconds()).padStart(3, '0');
        const time = `${h}:${m}:${s}.${ms}`;

        const formatted = `[${time}] [${level}] ${message}`;
        this.appendLine(formatted, data.level || 'info');
    },

    /**
     * Append a line to the output area and persist it.
     * @param {string} text - The output text.
     * @param {string} level - 'info', 'success', 'error', 'warning', or 'command'.
     */
    appendLine(text, level) {
        const outputWrapper = document.getElementById('scriptOutput');
        if (!outputWrapper) return;

        const line = document.createElement('div');
        line.className = 'script-output-line' + (level ? ' script-output-' + level : '');

        const span = document.createElement('span');
        span.className = 'script-output-text';
        span.textContent = text;
        line.appendChild(span);

        outputWrapper.appendChild(line);

        // Auto-scroll to bottom
        requestAnimationFrame(() => {
            outputWrapper.scrollTop = outputWrapper.scrollHeight;
        });

        // Persist to backend
        this.persistLine(text, level);
    },

    /**
     * Persist a single output line to the backend settings file.
     * @param {string} text
     * @param {string} level
     */
    persistLine(text, level) {
        this.outputEntries.push({ text, level });
        // Keep at most 500 lines to avoid unbounded growth
        while (this.outputEntries.length > 500) this.outputEntries.shift();

        if (App.isWebViewReady) {
            window.chrome.webview.postMessage(JSON.stringify({
                type: 'scriptOutputChanged',
                output: this.outputEntries
            }));
        }
    },

    /**
     * Clear the output area and persisted data.
     */
    clearOutput() {
        const outputWrapper = document.getElementById('scriptOutput');
        if (!outputWrapper) return;
        outputWrapper.innerHTML = '';
        this.outputEntries = [];

        // Clear persisted data in backend
        if (App.isWebViewReady) {
            window.chrome.webview.postMessage(JSON.stringify({
                type: 'scriptOutputChanged',
                output: []
            }));
        }
    }
};
