using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace app.Views;

/// <summary>
/// Adorner that renders a semi-transparent ghost of the dragged tab header,
/// following the mouse cursor during a drag operation.
/// </summary>
public class TabDragAdorner : Adorner
{
    private readonly VisualBrush _brush;
    private readonly Size _size;
    private Point _currentPos;

    /// <summary>
    /// Creates a drag adorner from the visual of the dragged TabItem's header.
    /// </summary>
    /// <param name="tabItem">The TabItem being dragged.</param>
    public TabDragAdorner(TabItem tabItem) : base(tabItem)
    {
        // Capture the visual appearance of the tab header.
        _brush = new VisualBrush(tabItem)
        {
            Opacity = 0.6
        };
        _size = tabItem.RenderSize;

        IsHitTestVisible = false; // Don't interfere with drag events.
    }

    /// <summary>
    /// Updates the position of the ghost relative to the adorned element's parent.
    /// </summary>
    public void UpdatePosition(Point position)
    {
        _currentPos = position;
        InvalidateVisual();
    }

    protected override Size MeasureOverride(Size constraint)
    {
        return _size;
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        return _size;
    }

    protected override void OnRender(DrawingContext dc)
    {
        dc.DrawRectangle(_brush, null, new Rect(_currentPos, _size));
    }

    /// <summary>
    /// Helper to get the adorner layer from a TabItem.
    /// </summary>
    public static TabDragAdorner? Create(TabItem tabItem)
    {
        var layer = AdornerLayer.GetAdornerLayer(tabItem);
        if (layer == null) return null;

        var adorner = new TabDragAdorner(tabItem);
        layer.Add(adorner);
        return adorner;
    }

    /// <summary>
    /// Removes this adorner from its adorner layer.
    /// </summary>
    public void Detach()
    {
        var layer = AdornerLayer.GetAdornerLayer(AdornedElement);
        layer?.Remove(this);
    }
}
