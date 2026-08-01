using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using app.ViewModels;

namespace app.Views;

/// <summary>
/// Code-behind for the main window. Handles tab drag-and-drop reordering
/// with visual feedback (ghost tab + blue drop indicator line).
/// </summary>
public partial class MainWindow : Window
{
    private Point _dragStartPoint;
    private bool _isDragging;
    private int _dragFromIndex = -1;
    private TabItem? _draggedTab;
    private TabDragAdorner? _dragAdorner;
    private TabDropAdorner? _dropAdorner;

    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel();
    }

    // ── Visual Tree Helpers ──────────────────────────────────────────

    private static TabItem? FindTabItem(DependencyObject? source)
    {
        while (source != null)
        {
            if (source is TabItem tabItem)
                return tabItem;
            source = VisualTreeHelper.GetParent(source);
        }
        return null;
    }

    private static TabControl? FindTabControl(DependencyObject? source)
    {
        while (source != null)
        {
            if (source is TabControl tabControl)
                return tabControl;
            source = VisualTreeHelper.GetParent(source);
        }
        return null;
    }

    // ── Drag Start ───────────────────────────────────────────────────

    protected override void OnPreviewMouseDown(MouseButtonEventArgs e)
    {
        _dragStartPoint = e.GetPosition(this);

        // Track which tab was clicked for drag adorner creation later.
        var source = e.OriginalSource as DependencyObject;
        _draggedTab = FindTabItem(source);

        base.OnPreviewMouseDown(e);
    }

    protected override void OnPreviewMouseMove(MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed || _isDragging)
        {
            base.OnPreviewMouseMove(e);
            return;
        }

        if (_draggedTab == null)
        {
            base.OnPreviewMouseMove(e);
            return;
        }

        Point currentPosition = e.GetPosition(this);
        Vector diff = currentPosition - _dragStartPoint;

        // Start drag after a small threshold to avoid accidental drags.
        if (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
            Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance)
        {
            _isDragging = true;

            var tabControl = FindTabControl(_draggedTab);
            if (tabControl == null)
            {
                _isDragging = false;
                base.OnPreviewMouseMove(e);
                return;
            }

            _dragFromIndex = tabControl.ItemContainerGenerator.IndexFromContainer(_draggedTab);

            // Create the ghost adorner (follows mouse).
            _dragAdorner = TabDragAdorner.Create(_draggedTab);

            // Create the drop indicator adorner (blue line).
            var adornerLayer = AdornerLayer.GetAdornerLayer(tabControl);
            if (adornerLayer != null)
            {
                _dropAdorner = new TabDropAdorner(tabControl)
                {
                    tabPlacement = tabControl.TabStripPlacement
                };
                adornerLayer.Add(_dropAdorner);
            }

            // Perform the drag-and-drop operation.
            var data = new DataObject("TabIndex", _dragFromIndex);
            DragDrop.DoDragDrop(_draggedTab, data, DragDropEffects.Move);

            // Clean up adorners after drop completes.
            CleanupAdorners();
            _isDragging = false;
            _dragFromIndex = -1;
        }

        base.OnPreviewMouseMove(e);
    }

    // ── Drag Feedback ────────────────────────────────────────────────

    /// <summary>
    /// Suppress the default drag cursor so our adorner provides the visual feedback.
    /// </summary>
    protected override void OnGiveFeedback(GiveFeedbackEventArgs e)
    {
        e.UseDefaultCursors = true;
        e.Handled = true;
        base.OnGiveFeedback(e);
    }

    // ── Drag Over (update adorner positions) ─────────────────────────

    private void OnTabControlDragOver(object sender, DragEventArgs e)
    {
        if (sender is not TabControl tabControl)
            return;

        if (!e.Data.GetDataPresent("TabIndex"))
            return;

        // Update ghost adorner position.
        if (_dragAdorner != null)
        {
            Point mousePos = e.GetPosition(tabControl);
            _dragAdorner.UpdatePosition(mousePos);
        }

        // Update drop indicator position.
        // The blue line shows where the tab will visually land after the drop.
        // rawTo is the visual insertion point (left edge of the tab at that index,
        // or after the last tab). We use rawTo directly for the adorner because
        // during the drag the source tab is still in its original position.
        if (_dropAdorner != null)
        {
            int rawTo = GetDropTargetIndex(tabControl, e.GetPosition(tabControl));
            _dropAdorner.targetIndex = rawTo;
        }
    }

    // ── Drop ─────────────────────────────────────────────────────────

    private void OnTabControlDrop(object sender, DragEventArgs e)
    {
        if (sender is not TabControl tabControl)
            return;

        if (!e.Data.GetDataPresent("TabIndex"))
            return;

        int fromIndex = (int)e.Data.GetData("TabIndex")!;
        int toIndex = GetDropTargetIndex(tabControl, e.GetPosition(tabControl));

        // Clamp to valid range: tabs.Move requires to in [0, Count-1].
        if (toIndex >= tabControl.Items.Count)
            toIndex = tabControl.Items.Count - 1;

        if (toIndex < 0 || fromIndex == toIndex)
            return;

        if (DataContext is MainViewModel vm)
        {
            vm.moveTabCommand.Execute($"{fromIndex},{toIndex}");
        }
    }

    // ── Cleanup ──────────────────────────────────────────────────────

    private void OnTabControlDragLeave(object sender, DragEventArgs e)
    {
        // Hide the drop indicator when the mouse leaves the tab control.
        if (_dropAdorner != null)
            _dropAdorner.targetIndex = -1;
    }

    private void CleanupAdorners()
    {
        _dragAdorner?.Detach();
        _dragAdorner = null;

        if (_dropAdorner != null)
        {
            var tabControl = FindTabControl(_draggedTab);
            if (tabControl != null)
            {
                var layer = AdornerLayer.GetAdornerLayer(tabControl);
                layer?.Remove(_dropAdorner);
            }
            _dropAdorner = null;
        }

        _draggedTab = null;
    }

    // ── Drop Target Calculation ──────────────────────────────────────

    /// <summary>
    /// Determines the raw target index based on the mouse position over the tab strip.
    /// This is before accounting for source removal; callers apply the effective correction.
    /// </summary>
    private static int GetDropTargetIndex(TabControl tabControl, Point position)
    {
        for (int i = 0; i < tabControl.Items.Count; i++)
        {
            var container = tabControl.ItemContainerGenerator.ContainerFromIndex(i) as TabItem;
            if (container == null) continue;

            Rect bounds = new Rect(
                container.TranslatePoint(new Point(), tabControl),
                container.RenderSize);

            if (bounds.Contains(position))
            {
                // Drop to the right half of the tab if past the midpoint.
                bool afterMidpoint = tabControl.TabStripPlacement == Dock.Left ||
                                     tabControl.TabStripPlacement == Dock.Right
                    ? position.Y > bounds.Top + bounds.Height / 2
                    : position.X > bounds.Left + bounds.Width / 2;

                return afterMidpoint ? i + 1 : i;
            }
        }

        return tabControl.Items.Count; // Drop at end.
    }
}
