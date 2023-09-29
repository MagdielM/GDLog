using Godot;

public partial class LogGraph : VBoxContainer
{
    public string GraphID { get; private set; }
    public double MinValue { get; private set; }
    public double MaxValue { get; private set; }
    public uint Length { get; private set; }
    public Color LineColor { get; private set; }
    public bool IsPhysicsGraph { get; private set; }
    public GraphBehaviorOptions GraphBehavior { get; private set; }

    private Label _topValueLabel;
    private Label _bottomValueLabel;
    private GraphLine _graphLine;

    public void Initialize(
        string graphID,
        double minValue,
        double maxValue,
        uint length,
        Color lineColor,
        bool isPhysicsGraph,
        GraphBehaviorOptions graphBehavior)
    {
        GraphID = graphID;
        MinValue = minValue;
        MaxValue = maxValue;
        Length = length;
        LineColor = lineColor;
        IsPhysicsGraph = isPhysicsGraph;
        GraphBehavior = graphBehavior;
    }

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        _topValueLabel = GetNode<Label>("%Top Value");
        _bottomValueLabel = GetNode<Label>("%Bottom Value");
        _graphLine = GetNode<GraphLine>("%Graph Line");

        _graphLine.Initialize(Length, MinValue, MaxValue, LineColor, GraphBehavior);

        GetNode<Label>("%Graph Title").Text = GraphID;
        _topValueLabel.Text = MaxValue.ToString();
        _bottomValueLabel.Text = MinValue.ToString();

        if (IsPhysicsGraph) GetNode<Label>("%Physics Process Indicator").Show();
    }

    public void PushGraphPoint(double point)
    {
        switch (GraphBehavior)
        {
            case GraphBehaviorOptions.AutoScale:
                if (point < MinValue)
                {
                    MinValue = Mathf.Floor(point);
                    _bottomValueLabel.Text = MinValue.ToString();
                }
                if (point > MaxValue)
                {
                    MaxValue = Mathf.Ceil(point);
                    _topValueLabel.Text = MaxValue.ToString();
                }
                break;
            case GraphBehaviorOptions.Clip:
                point = Mathf.Clamp(point, MinValue, MaxValue);
                break;
        }

        _graphLine.PushDataPoint(point);
    }
}
