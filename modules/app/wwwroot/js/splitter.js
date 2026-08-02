/**
 * splitter.js - Resizable splitter between tab strip and content area.
 * Only active when tab position is 'left' or 'right'.
 * Allows users to drag to adjust the tab strip width.
 */

const Splitter = {
    // Constraints
    MIN_WIDTH: 100,
    MAX_WIDTH: 400,

    // Drag state
    isDragging: false,
    startX: 0,
    startWidth: 0,
    activeSplitter: null,

    /**
     * Initialize the splitter module.
     */
    init() {
        this.setupEventListeners();
    },

    /**
     * Set up mouse event listeners for both splitters.
     */
    setupEventListeners() {
        const leftSplitter = document.getElementById('splitterLeft');
        const rightSplitter = document.getElementById('splitterRight');

        if (leftSplitter) {
            leftSplitter.addEventListener('mousedown', (e) => this.onMouseDown(e, 'left'));
        }
        if (rightSplitter) {
            rightSplitter.addEventListener('mousedown', (e) => this.onMouseDown(e, 'right'));
        }

        document.addEventListener('mousemove', (e) => this.onMouseMove(e));
        document.addEventListener('mouseup', () => this.onMouseUp());
    },

    /**
     * Handle mousedown on a splitter element.
     * @param {MouseEvent} e - The mouse event.
     * @param {string} side - 'left' or 'right'.
     */
    onMouseDown(e, side) {
        e.preventDefault();
        this.isDragging = true;
        this.startX = e.clientX;
        this.activeSplitter = side;

        const tabStrip = document.querySelector('.tab-strip');
        this.startWidth = tabStrip.offsetWidth;

        // Add active class for visual feedback
        const splitter = side === 'left'
            ? document.getElementById('splitterLeft')
            : document.getElementById('splitterRight');
        if (splitter) splitter.classList.add('active');

        document.body.style.cursor = 'col-resize';
        document.body.style.userSelect = 'none';
    },

    /**
     * Handle mousemove during drag.
     * @param {MouseEvent} e - The mouse event.
     */
    onMouseMove(e) {
        if (!this.isDragging) return;

        const dx = e.clientX - this.startX;
        let newWidth;

        if (this.activeSplitter === 'left') {
            // Left splitter: drag right increases width
            newWidth = this.startWidth + dx;
        } else {
            // Right splitter: drag left increases width
            newWidth = this.startWidth - dx;
        }

        // Clamp to min/max
        newWidth = Math.max(this.MIN_WIDTH, Math.min(this.MAX_WIDTH, newWidth));

        // Apply the new width to the tab strip
        const tabStrip = document.querySelector('.tab-strip');
        if (tabStrip) {
            tabStrip.style.width = newWidth + 'px';
            tabStrip.style.minWidth = newWidth + 'px';
        }
    },

    /**
     * Handle mouseup - end drag operation.
     */
    onMouseUp() {
        if (!this.isDragging) return;

        this.isDragging = false;

        // Remove active class from splitter
        const splitter = this.activeSplitter === 'left'
            ? document.getElementById('splitterLeft')
            : document.getElementById('splitterRight');
        if (splitter) splitter.classList.remove('active');

        document.body.style.cursor = '';
        document.body.style.userSelect = '';

        // Save the new width to settings
        const tabStrip = document.querySelector('.tab-strip');
        if (tabStrip) {
            const width = tabStrip.offsetWidth;
            Settings.settings.tabStripWidth = width;
            Settings.saveTabStripWidth(width);
        }

        this.activeSplitter = null;
    },

    /**
     * Apply a saved width to the tab strip.
     * @param {number} width - The width to apply.
     */
    applyWidth(width) {
        const clampedWidth = Math.max(this.MIN_WIDTH, Math.min(this.MAX_WIDTH, width));
        const tabStrip = document.querySelector('.tab-strip');
        if (tabStrip) {
            tabStrip.style.width = clampedWidth + 'px';
            tabStrip.style.minWidth = clampedWidth + 'px';
        }
    },

    /**
     * Reset the tab strip width to default (used when switching to top position).
     */
    resetWidth() {
        const tabStrip = document.querySelector('.tab-strip');
        if (tabStrip) {
            tabStrip.style.width = '';
            tabStrip.style.minWidth = '';
        }
    }
};
