using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Controls;


// used to create a hovering copy for drag and drop of controls such as button
public class DragAdorner : Adorner
{
    private readonly UIElement _child;
    private readonly VisualCollection _visuals;
    private readonly TranslateTransform _transform;

    public DragAdorner(UIElement adornedElement, UIElement adornment) : base(adornedElement)
    {
        _child = adornment;
        _visuals = new VisualCollection(this) { _child };

        _transform = new TranslateTransform();
        _child.RenderTransform = _transform;

        IsHitTestVisible = false;
    }

    public void SetPosition(double left, double top)
    {
        _transform.X = left;
        _transform.Y = top;
    }

    protected override int VisualChildrenCount => _visuals.Count;

    protected override Visual GetVisualChild(int index) => _visuals[index];

    protected override Size MeasureOverride(Size constraint)
    {
        _child.Measure(constraint);
        return _child.DesiredSize;
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        _child.Arrange(new Rect(_child.DesiredSize));
        return finalSize;
    }
}