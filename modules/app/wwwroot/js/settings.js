/**
 * settings.js - Settings panel logic.
 * Handles the settings overlay, category navigation, and settings persistence.
 */

const Settings = {
    // Current settings state
    settings: {
        tabPosition: 'top',
        tabStripWidth: 140,
        pythonPath: ''
    },

    // Original settings (for change tracking)
    originalSettings: null,

    // Selected category
    selectedCategory: 'general',

    // Python path validation state
    pythonPathValid: true,
    pythonPathValidating: false,
    pythonPathError: '',

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

        // Browse button (opens native file dialog via WebView2)
        const browseBtn = document.getElementById('browsePythonPath');
        if (browseBtn) {
            browseBtn.addEventListener('click', () => {
                this.browsePythonPath();
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

        // Python path
        const pythonPathInput = document.getElementById('settingPythonPath');
        if (pythonPathInput) {
            pythonPathInput.value = this.settings.pythonPath;
        }

        // Validate python path
        this.validatePythonPath();
    },

    /**
     * Select a settings category.
     * @param {string} category - The category to select ('general' or 'scripting').
     */
    selectCategory(category) {
        this.selectedCategory = category;

        // Update sidebar selection
        document.querySelectorAll('.settings-category').forEach(cat => {
            cat.classList.toggle('active', cat.dataset.category === category);
        });

        // Show/hide sections
        document.getElementById('settingsGeneral').style.display =
            category === 'general' ? 'block' : 'none';
        document.getElementById('settingsScripting').style.display =
            category === 'scripting' ? 'block' : 'none';
    },

    /**
     * Validate the Python path by sending a request to the C# backend.
     * Shows an error message if the path is not empty and doesn't exist.
     */
    validatePythonPath() {
        const pythonPath = this.settings.pythonPath;
        const errorEl = document.getElementById('pythonPathError');
        const inputEl = document.getElementById('settingPythonPath');

        // If empty, consider valid (not required)
        if (!pythonPath || pythonPath.trim() === '') {
            this.pythonPathValid = true;
            this.pythonPathError = '';
            if (errorEl) errorEl.style.display = 'none';
            if (inputEl) inputEl.style.borderColor = '';
            this.updateChangeTracking();
            return;
        }

        // Mark as validating
        this.pythonPathValidating = true;

        // Send validation request to C# backend
        if (App.isWebViewReady) {
            window.chrome.webview.postMessage(JSON.stringify({
                type: 'validatePythonPath',
                path: pythonPath
            }));
        }
    },

    /**
     * Handle validation response from the C# backend.
     * @param {object} data - The validation result with isValid and error properties.
     */
    handleValidationResponse(data) {
        this.pythonPathValidating = false;
        this.pythonPathValid = data.isValid;
        this.pythonPathError = data.error || '';

        const errorEl = document.getElementById('pythonPathError');
        const inputEl = document.getElementById('settingPythonPath');

        if (!data.isValid) {
            // Show error with red text
            if (errorEl) {
                errorEl.textContent = this.pythonPathError;
                errorEl.style.display = 'block';
            }
            if (inputEl) {
                inputEl.style.borderColor = 'var(--danger-color)';
            }
        } else {
            // Clear error
            if (errorEl) errorEl.style.display = 'none';
            if (inputEl) inputEl.style.borderColor = '';
        }

        this.updateChangeTracking();
    },

    /**
     * Update change tracking (enable/disable Apply/Save buttons).
     */
    updateChangeTracking() {
        const hasChanges = this.originalSettings &&
            (this.settings.tabPosition !== this.originalSettings.tabPosition ||
             this.settings.pythonPath !== this.originalSettings.pythonPath);

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
     * Save the current settings and close the panel.
     * Validates Python path before saving - blocks save if path is invalid.
     */
    save() {
        // Check if Python path is valid before saving
        if (!this.pythonPathValid) {
            this.showValidationWarning();
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
    }
};
