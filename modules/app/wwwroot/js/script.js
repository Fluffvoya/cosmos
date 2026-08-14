/**
 * script.js - Terminal-style cm-script runner tab.
 * Each line entered is executed individually, similar to a terminal REPL.
 * Script logs (Log/Warning/Error) are displayed inline in the output area.
 */

const ScriptPlayground = {
    /** ID of the Script tab (if open) */
    scriptTabId: null,

    /** Command history for up/down arrow navigation */
    history: [],
    historyIndex: -1,

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

        // Output area (scrollable, takes up most of the space)
        const outputWrapper = document.createElement('div');
        outputWrapper.className = 'script-terminal-output';
        outputWrapper.id = 'scriptOutput';

        // Welcome message
        const welcome = document.createElement('div');
        welcome.className = 'script-output-line script-output-info';
        welcome.textContent = 'cm-script Terminal - Type a command and press Enter to execute.';
        outputWrapper.appendChild(welcome);

        container.appendChild(outputWrapper);

        // Input area at the bottom (terminal prompt)
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

        // Focus the input when the panel is rendered
        requestAnimationFrame(() => input.focus());
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
        this.appendOutput('> ' + source, 'command');

        // Clear input
        input.value = '';

        // Send to backend for execution
        if (App.isWebViewReady) {
            window.chrome.webview.postMessage(JSON.stringify({
                type: 'scriptRunSource',
                source: source
            }));
        } else {
            this.appendOutput('WebView2 not available. Cannot run script.', 'error');
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
     * @param {object} data - {type, success, message, line}
     */
    handleRunResult(data) {
        if (data.success) {
            if (data.message) {
                this.appendOutput(data.message, 'success');
            }
        } else {
            this.appendOutput(data.message || 'Execution failed.', 'error');
        }
    },

    /**
     * Handle a script log message forwarded from the backend.
     * These are Log/Warning/Error calls made by cm-script during execution.
     * @param {object} data - {type, level, message}
     */
    handleScriptLog(data) {
        const level = data.level || 'info';
        this.appendOutput(data.message || '', level);
    },

    /**
     * Append a line to the output area.
     * @param {string} text - The output text.
     * @param {string} level - 'info', 'success', 'error', 'warning', or 'command'.
     */
    appendOutput(text, level) {
        const outputWrapper = document.getElementById('scriptOutput');
        if (!outputWrapper) return;

        const line = document.createElement('div');
        line.className = 'script-output-line' + (level ? ' script-output-' + level : '');

        // Timestamp (skip for command echoes)
        if (level !== 'command') {
            const timestamp = document.createElement('span');
            timestamp.className = 'script-output-time';
            const now = new Date();
            const h = String(now.getHours()).padStart(2, '0');
            const m = String(now.getMinutes()).padStart(2, '0');
            const s = String(now.getSeconds()).padStart(2, '0');
            timestamp.textContent = h + ':' + m + ':' + s;
            line.appendChild(timestamp);

            // Level badge
            const badge = document.createElement('span');
            badge.className = 'script-output-badge script-output-badge-' + (level || 'info');
            badge.textContent = (level || 'info').toUpperCase();
            line.appendChild(badge);
        }

        // Message text
        const msg = document.createElement('span');
        msg.className = 'script-output-text';
        msg.textContent = text;
        line.appendChild(msg);

        outputWrapper.appendChild(line);

        // Auto-scroll to bottom
        requestAnimationFrame(() => {
            outputWrapper.scrollTop = outputWrapper.scrollHeight;
        });
    },

    /**
     * Clear the output area.
     */
    clearOutput() {
        const outputWrapper = document.getElementById('scriptOutput');
        if (!outputWrapper) return;
        outputWrapper.innerHTML = '';
    }
};
