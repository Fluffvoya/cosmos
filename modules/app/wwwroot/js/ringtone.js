/**
 * ringtone.js - Ringtone tab logic.
 * Manages active ringtones received from cm-script PlayRingtone requests.
 * Each ringtone is displayed as a horizontal bar with a close button.
 * Closing a ringtone stops its audio playback and removes the bar.
 */

const Ringtone = {
    /** ID of the Ringtone tab (if open) */
    ringtoneTabId: null,

    /** Active ringtones: array of { id, filePath, label, audioElement } */
    activeRingtones: [],

    /** Counter for generating unique ringtone IDs */
    _idCounter: 0,

    /**
     * Open (or focus) the Ringtone tab.
     */
    openRingtoneTab() {
        if (this.ringtoneTabId) {
            const existing = Tabs.tabs.find(t => t.id === this.ringtoneTabId);
            if (existing) {
                Tabs.selectTab(this.ringtoneTabId);
                return;
            }
            this.ringtoneTabId = null;
        }

        const tab = Tabs.addTab({
            title: 'Ringtone',
            contentType: 'Ringtone',
            icon: '🔔' // bell emoji
        });
        this.ringtoneTabId = tab.id;
    },

    /**
     * Render the Ringtone tab panel.
     */
    renderRingtonePanel(container) {
        container.innerHTML = '';
        container.className = 'tab-panel active ringtone-panel';

        // Toolbar
        const toolbar = document.createElement('div');
        toolbar.className = 'ringtone-toolbar';

        const title = document.createElement('span');
        title.className = 'ringtone-toolbar-title';
        title.textContent = 'Active Ringtones';
        toolbar.appendChild(title);

        container.appendChild(toolbar);

        // Ringtones list
        const list = document.createElement('div');
        list.className = 'ringtone-list';
        list.id = 'ringtoneList';

        if (this.activeRingtones.length === 0) {
            const empty = document.createElement('div');
            empty.className = 'ringtone-empty';
            empty.textContent = 'No active ringtones. Use PlayRingtone() from cm-script to play a ringtone.';
            list.appendChild(empty);
        } else {
            this.activeRingtones.forEach(ringtone => {
                list.appendChild(this.createRingtoneBar(ringtone));
            });
        }

        container.appendChild(list);
    },

    /**
     * Create a horizontal bar element for a ringtone.
     * @param {object} ringtone - The ringtone object { id, filePath, label, audioElement }.
     * @returns {HTMLElement} The bar element.
     */
    createRingtoneBar(ringtone) {
        const bar = document.createElement('div');
        bar.className = 'ringtone-bar';
        bar.dataset.ringtoneId = ringtone.id;

        // Icon
        const icon = document.createElement('span');
        icon.className = 'ringtone-bar-icon';
        icon.textContent = '🔔'; // bell emoji
        bar.appendChild(icon);

        // Label (file name)
        const label = document.createElement('span');
        label.className = 'ringtone-bar-label';
        label.textContent = ringtone.label;
        label.title = ringtone.filePath;
        bar.appendChild(label);

        // Close button
        const closeBtn = document.createElement('button');
        closeBtn.className = 'ringtone-bar-close';
        closeBtn.innerHTML = '&times;';
        closeBtn.title = 'Stop and close';
        closeBtn.addEventListener('click', () => {
            this.stopRingtone(ringtone.id);
        });
        bar.appendChild(closeBtn);

        return bar;
    },

    /**
     * Play a ringtone from a URL.
     * Creates an Audio element, adds to active ringtones, and renders.
     * @param {string} filePath - Original file path (for display label).
     * @param {string} audioUrl - URL to the audio file (virtual host URL).
     */
    playRingtone(filePath, audioUrl) {
        const id = 'ring_' + (++this._idCounter) + '_' + Date.now();
        const label = filePath.split(/[/\\]/).pop() || filePath;

        // Create audio element using the virtual host URL
        let audioElement;
        try {
            audioElement = new Audio(audioUrl);
            audioElement.loop = true;
            audioElement.play().catch(err => {
                console.warn('Failed to play ringtone:', err);
            });
        } catch (err) {
            console.warn('Failed to create audio element:', err);
            audioElement = null;
        }

        const ringtone = {
            id: id,
            filePath: filePath,
            label: label,
            audioElement: audioElement
        };

        this.activeRingtones.push(ringtone);

        // Auto-open the Ringtone tab
        this.openRingtoneTab();

        // If the tab is currently visible, refresh the panel
        this.refreshPanel();
    },

    /**
     * Stop and remove a ringtone by ID.
     * @param {string} id - The ringtone ID to stop.
     */
    stopRingtone(id) {
        const index = this.activeRingtones.findIndex(r => r.id === id);
        if (index === -1) return;

        const ringtone = this.activeRingtones[index];

        // Stop audio playback
        if (ringtone.audioElement) {
            ringtone.audioElement.pause();
            ringtone.audioElement.src = '';
            ringtone.audioElement = null;
        }

        // Remove from array
        this.activeRingtones.splice(index, 1);

        // Remove bar from DOM
        const bar = document.querySelector(`.ringtone-bar[data-ringtone-id="${id}"]`);
        if (bar) {
            bar.remove();
        }

        // If no more ringtones, show empty message
        if (this.activeRingtones.length === 0) {
            this.refreshPanel();
        }
    },

    /**
     * Handle a ringtonePlay message from the C# backend.
     * @param {object} data - The message data with filePath and audioUrl properties.
     */
    handlePlayRingtone(data) {
        const filePath = data.filePath || '';
        const audioUrl = data.audioUrl || '';
        console.log('Ringtone: received play request', { filePath, audioUrl });
        if (!audioUrl) {
            console.warn('Ringtone: received play request with no audioUrl');
            return;
        }
        this.playRingtone(filePath, audioUrl);
    },

    /**
     * Refresh the Ringtone panel if the tab is currently visible.
     */
    refreshPanel() {
        if (this.ringtoneTabId && Tabs.activeTabId === this.ringtoneTabId) {
            const contentArea = document.getElementById('tabContent');
            if (contentArea && contentArea.firstChild) {
                this.renderRingtonePanel(contentArea.firstChild);
            }
        }
    }
};
