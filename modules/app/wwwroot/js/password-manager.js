/**
 * password-manager.js - Secure password manager tab.
 * Manages platforms and accounts with encrypted storage.
 * Master password protects access to stored credentials.
 * Passwords are never displayed in plain text - only copyable.
 */

const PasswordManager = {
    /** ID of the Password Manager tab (if open) */
    passwordTabId: null,

    /** Current authentication state */
    isAuthenticated: false,

    /** In-memory platform data (decrypted) */
    platforms: [],

    /** Currently selected platform index (-1 if none) */
    selectedPlatformIndex: -1,

    /** Password generator settings */
    generatorSettings: {
        length: 14,
        useSpecialChars: true
    },

    /**
     * Open (or focus) the Password Manager tab.
     */
    openPasswordManagerTab() {
        if (this.passwordTabId) {
            const existing = Tabs.tabs.find(t => t.id === this.passwordTabId);
            if (existing) {
                Tabs.selectTab(this.passwordTabId);
                return;
            }
            // Tab was closed externally - reset
            this.passwordTabId = null;
            this.isAuthenticated = false;
        }

        const tab = Tabs.addTab({
            title: 'Password Manager',
            contentType: 'PasswordManager',
            icon: '\u{1F512}'  // lock emoji
        });
        this.passwordTabId = tab.id;
    },

    /**
     * Render the Password Manager panel.
     * Called by Tabs.renderTabContent when contentType === 'PasswordManager'.
     * @param {HTMLElement} container - The panel element to render into.
     */
    renderPasswordManagerPanel(container) {
        container.innerHTML = '';
        container.className = 'tab-panel active password-panel';

        // Check if master password is set up
        if (App.isWebViewReady) {
            window.chrome.webview.postMessage(JSON.stringify({
                type: 'passwordManagerCheckSetup'
            }));
        }

        // Show loading state initially
        this.renderLoadingScreen(container);
    },

    /**
     * Render loading screen while checking setup status.
     * @param {HTMLElement} container
     */
    renderLoadingScreen(container) {
        const loading = document.createElement('div');
        loading.className = 'password-loading';
        loading.textContent = 'Loading...';
        container.appendChild(loading);
    },

    /**
     * Handle setup check response from backend.
     * @param {boolean} isSetup - Whether master password is already set up.
     */
    handleSetupCheck(isSetup) {
        const container = document.querySelector('.password-panel');
        if (!container) return;

        if (isSetup) {
            this.renderLoginScreen(container);
        } else {
            this.renderSetupScreen(container);
        }
    },

    /**
     * Render first-time setup screen.
     * @param {HTMLElement} container
     */
    renderSetupScreen(container) {
        container.innerHTML = '';

        const setupDiv = document.createElement('div');
        setupDiv.className = 'password-setup';

        const title = document.createElement('h2');
        title.textContent = 'Set Up Password Manager';
        setupDiv.appendChild(title);

        const desc = document.createElement('p');
        desc.className = 'password-setup-desc';
        desc.textContent = 'Create a master password to protect your credentials. You will need this password to access your stored accounts.';
        setupDiv.appendChild(desc);

        // Password input
        const passwordGroup = document.createElement('div');
        passwordGroup.className = 'password-input-group';

        const passwordLabel = document.createElement('label');
        passwordLabel.textContent = 'Master Password';
        passwordGroup.appendChild(passwordLabel);

        const passwordInput = document.createElement('input');
        passwordInput.type = 'password';
        passwordInput.id = 'setupPassword';
        passwordInput.placeholder = 'Enter master password';
        passwordGroup.appendChild(passwordInput);

        setupDiv.appendChild(passwordGroup);

        // Confirm password input
        const confirmGroup = document.createElement('div');
        confirmGroup.className = 'password-input-group';

        const confirmLabel = document.createElement('label');
        confirmLabel.textContent = 'Confirm Password';
        confirmGroup.appendChild(confirmLabel);

        const confirmInput = document.createElement('input');
        confirmInput.type = 'password';
        confirmInput.id = 'setupPasswordConfirm';
        confirmInput.placeholder = 'Confirm master password';
        confirmGroup.appendChild(confirmInput);

        setupDiv.appendChild(confirmGroup);

        // Error message
        const errorMsg = document.createElement('div');
        errorMsg.className = 'password-error';
        errorMsg.id = 'setupError';
        errorMsg.style.display = 'none';
        setupDiv.appendChild(errorMsg);

        // Submit button
        const submitBtn = document.createElement('button');
        submitBtn.className = 'btn btn-primary password-submit-btn';
        submitBtn.textContent = 'Set Password';
        submitBtn.addEventListener('click', () => this.handleSetupSubmit());
        setupDiv.appendChild(submitBtn);

        container.appendChild(setupDiv);

        // Focus first input
        requestAnimationFrame(() => passwordInput.focus());
    },

    /**
     * Handle setup form submission.
     */
    handleSetupSubmit() {
        const password = document.getElementById('setupPassword').value;
        const confirm = document.getElementById('setupPasswordConfirm').value;
        const errorEl = document.getElementById('setupError');

        // Validate
        if (!password) {
            errorEl.textContent = 'Please enter a password.';
            errorEl.style.display = 'block';
            return;
        }

        if (password.length < 4) {
            errorEl.textContent = 'Password must be at least 4 characters.';
            errorEl.style.display = 'block';
            return;
        }

        if (password !== confirm) {
            errorEl.textContent = 'Passwords do not match.';
            errorEl.style.display = 'block';
            return;
        }

        errorEl.style.display = 'none';

        // Send to backend
        if (App.isWebViewReady) {
            window.chrome.webview.postMessage(JSON.stringify({
                type: 'passwordManagerSetup',
                password: password
            }));
        }
    },

    /**
     * Handle setup success response from backend.
     */
    handleSetupSuccess() {
        this.isAuthenticated = true;
        this.platforms = [];

        const container = document.querySelector('.password-panel');
        if (!container) return;

        this.renderMainScreen(container);
    },

    /**
     * Render login screen.
     * @param {HTMLElement} container
     */
    renderLoginScreen(container) {
        container.innerHTML = '';

        const loginDiv = document.createElement('div');
        loginDiv.className = 'password-login';

        const title = document.createElement('h2');
        title.textContent = 'Password Manager';
        loginDiv.appendChild(title);

        const desc = document.createElement('p');
        desc.className = 'password-login-desc';
        desc.textContent = 'Enter your master password to access your credentials.';
        loginDiv.appendChild(desc);

        // Password input
        const passwordGroup = document.createElement('div');
        passwordGroup.className = 'password-input-group';

        const passwordLabel = document.createElement('label');
        passwordLabel.textContent = 'Master Password';
        passwordGroup.appendChild(passwordLabel);

        const passwordInput = document.createElement('input');
        passwordInput.type = 'password';
        passwordInput.id = 'loginPassword';
        passwordInput.placeholder = 'Enter master password';
        passwordInput.addEventListener('keydown', (e) => {
            if (e.key === 'Enter') {
                this.handleLoginSubmit();
            }
        });
        passwordGroup.appendChild(passwordInput);

        loginDiv.appendChild(passwordGroup);

        // Error message
        const errorMsg = document.createElement('div');
        errorMsg.className = 'password-error';
        errorMsg.id = 'loginError';
        errorMsg.style.display = 'none';
        loginDiv.appendChild(errorMsg);

        // Submit button
        const submitBtn = document.createElement('button');
        submitBtn.className = 'btn btn-primary password-submit-btn';
        submitBtn.textContent = 'Unlock';
        submitBtn.addEventListener('click', () => this.handleLoginSubmit());
        loginDiv.appendChild(submitBtn);

        container.appendChild(loginDiv);

        // Focus input
        requestAnimationFrame(() => passwordInput.focus());
    },

    /**
     * Handle login form submission.
     */
    handleLoginSubmit() {
        const password = document.getElementById('loginPassword').value;
        const errorEl = document.getElementById('loginError');

        if (!password) {
            errorEl.textContent = 'Please enter your password.';
            errorEl.style.display = 'block';
            return;
        }

        errorEl.style.display = 'none';

        // Send to backend for verification
        if (App.isWebViewReady) {
            window.chrome.webview.postMessage(JSON.stringify({
                type: 'passwordManagerAuth',
                password: password
            }));
        }
    },

    /**
     * Handle authentication failure.
     * @param {string} message - Error message.
     */
    handleAuthFailure(message) {
        const errorEl = document.getElementById('loginError');
        if (errorEl) {
            errorEl.textContent = message || 'Incorrect password.';
            errorEl.style.display = 'block';
        }
    },

    /**
     * Handle authentication success and load data.
     * @param {Array} platforms - Decrypted platform data.
     */
    handleAuthSuccess(platforms) {
        this.isAuthenticated = true;
        this.platforms = platforms || [];
        this.selectedPlatformIndex = this.platforms.length > 0 ? 0 : -1;

        const container = document.querySelector('.password-panel');
        if (!container) return;

        this.renderMainScreen(container);
    },

    /**
     * Render main password manager screen.
     * @param {HTMLElement} container
     */
    renderMainScreen(container) {
        container.innerHTML = '';

        // Toolbar
        const toolbar = document.createElement('div');
        toolbar.className = 'password-toolbar';

        const title = document.createElement('span');
        title.className = 'password-toolbar-title';
        title.textContent = 'Password Manager';
        toolbar.appendChild(title);

        // Change password button
        const changePwdBtn = document.createElement('button');
        changePwdBtn.className = 'btn btn-secondary password-change-pwd-btn';
        changePwdBtn.textContent = 'Change Password';
        changePwdBtn.addEventListener('click', () => this.showChangePasswordDialog());
        toolbar.appendChild(changePwdBtn);

        // Add platform button
        const addPlatformBtn = document.createElement('button');
        addPlatformBtn.className = 'btn btn-primary password-add-platform-btn';
        addPlatformBtn.textContent = '+ Platform';
        addPlatformBtn.addEventListener('click', () => this.showAddPlatformDialog());
        toolbar.appendChild(addPlatformBtn);

        container.appendChild(toolbar);

        // Main content area (sidebar + detail)
        const mainArea = document.createElement('div');
        mainArea.className = 'password-main-area';

        // Platform sidebar
        const sidebar = document.createElement('div');
        sidebar.className = 'password-sidebar';
        this.renderPlatformList(sidebar);
        mainArea.appendChild(sidebar);

        // Account detail area
        const detail = document.createElement('div');
        detail.className = 'password-detail';
        if (this.selectedPlatformIndex >= 0 && this.selectedPlatformIndex < this.platforms.length) {
            this.renderAccountList(detail);
        } else {
            this.renderEmptyDetail(detail);
        }
        mainArea.appendChild(detail);

        container.appendChild(mainArea);
    },

    /**
     * Render platform list in sidebar.
     * @param {HTMLElement} sidebar
     */
    renderPlatformList(sidebar) {
        sidebar.innerHTML = '';

        if (this.platforms.length === 0) {
            const empty = document.createElement('div');
            empty.className = 'password-sidebar-empty';
            empty.textContent = 'No platforms added';
            sidebar.appendChild(empty);
            return;
        }

        this.platforms.forEach((platform, index) => {
            const item = document.createElement('div');
            item.className = 'password-platform-item' + (index === this.selectedPlatformIndex ? ' active' : '');

            const name = document.createElement('span');
            name.className = 'password-platform-name';
            name.textContent = platform.name;
            item.appendChild(name);

            const count = document.createElement('span');
            count.className = 'password-platform-count';
            count.textContent = platform.accounts.length;
            item.appendChild(count);

            // Delete button
            const deleteBtn = document.createElement('button');
            deleteBtn.className = 'password-platform-delete';
            deleteBtn.innerHTML = '&times;';
            deleteBtn.title = 'Delete platform';
            deleteBtn.addEventListener('click', (e) => {
                e.stopPropagation();
                this.deletePlatform(index);
            });
            item.appendChild(deleteBtn);

            item.addEventListener('click', () => {
                this.selectedPlatformIndex = index;
                this.refreshMainScreen();
            });

            sidebar.appendChild(item);
        });
    },

    /**
     * Render empty detail area.
     * @param {HTMLElement} detail
     */
    renderEmptyDetail(detail) {
        detail.innerHTML = '';

        const empty = document.createElement('div');
        empty.className = 'password-detail-empty';
        empty.textContent = this.platforms.length === 0
            ? 'Add a platform to get started'
            : 'Select a platform from the sidebar';
        detail.appendChild(empty);
    },

    /**
     * Render account list for selected platform.
     * @param {HTMLElement} detail
     */
    renderAccountList(detail) {
        detail.innerHTML = '';

        const platform = this.platforms[this.selectedPlatformIndex];
        if (!platform) {
            this.renderEmptyDetail(detail);
            return;
        }

        // Platform header
        const header = document.createElement('div');
        header.className = 'password-account-header';

        const platformName = document.createElement('h3');
        platformName.textContent = platform.name;
        header.appendChild(platformName);

        const addAccountBtn = document.createElement('button');
        addAccountBtn.className = 'btn btn-primary';
        addAccountBtn.textContent = '+ Account';
        addAccountBtn.addEventListener('click', () => this.showAddAccountDialog());
        header.appendChild(addAccountBtn);

        detail.appendChild(header);

        // Account list
        if (platform.accounts.length === 0) {
            const empty = document.createElement('div');
            empty.className = 'password-accounts-empty';
            empty.textContent = 'No accounts added for this platform';
            detail.appendChild(empty);
            return;
        }

        const accountList = document.createElement('div');
        accountList.className = 'password-account-list';

        platform.accounts.forEach((account, index) => {
            const card = document.createElement('div');
            card.className = 'password-account-card';

            // Username row
            const usernameRow = document.createElement('div');
            usernameRow.className = 'password-account-row';

            const usernameLabel = document.createElement('span');
            usernameLabel.className = 'password-account-label';
            usernameLabel.textContent = 'Username:';
            usernameRow.appendChild(usernameLabel);

            const usernameValue = document.createElement('span');
            usernameValue.className = 'password-account-value';
            usernameValue.textContent = account.username;
            usernameRow.appendChild(usernameValue);

            // Copy username button
            const copyUsernameBtn = document.createElement('button');
            copyUsernameBtn.className = 'password-copy-btn';
            copyUsernameBtn.textContent = 'Copy';
            copyUsernameBtn.addEventListener('click', () => {
                this.copyToClipboard(account.username);
                this.showCopyFeedback(copyUsernameBtn);
            });
            usernameRow.appendChild(copyUsernameBtn);

            card.appendChild(usernameRow);

            // Password row
            const passwordRow = document.createElement('div');
            passwordRow.className = 'password-account-row';

            const passwordLabel = document.createElement('span');
            passwordLabel.className = 'password-account-label';
            passwordLabel.textContent = 'Password:';
            passwordRow.appendChild(passwordLabel);

            const passwordValue = document.createElement('span');
            passwordValue.className = 'password-account-value password-hidden';
            passwordValue.textContent = '••••••••••••';
            passwordRow.appendChild(passwordValue);

            // Toggle password visibility
            const toggleBtn = document.createElement('button');
            toggleBtn.className = 'password-toggle-btn';
            toggleBtn.textContent = 'Show';
            toggleBtn.addEventListener('click', () => {
                if (passwordValue.classList.contains('password-hidden')) {
                    passwordValue.textContent = account.password;
                    passwordValue.classList.remove('password-hidden');
                    toggleBtn.textContent = 'Hide';
                } else {
                    passwordValue.textContent = '••••••••••••';
                    passwordValue.classList.add('password-hidden');
                    toggleBtn.textContent = 'Show';
                }
            });
            passwordRow.appendChild(toggleBtn);

            // Copy password button
            const copyPasswordBtn = document.createElement('button');
            copyPasswordBtn.className = 'password-copy-btn';
            copyPasswordBtn.textContent = 'Copy';
            copyPasswordBtn.addEventListener('click', () => {
                this.copyToClipboard(account.password);
                this.showCopyFeedback(copyPasswordBtn);
            });
            passwordRow.appendChild(copyPasswordBtn);

            card.appendChild(passwordRow);

            // Actions row
            const actionsRow = document.createElement('div');
            actionsRow.className = 'password-account-actions';

            const editBtn = document.createElement('button');
            editBtn.className = 'btn btn-secondary';
            editBtn.textContent = 'Edit';
            editBtn.addEventListener('click', () => this.showEditAccountDialog(index));
            actionsRow.appendChild(editBtn);

            const deleteBtn = document.createElement('button');
            deleteBtn.className = 'btn btn-danger';
            deleteBtn.textContent = 'Delete';
            deleteBtn.addEventListener('click', () => this.deleteAccount(index));
            actionsRow.appendChild(deleteBtn);

            card.appendChild(actionsRow);

            accountList.appendChild(card);
        });

        detail.appendChild(accountList);
    },

    /**
     * Refresh the main screen.
     */
    refreshMainScreen() {
        const container = document.querySelector('.password-panel');
        if (!container) return;
        this.renderMainScreen(container);
    },

    /**
     * Show add platform dialog.
     */
    showAddPlatformDialog() {
        const overlay = document.createElement('div');
        overlay.className = 'password-dialog-overlay';
        overlay.id = 'passwordDialogOverlay';

        const dialog = document.createElement('div');
        dialog.className = 'password-dialog';

        const header = document.createElement('div');
        header.className = 'password-dialog-header';

        const title = document.createElement('span');
        title.className = 'password-dialog-title';
        title.textContent = 'Add Platform';
        header.appendChild(title);

        const closeBtn = document.createElement('button');
        closeBtn.className = 'password-dialog-close';
        closeBtn.innerHTML = '&times;';
        closeBtn.addEventListener('click', () => this.closeDialog());
        header.appendChild(closeBtn);

        dialog.appendChild(header);

        // Platform name input
        const nameGroup = document.createElement('div');
        nameGroup.className = 'password-input-group';

        const nameLabel = document.createElement('label');
        nameLabel.textContent = 'Platform Name';
        nameGroup.appendChild(nameLabel);

        const nameInput = document.createElement('input');
        nameInput.type = 'text';
        nameInput.id = 'dialogPlatformName';
        nameInput.placeholder = 'e.g., Google, GitHub, Netflix';
        nameGroup.appendChild(nameInput);

        dialog.appendChild(nameGroup);

        // Error message
        const errorMsg = document.createElement('div');
        errorMsg.className = 'password-error';
        errorMsg.id = 'dialogError';
        errorMsg.style.display = 'none';
        dialog.appendChild(errorMsg);

        // Buttons
        const buttons = document.createElement('div');
        buttons.className = 'password-dialog-buttons';

        const cancelBtn = document.createElement('button');
        cancelBtn.className = 'btn btn-secondary';
        cancelBtn.textContent = 'Cancel';
        cancelBtn.addEventListener('click', () => this.closeDialog());
        buttons.appendChild(cancelBtn);

        const addBtn = document.createElement('button');
        addBtn.className = 'btn btn-primary';
        addBtn.textContent = 'Add';
        addBtn.addEventListener('click', () => this.handleAddPlatform());
        buttons.appendChild(addBtn);

        dialog.appendChild(buttons);

        overlay.appendChild(dialog);
        document.body.appendChild(overlay);

        // Focus input
        requestAnimationFrame(() => nameInput.focus());
    },

    /**
     * Handle add platform submission.
     */
    handleAddPlatform() {
        const name = document.getElementById('dialogPlatformName').value.trim();
        const errorEl = document.getElementById('dialogError');

        if (!name) {
            errorEl.textContent = 'Please enter a platform name.';
            errorEl.style.display = 'block';
            return;
        }

        // Check for duplicate
        if (this.platforms.some(p => p.name.toLowerCase() === name.toLowerCase())) {
            errorEl.textContent = 'This platform already exists.';
            errorEl.style.display = 'block';
            return;
        }

        errorEl.style.display = 'none';

        // Add platform
        this.platforms.push({
            name: name,
            accounts: []
        });

        this.selectedPlatformIndex = this.platforms.length - 1;
        this.saveData();
        this.closeDialog();
        this.refreshMainScreen();
    },

    /**
     * Delete a platform.
     * @param {number} index - Platform index.
     */
    deletePlatform(index) {
        if (index < 0 || index >= this.platforms.length) return;

        const platform = this.platforms[index];
        if (!confirm(`Delete platform "${platform.name}" and all its accounts?`)) {
            return;
        }

        this.platforms.splice(index, 1);

        if (this.selectedPlatformIndex >= this.platforms.length) {
            this.selectedPlatformIndex = this.platforms.length - 1;
        }

        this.saveData();
        this.refreshMainScreen();
    },

    /**
     * Show add account dialog.
     */
    showAddAccountDialog() {
        if (this.selectedPlatformIndex < 0) return;

        const overlay = document.createElement('div');
        overlay.className = 'password-dialog-overlay';
        overlay.id = 'passwordDialogOverlay';

        const dialog = document.createElement('div');
        dialog.className = 'password-dialog password-dialog-wide';

        const header = document.createElement('div');
        header.className = 'password-dialog-header';

        const title = document.createElement('span');
        title.className = 'password-dialog-title';
        title.textContent = 'Add Account';
        header.appendChild(title);

        const closeBtn = document.createElement('button');
        closeBtn.className = 'password-dialog-close';
        closeBtn.innerHTML = '&times;';
        closeBtn.addEventListener('click', () => this.closeDialog());
        header.appendChild(closeBtn);

        dialog.appendChild(header);

        // Username input
        const usernameGroup = document.createElement('div');
        usernameGroup.className = 'password-input-group';

        const usernameLabel = document.createElement('label');
        usernameLabel.textContent = 'Username / Email';
        usernameGroup.appendChild(usernameLabel);

        const usernameInput = document.createElement('input');
        usernameInput.type = 'text';
        usernameInput.id = 'dialogAccountUsername';
        usernameInput.placeholder = 'Enter username or email';
        usernameGroup.appendChild(usernameInput);

        dialog.appendChild(usernameGroup);

        // Password input with generator
        const passwordGroup = document.createElement('div');
        passwordGroup.className = 'password-input-group';

        const passwordLabel = document.createElement('label');
        passwordLabel.textContent = 'Password';
        passwordGroup.appendChild(passwordLabel);

        const passwordInputRow = document.createElement('div');
        passwordInputRow.className = 'password-input-row';

        const passwordInput = document.createElement('input');
        passwordInput.type = 'password';
        passwordInput.id = 'dialogAccountPassword';
        passwordInput.placeholder = 'Enter password';
        passwordInputRow.appendChild(passwordInput);

        const generateBtn = document.createElement('button');
        generateBtn.className = 'btn btn-secondary';
        generateBtn.textContent = 'Generate';
        generateBtn.addEventListener('click', () => this.showGeneratorDialog());
        passwordInputRow.appendChild(generateBtn);

        passwordGroup.appendChild(passwordInputRow);

        dialog.appendChild(passwordGroup);

        // Error message
        const errorMsg = document.createElement('div');
        errorMsg.className = 'password-error';
        errorMsg.id = 'dialogError';
        errorMsg.style.display = 'none';
        dialog.appendChild(errorMsg);

        // Buttons
        const buttons = document.createElement('div');
        buttons.className = 'password-dialog-buttons';

        const cancelBtn = document.createElement('button');
        cancelBtn.className = 'btn btn-secondary';
        cancelBtn.textContent = 'Cancel';
        cancelBtn.addEventListener('click', () => this.closeDialog());
        buttons.appendChild(cancelBtn);

        const addBtn = document.createElement('button');
        addBtn.className = 'btn btn-primary';
        addBtn.textContent = 'Add';
        addBtn.addEventListener('click', () => this.handleAddAccount());
        buttons.appendChild(addBtn);

        dialog.appendChild(buttons);

        overlay.appendChild(dialog);
        document.body.appendChild(overlay);

        // Focus input
        requestAnimationFrame(() => usernameInput.focus());
    },

    /**
     * Handle add account submission.
     */
    handleAddAccount() {
        const username = document.getElementById('dialogAccountUsername').value.trim();
        const password = document.getElementById('dialogAccountPassword').value;
        const errorEl = document.getElementById('dialogError');

        if (!username) {
            errorEl.textContent = 'Please enter a username.';
            errorEl.style.display = 'block';
            return;
        }

        if (!password) {
            errorEl.textContent = 'Please enter a password.';
            errorEl.style.display = 'block';
            return;
        }

        errorEl.style.display = 'none';

        // Add account to selected platform
        const platform = this.platforms[this.selectedPlatformIndex];
        platform.accounts.push({
            username: username,
            password: password
        });

        this.saveData();
        this.closeDialog();
        this.refreshMainScreen();
    },

    /**
     * Show edit account dialog.
     * @param {number} accountIndex - Account index.
     */
    showEditAccountDialog(accountIndex) {
        const platform = this.platforms[this.selectedPlatformIndex];
        if (!platform || accountIndex < 0 || accountIndex >= platform.accounts.length) return;

        const account = platform.accounts[accountIndex];

        const overlay = document.createElement('div');
        overlay.className = 'password-dialog-overlay';
        overlay.id = 'passwordDialogOverlay';

        const dialog = document.createElement('div');
        dialog.className = 'password-dialog password-dialog-wide';

        const header = document.createElement('div');
        header.className = 'password-dialog-header';

        const title = document.createElement('span');
        title.className = 'password-dialog-title';
        title.textContent = 'Edit Account';
        header.appendChild(title);

        const closeBtn = document.createElement('button');
        closeBtn.className = 'password-dialog-close';
        closeBtn.innerHTML = '&times;';
        closeBtn.addEventListener('click', () => this.closeDialog());
        header.appendChild(closeBtn);

        dialog.appendChild(header);

        // Username input
        const usernameGroup = document.createElement('div');
        usernameGroup.className = 'password-input-group';

        const usernameLabel = document.createElement('label');
        usernameLabel.textContent = 'Username / Email';
        usernameGroup.appendChild(usernameLabel);

        const usernameInput = document.createElement('input');
        usernameInput.type = 'text';
        usernameInput.id = 'dialogAccountUsername';
        usernameInput.value = account.username;
        usernameGroup.appendChild(usernameInput);

        dialog.appendChild(usernameGroup);

        // Password input with generator
        const passwordGroup = document.createElement('div');
        passwordGroup.className = 'password-input-group';

        const passwordLabel = document.createElement('label');
        passwordLabel.textContent = 'Password';
        passwordGroup.appendChild(passwordLabel);

        const passwordInputRow = document.createElement('div');
        passwordInputRow.className = 'password-input-row';

        const passwordInput = document.createElement('input');
        passwordInput.type = 'password';
        passwordInput.id = 'dialogAccountPassword';
        passwordInput.value = account.password;
        passwordInputRow.appendChild(passwordInput);

        const generateBtn = document.createElement('button');
        generateBtn.className = 'btn btn-secondary';
        generateBtn.textContent = 'Generate';
        generateBtn.addEventListener('click', () => this.showGeneratorDialog());
        passwordInputRow.appendChild(generateBtn);

        passwordGroup.appendChild(passwordInputRow);

        dialog.appendChild(passwordGroup);

        // Error message
        const errorMsg = document.createElement('div');
        errorMsg.className = 'password-error';
        errorMsg.id = 'dialogError';
        errorMsg.style.display = 'none';
        dialog.appendChild(errorMsg);

        // Buttons
        const buttons = document.createElement('div');
        buttons.className = 'password-dialog-buttons';

        const cancelBtn = document.createElement('button');
        cancelBtn.className = 'btn btn-secondary';
        cancelBtn.textContent = 'Cancel';
        cancelBtn.addEventListener('click', () => this.closeDialog());
        buttons.appendChild(cancelBtn);

        const saveBtn = document.createElement('button');
        saveBtn.className = 'btn btn-primary';
        saveBtn.textContent = 'Save';
        saveBtn.addEventListener('click', () => this.handleEditAccount(accountIndex));
        buttons.appendChild(saveBtn);

        dialog.appendChild(buttons);

        overlay.appendChild(dialog);
        document.body.appendChild(overlay);

        // Focus input
        requestAnimationFrame(() => usernameInput.focus());
    },

    /**
     * Handle edit account submission.
     * @param {number} accountIndex - Account index.
     */
    handleEditAccount(accountIndex) {
        const username = document.getElementById('dialogAccountUsername').value.trim();
        const password = document.getElementById('dialogAccountPassword').value;
        const errorEl = document.getElementById('dialogError');

        if (!username) {
            errorEl.textContent = 'Please enter a username.';
            errorEl.style.display = 'block';
            return;
        }

        if (!password) {
            errorEl.textContent = 'Please enter a password.';
            errorEl.style.display = 'block';
            return;
        }

        errorEl.style.display = 'none';

        // Update account
        const platform = this.platforms[this.selectedPlatformIndex];
        platform.accounts[accountIndex] = {
            username: username,
            password: password
        };

        this.saveData();
        this.closeDialog();
        this.refreshMainScreen();
    },

    /**
     * Delete an account.
     * @param {number} accountIndex - Account index.
     */
    deleteAccount(accountIndex) {
        const platform = this.platforms[this.selectedPlatformIndex];
        if (!platform || accountIndex < 0 || accountIndex >= platform.accounts.length) return;

        const account = platform.accounts[accountIndex];
        if (!confirm(`Delete account "${account.username}"?`)) {
            return;
        }

        platform.accounts.splice(accountIndex, 1);
        this.saveData();
        this.refreshMainScreen();
    },

    /**
     * Show password generator dialog.
     */
    showGeneratorDialog() {
        const overlay = document.createElement('div');
        overlay.className = 'password-dialog-overlay';
        overlay.id = 'passwordGeneratorOverlay';

        const dialog = document.createElement('div');
        dialog.className = 'password-dialog';

        const header = document.createElement('div');
        header.className = 'password-dialog-header';

        const title = document.createElement('span');
        title.className = 'password-dialog-title';
        title.textContent = 'Generate Password';
        header.appendChild(title);

        const closeBtn = document.createElement('button');
        closeBtn.className = 'password-dialog-close';
        closeBtn.innerHTML = '&times;';
        closeBtn.addEventListener('click', () => this.closeGeneratorDialog());
        header.appendChild(closeBtn);

        dialog.appendChild(header);

        // Length input
        const lengthGroup = document.createElement('div');
        lengthGroup.className = 'password-input-group';

        const lengthLabel = document.createElement('label');
        lengthLabel.textContent = 'Password Length';
        lengthGroup.appendChild(lengthLabel);

        const lengthRow = document.createElement('div');
        lengthRow.className = 'password-input-row';

        const lengthInput = document.createElement('input');
        lengthInput.type = 'number';
        lengthInput.id = 'generatorLength';
        lengthInput.min = '4';
        lengthInput.max = '64';
        lengthInput.value = this.generatorSettings.length.toString();
        lengthRow.appendChild(lengthInput);

        lengthGroup.appendChild(lengthRow);
        dialog.appendChild(lengthGroup);

        // Special characters checkbox
        const checkboxGroup = document.createElement('div');
        checkboxGroup.className = 'password-checkbox-group';

        const checkbox = document.createElement('input');
        checkbox.type = 'checkbox';
        checkbox.id = 'generatorSpecialChars';
        checkbox.checked = this.generatorSettings.useSpecialChars;
        checkboxGroup.appendChild(checkbox);

        const checkboxLabel = document.createElement('label');
        checkboxLabel.htmlFor = 'generatorSpecialChars';
        checkboxLabel.textContent = 'Include special characters (!@#$%^&*)';
        checkboxGroup.appendChild(checkboxLabel);

        dialog.appendChild(checkboxGroup);

        // Generated password preview
        const previewGroup = document.createElement('div');
        previewGroup.className = 'password-input-group';

        const previewLabel = document.createElement('label');
        previewLabel.textContent = 'Generated Password';
        previewGroup.appendChild(previewLabel);

        const previewInput = document.createElement('input');
        previewInput.type = 'text';
        previewInput.id = 'generatorPreview';
        previewInput.readOnly = true;
        previewGroup.appendChild(previewInput);

        dialog.appendChild(previewGroup);

        // Generate button
        const generateBtn = document.createElement('button');
        generateBtn.className = 'btn btn-secondary password-generate-btn';
        generateBtn.textContent = 'Generate New';
        generateBtn.addEventListener('click', () => this.generateAndPreview());
        dialog.appendChild(generateBtn);

        // Buttons
        const buttons = document.createElement('div');
        buttons.className = 'password-dialog-buttons';

        const cancelBtn = document.createElement('button');
        cancelBtn.className = 'btn btn-secondary';
        cancelBtn.textContent = 'Cancel';
        cancelBtn.addEventListener('click', () => this.closeGeneratorDialog());
        buttons.appendChild(cancelBtn);

        const useBtn = document.createElement('button');
        useBtn.className = 'btn btn-primary';
        useBtn.textContent = 'Use Password';
        useBtn.addEventListener('click', () => this.useGeneratedPassword());
        buttons.appendChild(useBtn);

        dialog.appendChild(buttons);

        overlay.appendChild(dialog);
        document.body.appendChild(overlay);

        // Generate initial password
        this.generateAndPreview();
    },

    /**
     * Generate a password and show it in the preview.
     */
    generateAndPreview() {
        const length = parseInt(document.getElementById('generatorLength').value) || 14;
        const useSpecialChars = document.getElementById('generatorSpecialChars').checked;

        this.generatorSettings.length = length;
        this.generatorSettings.useSpecialChars = useSpecialChars;

        const password = this.generatePassword(length, useSpecialChars);
        document.getElementById('generatorPreview').value = password;
    },

    /**
     * Use the generated password and close the generator.
     */
    useGeneratedPassword() {
        const password = document.getElementById('generatorPreview').value;
        const passwordInput = document.getElementById('dialogAccountPassword');

        if (passwordInput) {
            passwordInput.value = password;
            passwordInput.type = 'text'; // Show the generated password
        }

        // Copy to clipboard
        this.copyToClipboard(password);

        this.closeGeneratorDialog();

        // Show feedback
        const container = document.querySelector('.password-dialog');
        if (container) {
            const feedback = document.createElement('div');
            feedback.className = 'password-copy-feedback';
            feedback.textContent = 'Password copied to clipboard!';
            container.appendChild(feedback);

            setTimeout(() => feedback.remove(), 2000);
        }
    },

    /**
     * Close the generator dialog.
     */
    closeGeneratorDialog() {
        const overlay = document.getElementById('passwordGeneratorOverlay');
        if (overlay) {
            overlay.remove();
        }
    },

    /**
     * Generate a random password.
     * @param {number} length - Password length.
     * @param {boolean} useSpecialChars - Whether to include special characters.
     * @returns {string} The generated password.
     */
    generatePassword(length, useSpecialChars) {
        const lowercase = 'abcdefghijklmnopqrstuvwxyz';
        const uppercase = 'ABCDEFGHIJKLMNOPQRSTUVWXYZ';
        const digits = '0123456789';
        const special = '!@#$%^&*()_+-=[]{}|;:,.<>?';

        let chars = lowercase + uppercase + digits;
        if (useSpecialChars) {
            chars += special;
        }

        let password = '';
        const array = new Uint32Array(length);
        crypto.getRandomValues(array);

        for (let i = 0; i < length; i++) {
            password += chars[array[i] % chars.length];
        }

        return password;
    },

    /**
     * Show change password dialog.
     */
    showChangePasswordDialog() {
        const overlay = document.createElement('div');
        overlay.className = 'password-dialog-overlay';
        overlay.id = 'passwordDialogOverlay';

        const dialog = document.createElement('div');
        dialog.className = 'password-dialog';

        const header = document.createElement('div');
        header.className = 'password-dialog-header';

        const title = document.createElement('span');
        title.className = 'password-dialog-title';
        title.textContent = 'Change Master Password';
        header.appendChild(title);

        const closeBtn = document.createElement('button');
        closeBtn.className = 'password-dialog-close';
        closeBtn.innerHTML = '&times;';
        closeBtn.addEventListener('click', () => this.closeDialog());
        header.appendChild(closeBtn);

        dialog.appendChild(header);

        // Current password input
        const currentGroup = document.createElement('div');
        currentGroup.className = 'password-input-group';

        const currentLabel = document.createElement('label');
        currentLabel.textContent = 'Current Password';
        currentGroup.appendChild(currentLabel);

        const currentInput = document.createElement('input');
        currentInput.type = 'password';
        currentInput.id = 'dialogCurrentPassword';
        currentInput.placeholder = 'Enter current password';
        currentGroup.appendChild(currentInput);

        dialog.appendChild(currentGroup);

        // New password input
        const newGroup = document.createElement('div');
        newGroup.className = 'password-input-group';

        const newLabel = document.createElement('label');
        newLabel.textContent = 'New Password';
        newGroup.appendChild(newLabel);

        const newInput = document.createElement('input');
        newInput.type = 'password';
        newInput.id = 'dialogNewPassword';
        newInput.placeholder = 'Enter new password';
        newGroup.appendChild(newInput);

        dialog.appendChild(newGroup);

        // Confirm new password input
        const confirmGroup = document.createElement('div');
        confirmGroup.className = 'password-input-group';

        const confirmLabel = document.createElement('label');
        confirmLabel.textContent = 'Confirm New Password';
        confirmGroup.appendChild(confirmLabel);

        const confirmInput = document.createElement('input');
        confirmInput.type = 'password';
        confirmInput.id = 'dialogConfirmPassword';
        confirmInput.placeholder = 'Confirm new password';
        confirmGroup.appendChild(confirmInput);

        dialog.appendChild(confirmGroup);

        // Error message
        const errorMsg = document.createElement('div');
        errorMsg.className = 'password-error';
        errorMsg.id = 'dialogError';
        errorMsg.style.display = 'none';
        dialog.appendChild(errorMsg);

        // Buttons
        const buttons = document.createElement('div');
        buttons.className = 'password-dialog-buttons';

        const cancelBtn = document.createElement('button');
        cancelBtn.className = 'btn btn-secondary';
        cancelBtn.textContent = 'Cancel';
        cancelBtn.addEventListener('click', () => this.closeDialog());
        buttons.appendChild(cancelBtn);

        const changeBtn = document.createElement('button');
        changeBtn.className = 'btn btn-primary';
        changeBtn.textContent = 'Change Password';
        changeBtn.addEventListener('click', () => this.handleChangePassword());
        buttons.appendChild(changeBtn);

        dialog.appendChild(buttons);

        overlay.appendChild(dialog);
        document.body.appendChild(overlay);

        // Focus input
        requestAnimationFrame(() => currentInput.focus());
    },

    /**
     * Handle change password submission.
     */
    handleChangePassword() {
        const current = document.getElementById('dialogCurrentPassword').value;
        const newPwd = document.getElementById('dialogNewPassword').value;
        const confirm = document.getElementById('dialogConfirmPassword').value;
        const errorEl = document.getElementById('dialogError');

        if (!current) {
            errorEl.textContent = 'Please enter your current password.';
            errorEl.style.display = 'block';
            return;
        }

        if (!newPwd) {
            errorEl.textContent = 'Please enter a new password.';
            errorEl.style.display = 'block';
            return;
        }

        if (newPwd.length < 4) {
            errorEl.textContent = 'New password must be at least 4 characters.';
            errorEl.style.display = 'block';
            return;
        }

        if (newPwd !== confirm) {
            errorEl.textContent = 'New passwords do not match.';
            errorEl.style.display = 'block';
            return;
        }

        errorEl.style.display = 'none';

        // Send to backend
        if (App.isWebViewReady) {
            window.chrome.webview.postMessage(JSON.stringify({
                type: 'passwordManagerChangePassword',
                currentPassword: current,
                newPassword: newPwd
            }));
        }
    },

    /**
     * Handle change password success.
     */
    handleChangePasswordSuccess() {
        this.closeDialog();

        // Show success message
        const container = document.querySelector('.password-panel');
        if (container) {
            const toolbar = container.querySelector('.password-toolbar');
            if (toolbar) {
                const msg = document.createElement('span');
                msg.className = 'password-success-msg';
                msg.textContent = 'Password changed successfully!';
                toolbar.appendChild(msg);

                setTimeout(() => msg.remove(), 3000);
            }
        }
    },

    /**
     * Handle change password failure.
     * @param {string} message - Error message.
     */
    handleChangePasswordFailure(message) {
        const errorEl = document.getElementById('dialogError');
        if (errorEl) {
            errorEl.textContent = message || 'Failed to change password.';
            errorEl.style.display = 'block';
        }
    },

    /**
     * Close the dialog overlay.
     */
    closeDialog() {
        const overlay = document.getElementById('passwordDialogOverlay');
        if (overlay) {
            overlay.remove();
        }
    },

    /**
     * Save data to backend.
     */
    saveData() {
        if (App.isWebViewReady) {
            window.chrome.webview.postMessage(JSON.stringify({
                type: 'passwordManagerSaveData',
                platforms: this.platforms
            }));
        }
    },

    /**
     * Copy text to clipboard.
     * @param {string} text - Text to copy.
     */
    copyToClipboard(text) {
        if (navigator.clipboard) {
            navigator.clipboard.writeText(text).catch(() => {
                // Fallback for older browsers
                this.fallbackCopyToClipboard(text);
            });
        } else {
            this.fallbackCopyToClipboard(text);
        }
    },

    /**
     * Fallback clipboard copy using textarea.
     * @param {string} text - Text to copy.
     */
    fallbackCopyToClipboard(text) {
        const textarea = document.createElement('textarea');
        textarea.value = text;
        textarea.style.position = 'fixed';
        textarea.style.opacity = '0';
        document.body.appendChild(textarea);
        textarea.select();

        try {
            document.execCommand('copy');
        } catch (err) {
            console.error('Failed to copy:', err);
        }

        document.body.removeChild(textarea);
    },

    /**
     * Show copy feedback on button.
     * @param {HTMLElement} btn - The button element.
     */
    showCopyFeedback(btn) {
        const originalText = btn.textContent;
        btn.textContent = 'Copied!';
        btn.classList.add('copied');

        setTimeout(() => {
            btn.textContent = originalText;
            btn.classList.remove('copied');
        }, 1500)
    }
};
