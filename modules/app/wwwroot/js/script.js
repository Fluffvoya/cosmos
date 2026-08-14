/**
 * script.js - Script Playground tab.
 * Provides an interactive editor where users can write and run cm-script code,
 * with execution output displayed inline.
 */

const ScriptPlayground = {
    /** ID of the Script tab (if open) */
    scriptTabId: null,

    /** Whether a script is currently running */
    isRunning: false,

    /**
     * Open (or focus) the Script Playground tab.
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
     * Render the Script Playground panel.
     * Called by Tabs.renderTabContent when contentType === 'Script'.
     * @param {HTMLElement} container - The panel element to render into.
     */
    renderScriptPanel(container) {
        container.innerHTML = '';
        container.className = 'tab-panel active script-panel';

        // Toolbar
        const toolbar = document.createElement('div');
        toolbar.className = 'script-toolbar';

        const title = document.createElement('span');
        title.className = 'script-toolbar-title';
        title.textContent = 'Script Playground';
        toolbar.appendChild(title);

        // Run button
        const runBtn = document.createElement('button');
        runBtn.className = 'btn btn-primary script-run-btn';
        runBtn.id = 'scriptRunBtn';
        runBtn.textContent = '▶ Run';
        runBtn.disabled = this.isRunning;
        runBtn.addEventListener('click', () => this.runScript());
        toolbar.appendChild(runBtn);

        // Clear output button
        const clearBtn = document.createElement('button');
        clearBtn.className = 'btn btn-secondary script-clear-btn';
        clearBtn.textContent = 'Clear Output';
        clearBtn.addEventListener('click', () => this.clearOutput());
        toolbar.appendChild(clearBtn);

        container.appendChild(toolbar);

        // Split layout: editor (top) + output (bottom)
        const splitLayout = document.createElement('div');
        splitLayout.className = 'script-split-layout';

        // Editor section
        const editorSection = document.createElement('div');
        editorSection.className = 'script-editor-section';

        const editorLabel = document.createElement('div');
        editorLabel.className = 'script-section-label';
        editorLabel.textContent = 'Editor';
        editorSection.appendChild(editorLabel);

        const editorWrapper = document.createElement('div');
        editorWrapper.className = 'script-editor-wrapper';

        const editor = document.createElement('textarea');
        editor.className = 'script-editor';
        editor.id = 'scriptEditor';
        editor.placeholder = '! Write your cm-script code here\n! Example:\nCOSMOS Log "Hello from Script Playground!"\nCOSMOS ShowMessage "Greeting" "Hello, World!"';
        editor.spellcheck = false;
        editorWrapper.appendChild(editor);
        editorSection.appendChild(editorWrapper);

        splitLayout.appendChild(editorSection);

        // Output section
        const outputSection = document.createElement('div');
        outputSection.className = 'script-output-section';

        const outputLabel = document.createElement('div');
        outputLabel.className = 'script-section-label';
        outputLabel.textContent = 'Output';
        outputSection.appendChild(outputLabel);

        const outputWrapper = document.createElement('div');
        outputWrapper.className = 'script-output-wrapper';
        outputWrapper.id = 'scriptOutput';

        // Show placeholder if empty
        const placeholder = document.createElement('div');
        placeholder.className = 'script-output-placeholder';
        placeholder.id = 'scriptOutputPlaceholder';
        placeholder.textContent = 'Output will appear here after running a script.';
        outputWrapper.appendChild(placeholder);

        outputSection.appendChild(outputWrapper);
        splitLayout.appendChild(outputSection);

        container.appendChild(splitLayout);
    },

    /**
     * Run the script content from the editor.
     * Sends the source to the backend for execution.
     */
    runScript() {
        const editor = document.getElementById('scriptEditor');
        if (!editor) return;

        const source = editor.value;
        if (!source.trim()) {
            this.appendOutput('No script content to run.', 'warning');
            return;
        }

        // Disable run button while executing
        this.isRunning = true;
        const runBtn = document.getElementById('scriptRunBtn');
        if (runBtn) runBtn.disabled = true;

        this.appendOutput('Running...', 'info');

        if (App.isWebViewReady) {
            window.chrome.webview.postMessage(JSON.stringify({
                type: 'scriptRunSource',
                source: source
            }));
        } else {
            // Standalone mode — no backend
            this.appendOutput('WebView2 not available. Cannot run script.', 'error');
            this.isRunning = false;
            if (runBtn) runBtn.disabled = false;
        }
    },

    /**
     * Handle script execution result from the backend.
     * @param {object} data - {type, success, message}
     */
    handleRunResult(data) {
        this.isRunning = false;
        const runBtn = document.getElementById('scriptRunBtn');
        if (runBtn) runBtn.disabled = false;

        if (data.success) {
            this.appendOutput(data.message || 'Script completed successfully.', 'success');
        } else {
            this.appendOutput(data.message || 'Script execution failed.', 'error');
        }
    },

    /**
     * Append a line to the output panel.
     * @param {string} text - The output text.
     * @param {string} level - 'info', 'success', 'error', or 'warning'.
     */
    appendOutput(text, level) {
        const outputWrapper = document.getElementById('scriptOutput');
        if (!outputWrapper) return;

        // Remove placeholder if present
        const placeholder = document.getElementById('scriptOutputPlaceholder');
        if (placeholder) placeholder.remove();

        const line = document.createElement('div');
        line.className = 'script-output-line' + (level ? ' script-output-' + level : '');

        // Timestamp
        const timestamp = document.createElement('span');
        timestamp.className = 'script-output-time';
        const now = new Date();
        const h = String(now.getHours()).padStart(2, '0');
        const m = String(now.getMinutes()).padStart(2, '0');
        const s = String(now.getSeconds()).padStart(2, '0');
        const ms = String(now.getMilliseconds()).padStart(3, '0');
        timestamp.textContent = h + ':' + m + ':' + s + '.' + ms;
        line.appendChild(timestamp);

        // Level badge
        const badge = document.createElement('span');
        badge.className = 'script-output-badge script-output-badge-' + (level || 'info');
        badge.textContent = (level || 'info').toUpperCase();
        line.appendChild(badge);

        // Message
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
     * Clear the output panel.
     */
    clearOutput() {
        const outputWrapper = document.getElementById('scriptOutput');
        if (!outputWrapper) return;

        outputWrapper.innerHTML = '';

        // Re-add placeholder
        const placeholder = document.createElement('div');
        placeholder.className = 'script-output-placeholder';
        placeholder.id = 'scriptOutputPlaceholder';
        placeholder.textContent = 'Output will appear here after running a script.';
        outputWrapper.appendChild(placeholder);
    }
};
