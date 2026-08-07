/**
 * settings.js - Settings panel logic.
 * Handles the settings overlay, category navigation, and settings persistence.
 */

const Settings = {
    // Current settings state
    settings: {
        tabPosition: 'top',
        tabStripWidth: 140,
        pythonPath: '',
        startupScriptPath: '',
    },

    // Original settings (for change tracking)
    originalSettings: null,

    // Selected category
    selectedCategory: 'general',

    // Python path validation state
    pythonPathValid: true,
    pythonPathValidating: false,
    pythonPathError: '',

    // Startup script path validation state
    startupScriptPathValid: true,
    startupScriptPathValidating: false,
    startupScriptPathError: '',

    /**
     * Initialize the settings module.
     */
    init() {
        this.setupEventHandlers();
        this.loadSettings(this.settings);
    },

    /**
     * Set up event handlers for the settings panel.
     */
    setupEventHandlers() {
        // Close button
        const closeBtn = document.getElementById('settingsClose');
        if (closeBtn) {
            closeBtn.addEventListener('click', () => this.hide());
        }

        // Category navigation
        const categories = document.querySelectorAll('.settings-category');
        categories.forEach(cat => {
            cat.addEventListener('click', () => {
                this.selectCategory(cat.dataset.category);
            });
        });

        // Tab position change
        const tabPositionSelect = document.getElementById('settingTabPosition');
        if (tabPositionSelect) {
            tabPositionSelect.addEventListener('change', () => {
                this.settings.tabPosition = tabPositionSelect.value;
                this.updateChangeTracking();
            });
        }

        // Python path change
        const pythonPathInput = document.getElementById('settingPythonPath');
        if (pythonPathInput) {
            pythonPathInput.addEventListener('input', () => {
                this.settings.pythonPath = pythonPathInput.value;
                this.validatePythonPath();
                this.updateChangeTracking();
            });
        }

        // Browse button for Python path (opens native file dialog via WebView2)
        const browseBtn = document.getElementById('browsePythonPath');
        if (browseBtn) {
            browseBtn.addEventListener('click', () => {
                this.browsePythonPath();
            });
        }

        // Startup script path change
        const startupScriptPathInput = document.getElementById('settingStartupScriptPath');
        if (startupScriptPathInput) {
            startupScriptPathInput.addEventListener('input', () => {
                this.settings.startupScriptPath = startupScriptPathInput.value;
                this.validateStartupScriptPath();
                this.updateChangeTracking();
            });
        }

        // Browse button for startup script (opens native file dialog via WebView2)
        const browseStartupScriptBtn = document.getElementById('browseStartupScriptPath');
        if (browseStartupScriptBtn) {
            browseStartupScriptBtn.addEventListener('click', () => {
                this.browseStartupScriptPath();
            });
        }

        // Apply button
        const applyBtn = document.getElementById('settingsApply');
        if (applyBtn) {
            applyBtn.addEventListener('click', () => this.apply());
        }

        // Save button
        const saveBtn = document.getElementById('settingsSave');
        if (saveBtn) {
            saveBtn.addEventListener('click', () => this.save());
        }

        // Cancel button
        const cancelBtn = document.getElementById('settingsCancel');
        if (cancelBtn) {
            cancelBtn.addEventListener('click', () => this.hide());
        }

        // Overlay click (close on outside click)
        const overlay = document.getElementById('settingsOverlay');
        if (overlay) {
            overlay.addEventListener('click', (e) => {
                if (e.target === overlay) {
                    this.hide();
                }
            });
        }
    },

    /**
     * Show the settings panel.
     */
    show() {
        // Store original settings for change tracking
        this.originalSettings = { ...this.settings };

        // Update UI to reflect current settings
        this.updateUI();

        // Show the overlay
        document.getElementById('settingsOverlay').style.display = 'flex';

        // Select the first category
        this.selectCategory('general');
    },

    /**
     * Hide the settings panel.
     */
    hide() {
        document.getElementById('settingsOverlay').style.display = 'none';
    },

    /**
     * Load settings from the backend.
     * @param {object} settings - The settings to load.
     */
    loadSettings(settings) {
        if (settings) {
            this.settings = { ...this.settings, ...settings };
        }
        this.updateUI();
    },

    /**
     * Update the UI to reflect current settings.
     */
    updateUI() {
        // Tab position
        const tabPositionSelect = document.getElementById('settingTabPosition');
        if (tabPositionSelect) {
            tabPositionSelect.value = this.settings.tabPosition;
        }

        // Startup script path
        const startupScriptPathInput = document.getElementById("settingStartupScriptPath");
        if (startupScriptPathInput) {
            startupScriptPathInput.value = this.settings.startupScriptPath;
        }

        // Python path
        const pythonPathInput = document.getElementById("settingPythonPath");
        if (pythonPathInput) {
            pythonPathInput.value = this.settings.pythonPath;
        }
    },

    /**
     * Select a settings category.
     * @param {string} category - The category to select ('general', 'scripting', or 'startup').
     */
    selectCategory(category) {
        this.selectedCategory = category;

        // Update sidebar selection
        document.querySelectorAll('.settings-category').forEach(cat => {
            cat.classList.toggle('active', cat.dataset.category === category);
        });

        // Show/hide sections based on category
        const generalSection = document.getElementById('settingsGeneral');
        const scriptingSection = document.getElementById('settingsScripting');
        const startupSection = document.getElementById('settingsStartup');

        if (generalSection) generalSection.style.display = category === 'general' ? '' : 'none';
        if (scriptingSection) scriptingSection.style.display = category === 'scripting' ? '' : 'none';
        if (startupSection) startupSection.style.display = category === 'startup' ? '' : 'none';
    },

    /**
     * Validate the Python path.
     * Sends the path to the backend for validation.
     */
    validatePythonPath() {
        const path = this.settings.pythonPath;

        // Reset validation state
        this.pythonPathValidating = true;
        this.pythonPathValid = true;
        this.pythonPathError = '';

        // Update UI to show validating state
        this.updatePythonPathValidationUI();

        // Empty path is valid (user hasn't set one yet)
        if (!path) {
            this.pythonPathValidating = false;
            this.updatePythonPathValidationUI();
            this.updateChangeTracking();
            return;
        }

        // Send validation request to backend
        if (App.isWebViewReady) {
            window.chrome.webview.postMessage(JSON.stringify({
                type: 'validatePythonPath',
                path: path
            }));
        } else {
            // In standalone mode, just mark as valid
            this.pythonPathValidating = false;
            this.updatePythonPathValidationUI();
            this.updateChangeTracking();
        }
    },

    /**
     * Update the UI based on Python path validation state.
     */
    updatePythonPathValidationUI() {
        const inputEl = document.getElementById('settingPythonPath');
        const errorEl = document.getElementById('pythonPathError');

        if (this.pythonPathValidating) {
            // Show loading state
            if (inputEl) inputEl.style.borderColor = 'var(--accent-color)';
            if (errorEl) {
                errorEl.textContent = 'Validating...';
                errorEl.style.display = 'block';
            }
        } else if (!this.pythonPathValid) {
            // Show error state
            if (inputEl) inputEl.style.borderColor = 'var(--error-color)';
            if (errorEl) {
                errorEl.textContent = this.pythonPathError;
                errorEl.style.display = 'block';
            }
        } else {
            // Clear error state
            if (inputEl) inputEl.style.borderColor = '';
            if (errorEl) errorEl.style.display = 'none';
        }

        this.updateChangeTracking();
    },

    /**
     * Handle validation response from the backend.
     * @param {object} data - Validation result with isValid and error properties.
     */
    handleValidationResponse(data) {
        this.pythonPathValidating = false;
        this.pythonPathValid = data.isValid;
        this.pythonPathError = data.error || '';

        this.updatePythonPathValidationUI();
        this.updateChangeTracking();
    },

    /**
     * Validate the startup script path.
     * Sends the path to the backend for validation.
     */
    validateStartupScriptPath() {
        const path = this.settings.startupScriptPath;

        // Reset validation state
        this.startupScriptPathValidating = true;
        this.startupScriptPathValid = true;
        this.startupScriptPathError = '';

        // Update UI to show validating state
        this.updateStartupScriptPathValidationUI();

        // Empty path is valid (startup script is optional)
        if (!path) {
            this.startupScriptPathValidating = false;
            this.updateStartupScriptPathValidationUI();
            this.updateChangeTracking();
            return;
        }

        // Send validation request to backend
        if (App.isWebViewReady) {
            window.chrome.webview.postMessage(JSON.stringify({
                type: 'validateStartupScriptPath',
                path: path
            }));
        } else {
            // In standalone mode, just mark as valid
            this.startupScriptPathValidating = false;
            this.updateStartupScriptPathValidationUI();
            this.updateChangeTracking();
        }
    },

    /**
     * Update the UI based on startup script path validation state.
     */
    updateStartupScriptPathValidationUI() {
        const inputEl = document.getElementById('settingStartupScriptPath');
        const errorEl = document.getElementById('startupScriptPathError');

        if (this.startupScriptPathValidating) {
            // Show loading state
            if (inputEl) inputEl.style.borderColor = 'var(--accent-color)';
            if (errorEl) {
                errorEl.textContent = 'Validating...';
                errorEl.style.display = 'block';
            }
        } else if (!this.startupScriptPathValid) {
            // Show error state
            if (inputEl) inputEl.style.borderColor = 'var(--error-color)';
            if (errorEl) {
                errorEl.textContent = this.startupScriptPathError;
                errorEl.style.display = 'block';
            }
        } else {
            // Clear error state
            if (inputEl) inputEl.style.borderColor = '';
            if (errorEl) errorEl.style.display = 'none';
        }

        this.updateChangeTracking();
    },

    /**
     * Handle validation response for startup script path from the backend.
     * @param {object} data - Validation result with isValid and error properties.
     */
    handleStartupScriptPathValidationResponse(data) {
        this.startupScriptPathValidating = false;
        this.startupScriptPathValid = data.isValid;
        this.startupScriptPathError = data.error || '';

        this.updateStartupScriptPathValidationUI();
        this.updateChangeTracking();
    },

    /**
     * Browse for startup script path.
     * Sends a request to the C# backend to open a native file dialog.
     */
    browseStartupScriptPath() {
        if (App.isWebViewReady) {
            window.chrome.webview.postMessage(JSON.stringify({
                type: 'browseStartupScriptPath'
            }));
        }
    },

    /**
     * Handle browse result for startup script from the C# backend.
     * @param {object} data - The browse result with selectedPath property.
     */
    handleStartupScriptBrowseResult(data) {
        if (data.selectedPath) {
            this.settings.startupScriptPath = data.selectedPath;

            const startupScriptPathInput = document.getElementById('settingStartupScriptPath');
            if (startupScriptPathInput) {
                startupScriptPathInput.value = data.selectedPath;
            }

            this.validateStartupScriptPath();
            this.updateChangeTracking();
        }
    },

    /**
     * Update change tracking (enable/disable Apply/Save buttons).
     */
    updateChangeTracking() {
        const hasChanges = this.originalSettings && (
            this.settings.tabPosition !== this.originalSettings.tabPosition || 
            this.settings.pythonPath !== this.originalSettings.pythonPath ||
            this.settings.startupScriptPath !== this.originalSettings.startupScriptPath
        );

        const applyBtn = document.getElementById('settingsApply');
        const saveBtn = document.getElementById('settingsSave');

        if (applyBtn) applyBtn.disabled = !hasChanges;
        if (saveBtn) saveBtn.disabled = !hasChanges;
    },

    /**
     * Apply the current settings.
     * Validates Python path before saving - blocks save if path is invalid.
     */
    apply() {
        // Check if Python path is valid before saving
        if (!this.pythonPathValid) {
            this.showValidationWarning();
            return false;
        }

        // Check if startup script path is valid before saving
        if (!this.startupScriptPathValid) {
            this.showStartupScriptValidationWarning();
            return false;
        }

        // Send settings to the backend
        App.sendSettingsChanged(this.settings);

        // Update original settings to reflect the applied state
        this.originalSettings = { ...this.settings };
        this.updateChangeTracking();

        // Apply tab position immediately
        this.applyTabPosition();

        return true;
    },

    /**
     * Show a warning dialog when Python path validation fails on Apply/Save.
     */
    showValidationWarning() {
        const overlay = document.getElementById('modalOverlay');
        const title = document.getElementById('modalTitle');
        const body = document.getElementById('modalMessage');

        title.textContent = 'Invalid Python Path';
        body.textContent = `The Python path "${this.settings.pythonPath}" is not valid: ${this.pythonPathError}\n\nPlease enter a valid Python interpreter path before saving.`;
        overlay.style.display = 'flex';

        // No requestId needed for this warning dialog
        overlay.dataset.requestId = '';
    },

    /**
     * Show a warning dialog when startup script path validation fails on Apply/Save.
     */
    showStartupScriptValidationWarning() {
        const overlay = document.getElementById('modalOverlay');
        const title = document.getElementById('modalTitle');
        const body = document.getElementById('modalMessage');

        title.textContent = 'Invalid Startup Script Path';
        body.textContent = `The startup script path "${this.settings.startupScriptPath}" is not valid: ${this.startupScriptPathError}\n\nPlease enter a valid script path before saving.`;
        overlay.style.display = 'flex';

        // No requestId needed for this warning dialog
        overlay.dataset.requestId = '';
    },

    /**
     * Save the current settings and close the panel.
     * Validates Python path before saving - blocks save if path is invalid.
     */
    save() {
        // Check if Python path is valid before saving
        if (!this.pythonPathValid) {
            this.showValidationWarning();
            return;
        }

        // Check if startup script path is valid before saving
        if (!this.startupScriptPathValid) {
            this.showStartupScriptValidationWarning();
            return;
        }

        // Apply and close
        if (this.apply()) {
            this.hide();
        }
    },

    /**
     * Apply the tab position setting to the UI.
     */
    applyTabPosition() {
        const tabStrip = document.querySelector('.tab-strip');
        const body = document.body;

        if (!tabStrip) return;

        // Remove existing position classes
        tabStrip.classList.remove('left', 'right');
        body.classList.remove('tab-left', 'tab-right');

        // Apply new position
        switch (this.settings.tabPosition) {
            case 'left':
                tabStrip.classList.add('left');
                body.classList.add('tab-left');
                // Apply saved width for left/right positions
                Splitter.applyWidth(this.settings.tabStripWidth);
                break;
            case 'right':
                tabStrip.classList.add('right');
                body.classList.add('tab-right');
                // Apply saved width for left/right positions
                Splitter.applyWidth(this.settings.tabStripWidth);
                break;
            case 'top':
            default:
                // Default is top - reset width and no additional classes needed
                Splitter.resetWidth();
                break;
        }
    },

    /**
     * Save the tab strip width to persistent storage.
     * @param {number} width - The new width value.
     */
    saveTabStripWidth(width) {
        this.settings.tabStripWidth = width;
        App.sendSettingsChanged(this.settings);
    },

    /**
     * Browse for Python interpreter path.
     * Sends a request to the C# backend to open a native file dialog.
     */
    browsePythonPath() {
        if (App.isWebViewReady) {
            window.chrome.webview.postMessage(JSON.stringify({
                type: 'browsePythonPath'
            }));
        }
    },

    /**
     * Handle browse result from the C# backend.
     * @param {object} data - The browse result with selectedPath property.
     */
    handleBrowseResult(data) {
        if (data.selectedPath) {
            this.settings.pythonPath = data.selectedPath;

            const pythonPathInput = document.getElementById('settingPythonPath');
            if (pythonPathInput) {
                pythonPathInput.value = data.selectedPath;
            }

            this.validatePythonPath();
            this.updateChangeTracking();
        }
    },
};
