/**
 * scheduler.js - Scheduler tab logic.
 * Manages {time -> cm-script path} pairs with recurrence options
 * and communicates with the backend to persist and execute scheduled tasks.
 */

const Scheduler = {
    /** ID of the Scheduler tab (if open) */
    schedulerTabId: null,

    /** Local copy of scheduled tasks (synced from backend settings) */
    tasks: [],

    /** Day-of-week labels */
    DAY_LABELS: ['Sun', 'Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat'],

    /**
     * Open (or focus) the Scheduler tab.
     */
    openSchedulerTab() {
        if (this.schedulerTabId) {
            const existing = Tabs.tabs.find(t => t.id === this.schedulerTabId);
            if (existing) {
                Tabs.selectTab(this.schedulerTabId);
                return;
            }
            this.schedulerTabId = null;
        }

        const tab = Tabs.addTab({
            title: 'Scheduler',
            contentType: 'Scheduler',
            icon: '\u23F0' // alarm clock emoji
        });
        this.schedulerTabId = tab.id;
    },

    /**
     * Render the Scheduler tab panel.
     */
    renderSchedulerPanel(container) {
        container.innerHTML = '';
        container.className = 'tab-panel active scheduler-panel';

        // ©¤©¤ Toolbar ©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤
        const toolbar = document.createElement('div');
        toolbar.className = 'scheduler-toolbar';

        const title = document.createElement('span');
        title.className = 'scheduler-toolbar-title';
        title.textContent = 'Scheduled Tasks';
        toolbar.appendChild(title);

        const addBtn = document.createElement('button');
        addBtn.className = 'btn btn-primary scheduler-add-btn';
        addBtn.textContent = '+ Add Task';
        addBtn.addEventListener('click', () => this.addTask());
        toolbar.appendChild(addBtn);

        container.appendChild(toolbar);

        // ©¤©¤ Table ©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤
        const tableWrapper = document.createElement('div');
        tableWrapper.className = 'scheduler-table-wrapper';

        const table = document.createElement('table');
        table.className = 'scheduler-table';

        const thead = document.createElement('thead');
        thead.innerHTML = '<tr>' +
            '<th class="scheduler-col-enabled">Enabled</th>' +
            '<th class="scheduler-col-time">Time</th>' +
            '<th class="scheduler-col-recurrence">Recurrence</th>' +
            '<th class="scheduler-col-script">cm-script Path</th>' +
            '<th class="scheduler-col-status">Last Status</th>' +
            '<th class="scheduler-col-actions">Actions</th>' +
            '</tr>';
        table.appendChild(thead);

        const tbody = document.createElement('tbody');
        tbody.id = 'schedulerTableBody';

        if (this.tasks.length === 0) {
            const tr = document.createElement('tr');
            tr.className = 'scheduler-empty-row';
            tr.innerHTML = '<td colspan="6">No scheduled tasks. Click "+ Add Task" to create one.</td>';
            tbody.appendChild(tr);
        } else {
            this.tasks.forEach((task, index) => {
                tbody.appendChild(this.createTaskRow(task, index));
            });
        }

        table.appendChild(tbody);
        tableWrapper.appendChild(table);
        container.appendChild(tableWrapper);
    },

    /**
     * Create a table row for a scheduled task.
     */
    createTaskRow(task, index) {
        const tr = document.createElement('tr');
        tr.className = 'scheduler-row' + (task.enabled ? '' : ' scheduler-row-disabled');
        tr.dataset.index = index;

        // ©¤©¤ Enabled checkbox ©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤
        const tdEnabled = document.createElement('td');
        tdEnabled.className = 'scheduler-col-enabled';
        const checkbox = document.createElement('input');
        checkbox.type = 'checkbox';
        checkbox.checked = task.enabled !== false;
        checkbox.addEventListener('change', () => {
            task.enabled = checkbox.checked;
            tr.classList.toggle('scheduler-row-disabled', !task.enabled);
            this.saveTasks();
        });
        tdEnabled.appendChild(checkbox);
        tr.appendChild(tdEnabled);

        // ©¤©¤ Time input ©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤
        const tdTime = document.createElement('td');
        tdTime.className = 'scheduler-col-time';
        const timeInput = document.createElement('input');
        timeInput.type = 'time';
        timeInput.value = task.time || '00:00';
        timeInput.className = 'scheduler-time-input';
        timeInput.addEventListener('change', () => {
            task.time = timeInput.value;
            this.saveTasks();
        });
        tdTime.appendChild(timeInput);
        tr.appendChild(tdTime);

        // ©¤©¤ Recurrence ©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤
        const tdRecurrence = document.createElement('td');
        tdRecurrence.className = 'scheduler-col-recurrence';

        const recurrenceGroup = document.createElement('div');
        recurrenceGroup.className = 'scheduler-recurrence-group';

        // Recurrence type selector
        const recSelect = document.createElement('select');
        recSelect.className = 'scheduler-recurrence-select';
        ['once', 'daily', 'weekly'].forEach(r => {
            const opt = document.createElement('option');
            opt.value = r;
            opt.textContent = r.charAt(0).toUpperCase() + r.slice(1);
            if ((task.recurrence || 'daily') === r) opt.selected = true;
            recSelect.appendChild(opt);
        });

        // Days container (for weekly)
        const daysContainer = document.createElement('div');
        daysContainer.className = 'scheduler-days-container';
        daysContainer.style.display = (task.recurrence === 'weekly') ? 'flex' : 'none';

        const days = task.days || [];
        for (let d = 0; d < 7; d++) {
            const dayBtn = document.createElement('button');
            dayBtn.type = 'button';
            dayBtn.className = 'scheduler-day-btn' + (days.includes(d) ? ' active' : '');
            dayBtn.textContent = this.DAY_LABELS[d];
            dayBtn.dataset.day = d;
            dayBtn.addEventListener('click', () => {
                const idx = task.days.indexOf(d);
                if (idx >= 0) {
                    task.days.splice(idx, 1);
                    dayBtn.classList.remove('active');
                } else {
                    task.days.push(d);
                    dayBtn.classList.add('active');
                }
                this.saveTasks();
            });
            daysContainer.appendChild(dayBtn);
        }

        // Once date container
        const onceContainer = document.createElement('div');
        onceContainer.className = 'scheduler-once-container';
        onceContainer.style.display = (task.recurrence === 'once') ? 'flex' : 'none';

        const onceDateInput = document.createElement('input');
        onceDateInput.type = 'date';
        onceDateInput.value = task.onceDate || '';
        onceDateInput.className = 'scheduler-once-date';
        onceDateInput.title = 'Leave empty to run at first matching time';
        onceDateInput.addEventListener('change', () => {
            task.onceDate = onceDateInput.value;
            this.saveTasks();
        });
        onceContainer.appendChild(onceDateInput);

        recSelect.addEventListener('change', () => {
            task.recurrence = recSelect.value;
            daysContainer.style.display = (task.recurrence === 'weekly') ? 'flex' : 'none';
            onceContainer.style.display = (task.recurrence === 'once') ? 'flex' : 'none';
            this.saveTasks();
        });

        recurrenceGroup.appendChild(recSelect);
        recurrenceGroup.appendChild(daysContainer);
        recurrenceGroup.appendChild(onceContainer);
        tdRecurrence.appendChild(recurrenceGroup);
        tr.appendChild(tdRecurrence);

        // ©¤©¤ Script path ©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤
        const tdScript = document.createElement('td');
        tdScript.className = 'scheduler-col-script';
        const scriptGroup = document.createElement('div');
        scriptGroup.className = 'scheduler-script-group';

        const scriptInput = document.createElement('input');
        scriptInput.type = 'text';
        scriptInput.value = task.scriptPath || '';
        scriptInput.placeholder = 'Path to .cms file';
        scriptInput.className = 'scheduler-script-input';
        scriptInput.addEventListener('change', () => {
            task.scriptPath = scriptInput.value;
            this.saveTasks();
        });

        const browseBtn = document.createElement('button');
        browseBtn.className = 'btn btn-secondary scheduler-browse-btn';
        browseBtn.textContent = '...';
        browseBtn.title = 'Browse';
        browseBtn.addEventListener('click', () => {
            this.browseScriptPath(index);
        });

        scriptGroup.appendChild(scriptInput);
        scriptGroup.appendChild(browseBtn);
        tdScript.appendChild(scriptGroup);
        tr.appendChild(tdScript);

        // ©¤©¤ Last status ©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤
        const tdStatus = document.createElement('td');
        tdStatus.className = 'scheduler-col-status';
        tdStatus.textContent = task.lastStatus || '\u2014'; // em dash
        tr.appendChild(tdStatus);

        // ©¤©¤ Actions ©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤
        const tdActions = document.createElement('td');
        tdActions.className = 'scheduler-col-actions';

        const runBtn = document.createElement('button');
        runBtn.className = 'btn btn-secondary scheduler-action-btn';
        runBtn.textContent = 'Run Now';
        runBtn.title = 'Execute this task immediately';
        runBtn.addEventListener('click', () => {
            this.runTask(index);
        });

        const deleteBtn = document.createElement('button');
        deleteBtn.className = 'btn btn-danger scheduler-action-btn';
        deleteBtn.textContent = 'Delete';
        deleteBtn.addEventListener('click', () => {
            this.deleteTask(index);
        });

        tdActions.appendChild(runBtn);
        tdActions.appendChild(deleteBtn);
        tr.appendChild(tdActions);

        return tr;
    },

    /**
     * Add a new task with default values.
     */
    addTask() {
        const now = new Date();
        const hh = String(now.getHours()).padStart(2, '0');
        const mm = String(now.getMinutes()).padStart(2, '0');

        this.tasks.push({
            enabled: true,
            time: `${hh}:${mm}`,
            scriptPath: '',
            recurrence: 'daily',
            days: [],
            onceDate: '',
            lastStatus: ''
        });

        this.refreshTable();
        this.saveTasks();
    },

    /**
     * Delete a task by index.
     */
    deleteTask(index) {
        this.tasks.splice(index, 1);
        this.refreshTable();
        this.saveTasks();
    },

    /**
     * Run a task immediately by sending it to the backend.
     */
    runTask(index) {
        const task = this.tasks[index];
        if (!task || !task.scriptPath) {
            task.lastStatus = 'No script path';
            this.refreshTable();
            return;
        }

        task.lastStatus = 'Running...';
        this.refreshTable();

        if (App.isWebViewReady) {
            window.chrome.webview.postMessage(JSON.stringify({
                type: 'schedulerRunTask',
                index: index,
                scriptPath: task.scriptPath
            }));
        }
    },

    /**
     * Browse for a script file path via the C# backend.
     */
    browseScriptPath(index) {
        if (App.isWebViewReady) {
            window.chrome.webview.postMessage(JSON.stringify({
                type: 'schedulerBrowseScript',
                index: index
            }));
        }
    },

    /**
     * Handle browse result from C# backend.
     */
    handleBrowseResult(data) {
        const idx = data.index;
        if (idx >= 0 && idx < this.tasks.length && data.selectedPath) {
            this.tasks[idx].scriptPath = data.selectedPath;
            this.refreshTable();
            this.saveTasks();
        }
    },

    /**
     * Handle task run result from C# backend.
     */
    /**
     * Handle auto-disabled notification from backend (once tasks after execution).
     */
    handleAutoDisabled(data) {
        const idx = data.index;
        if (idx >= 0 && idx < this.tasks.length) {
            this.tasks[idx].enabled = false;
            this.tasks[idx].lastStatus = 'Completed (disabled)';
            this.refreshTable();
            this.saveTasks(); // Sync disabled state to backend
        }
    },

    handleRunResult(data) {
        const idx = data.index;
        if (idx >= 0 && idx < this.tasks.length) {
            this.tasks[idx].lastStatus = data.success ? 'OK' : ('Error: ' + (data.message || 'unknown'));
            this.refreshTable();
        }
    },

    /**
     * Load tasks from settings (called from backend settingsLoaded).
     */
    loadTasks(scheduledTasks) {
        if (Array.isArray(scheduledTasks)) {
            // Migrate old tasks that lack recurrence fields
            this.tasks = scheduledTasks.map(t => ({
                ...t,
                recurrence: t.recurrence || 'daily',
                days: t.days || [],
                onceDate: t.onceDate || ''
            }));
        }
        if (this.schedulerTabId && Tabs.activeTabId === this.schedulerTabId) {
            const contentArea = document.getElementById('tabContent');
            if (contentArea && contentArea.firstChild) {
                this.renderSchedulerPanel(contentArea.firstChild);
            }
        }
    },

    /**
     * Persist the current tasks by sending them to the backend.
     */
    saveTasks() {
        if (App.isWebViewReady) {
            window.chrome.webview.postMessage(JSON.stringify({
                type: 'schedulerTasksChanged',
                tasks: this.tasks
            }));
        }
    },

    /**
     * Refresh the table body in-place.
     */
    refreshTable() {
        const tbody = document.getElementById('schedulerTableBody');
        if (!tbody) return;

        tbody.innerHTML = '';

        if (this.tasks.length === 0) {
            const tr = document.createElement('tr');
            tr.className = 'scheduler-empty-row';
            tr.innerHTML = '<td colspan="6">No scheduled tasks. Click "+ Add Task" to create one.</td>';
            tbody.appendChild(tr);
        } else {
            this.tasks.forEach((task, index) => {
                tbody.appendChild(this.createTaskRow(task, index));
            });
        }
    }
};