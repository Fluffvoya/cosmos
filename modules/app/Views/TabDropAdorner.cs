using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace app.Views;

/// <summary>
/// Adorner that draws a blue vertical (or horizontal) insertion line
/// to indicate where a dragged tab will be dropped.
/// </summary>
public class TabDropAdorner : Adorner
{
    private int _targetIndex = -1;
    private Dock _tabPlacement = Dock.Top;

    /// <summary>
    /// The index where the tab will be inserted. The line is drawn at the left (or top)
    /// edge of the tab at this index, or after the last tab if equal to Items.Count.
    /// </summary>
    public int targetIndex
    {
        get => _targetIndex;
        set
        {
            if (_targetIndex != value)
            {
                _targetIndex = value;
                InvalidateVisual();
            }
        }
    }

    /// <summary>
    /// The tab strip placement (Top, Left, Right) — determines line orientation.
    /// </summary>
    public Dock tabPlacement
    {
        get => _tabPlacement;
        set
        {
            if (_tabPlacement != value)
            {
                _tabPlacement = value;
                InvalidateVisual();
            }
        }
    }

    public TabDropAdorner(UIElement adornedElement) : base(adornedElement) { }

    protected override void OnRender(DrawingContext dc)
    {
        if (AdornedElement is not TabControl tabControl || _targetIndex < 0)
            return;

        bool isVertical = _tabPlacement == Dock.Left || _tabPlacement == Dock.Right;
        double lineThickness = 3.0;
        var brush = new SolidColorBrush(Color.FromRgb(0x39, 0x8A, 0xF4)); // Blue accent
        var pen = new Pen(brush, lineThickness);

        if (isVertical)
        {
            // Horizontal line above the target tab item
            double y = 0;
            if (_targetIndex < tabControl.Items.Count)
            {
                var container = tabControl.ItemContainerGenerator.ContainerFromIndex(_targetIndex) as TabItem;
                if (container != null)
                {
                    var pos = container.TranslatePoint(new Point(), tabControl);
                    y = pos.Y;
                }
                else
                {
                    return;
                }
            }
            else
            {
                // After the last tab
                int last = tabControl.Items.Count - 1;
                if (last >= 0)
                {
                    var container = tabControl.ItemContainerGenerator.ContainerFromIndex(last) as TabItem;
                    if (container != null)
                    {
                        var pos = container.TranslatePoint(new Point(), tabControl);
                        y = pos.Y + container.RenderSize.Height;
                    }
                }
            }

            double tabStripWidth = GetTabStripWidth(tabControl);
            dc.DrawLine(pen, new Point(0, y), new Point(tabStripWidth, y));
        }
        else
        {
            // Vertical line to the left of the target tab item
            double x = 0;
            if (_targetIndex < tabControl.Items.Count)
            {
                var container = tabControl.ItemContainerGenerator.ContainerFromIndex(_targetIndex) as TabItem;
                if (container != null)
                {
                    var pos = container.TranslatePoint(new Point(), tabControl);
                    x = pos.X;
                }
                else
                {
                    return;
                }
            }
            else
            {
                // After the last tab
                int last = tabControl.Items.Count - 1;
                if (last >= 0)
                {
                    var container = tabControl.ItemContainerGenerator.ContainerFromIndex(last) as TabItem;
                    if (container != null)
                    {
                        var pos = container.TranslatePoint(new Point(), tabControl);
                        x = pos.X + container.RenderSize.Width;
                    }
                }
            }

            double tabStripHeight = GetTabStripHeight(tabControl);
            dc.DrawLine(pen, new Point(x, 0), new Point(x, tabStripHeight));
        }
    }

    /// <summary>
    /// Gets the height of the tab strip area (for Left/Right placement).
    /// </summary>
    private static double GetTabStripHeight(TabControl tabControl)
    {
        double maxHeight = 0;
        for (int i = 0; i < tabControl.Items.Count; i++)
        {
            var container = tabControl.ItemContainerGenerator.ContainerFromIndex(i) as TabItem;
            if (container != null)
            {
                var pos = container.TranslatePoint(new Point(), tabControl);
                double bottom = pos.Y + container.RenderSize.Height;
                if (bottom > maxHeight)
                    maxHeight = bottom;
            }
        }
        return maxHeight;
    }

    /// <summary>
    /// Gets the width of the tab strip area (for Top placement).
    /// </summary>
    private static double GetTabStripWidth(TabControl tabControl)
    {
        double maxWidth = 0;
        for (int i = 0; i < tabControl.Items.Count; i++)
        {
            var container = tabControl.ItemContainerGenerator.ContainerFromIndex(i) as TabItem;
            if (container != null)
            {
                var pos = container.TranslatePoint(new Point(), tabControl);
                double right = pos.X + container.RenderSize.Width;
                if (right > maxWidth)
                    maxWidth = right;
            }
        }
        return maxWidth;
    }
}
