using System.Collections.Generic;
using Godot;

public partial class GraphLine : Control
{
    // Would love to use a queue, but drawing needs access by index.
    private List<double> DataPoints { get; init; } = new List<double>();

    private uint Length { get; set; }

    private double MinValue { get; set; }
    private double MaxValue { get; set; }

    private Color LineColor { get; set; }

    private GraphBehaviorOptions GraphBehavior { get; set; }

    public override void _Draw()
    {
        if (DataPoints.Count < 2) return;

        Vector2 graphSize = Size;
        Vector2[] points = new Vector2[DataPoints.Count];
        
        for (int i = 0; i < DataPoints.Count; ++i)
        {
            float x = graphSize.X / Length * i;
            float y = (float)Mathf.Remap(DataPoints[i], MinValue, MaxValue, graphSize.Y, 0.0f);
            
            points[i] = new Vector2(x, y);
        }

        DrawPolyline(points, LineColor, 0.5f, true);
    }

    public void Initialize(
        uint length,
        double minValue,
        double maxValue,
        Color lineColor,
        GraphBehaviorOptions graphBehavior)
    {
        Length = length;
        MinValue = minValue;
        MaxValue = maxValue;
        LineColor = lineColor;
        GraphBehavior = graphBehavior;
    }

    public void PushDataPoint(double point)
    {
        DataPoints.Add(point);
        
        if (GraphBehavior == GraphBehaviorOptions.AutoScale)
        {
            if (point < MinValue) MinValue = Mathf.Floor(point);
            if (point > MaxValue) MaxValue = Mathf.Ceil(point);
        }

        QueueRedraw();

        if (DataPoints.Count > Length) DataPoints.RemoveAt(0);
    }
}
