using Godot;

public partial class LogGraph : VBoxContainer
{
    public string GraphID { get; private set; }
    public double MinValue { get; private set; }
    public double MaxValue { get; private set; }
    public uint Length { get; private set; }
    public Color LineColor { get; private set; }
    public bool IsPhysicsGraph { get; private set; }

    private GraphLine _graphLine;

    public void Initialize(string graphID, double minValue, double maxValue, uint length, Color lineColor, bool isPhysicsGraph)
    {
        GraphID = graphID;
        MinValue = minValue;
        MaxValue = maxValue;
        Length = length;
        LineColor = lineColor;
        IsPhysicsGraph = isPhysicsGraph;
    }

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        GetNode<Label>("%Graph Title").Text = GraphID;
        GetNode<Label>("%Top Value").Text = MaxValue.ToString();
        GetNode<Label>("%Bottom Value").Text = MinValue.ToString();
        if (IsPhysicsGraph) GetNode<Label>("%Physics Process Indicator").Show();
        _graphLine = GetNode<GraphLine>("%Graph Line");
        _graphLine.Initialize(Length, MinValue, MaxValue, LineColor);
    }

    public void PushGraphPoint(double point)
    {
        _graphLine.PushDataPoint(point);
    }
}
