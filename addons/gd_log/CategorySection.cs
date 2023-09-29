using Godot;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public partial class CategorySection : VBoxContainer
{
    public string Category;

    public List<LogText> TextEntries { get; private set; } = new();
    public List<LogGraphPoint> ProcessGraphEntries { get; private set; } = new();
    public List<LogGraphPoint> PhysicsProcessGraphEntries { get; private set; } = new();

    [Export] private PackedScene _graphScene;

    private Label CategoryLabel { get; set; }
    private Label EntryText { get; set; }
    private HSeparator Divider { get; set; }

    private bool _graphsNeedSorting = false;

    private readonly Dictionary<string, LogGraph> _processLogGraphs = new();
    private readonly Dictionary<string, LogGraph> _physicsProcessLogGraphs = new();

    public override void _Ready()
    {
        CategoryLabel = GetNode<Label>("%Category Label");
        CategoryLabel.Text = Category;

        EntryText = GetNode<Label>("%Entry Text");
        Divider = GetNode<HSeparator>("%Divider");
    }

    public override void _Process(double delta)
    {
        RegisterTextEntries();
        RegisterGraphEntries(ProcessGraphEntries, _processLogGraphs, false);

        EntryText.Visible = !string.IsNullOrEmpty(EntryText.Text);

        Divider.Visible = EntryText.Visible
            && (_processLogGraphs.Count > 0 || _physicsProcessLogGraphs.Count > 0);

        if (_graphsNeedSorting)
        {
            IEnumerable<LogGraph> graphs = _processLogGraphs.Values
                .Concat(_physicsProcessLogGraphs.Values);

            foreach (LogGraph graph in graphs)
            {
                RemoveChild(graph);
            }
            graphs = graphs.OrderBy((graph) => graph.GraphID);
            foreach (LogGraph section in graphs)
            {
                AddChild(section);
            }
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        RegisterGraphEntries(PhysicsProcessGraphEntries, _physicsProcessLogGraphs, true);
    }

    public void RegisterTextEntries()
    {
        EntryText.Text = "";

        // Sort alphabetically.
        StringBuilder fullText = new();
        IEnumerable<LogText> orderedEntries = TextEntries.OrderBy((entry) => entry.Text);
        fullText.AppendJoin('\n', orderedEntries.Select((entry) => entry.Text));
        EntryText.Text = fullText.ToString();

        TextEntries.Clear();
    }

    // Similar logic as categories in Log, but for graphs.
    public void RegisterGraphEntries(
        List<LogGraphPoint> graphEntries,
        Dictionary<string, LogGraph> logGraphs,
        bool areEntriesFromPhysicsFrames)
    {
        List<string> pendingRemoval = logGraphs.Keys.ToList();

        foreach (LogGraphPoint entry in graphEntries)
        {
            pendingRemoval.Remove(entry.GraphID); // There's an entry for the graph, it persists.

            if (!logGraphs.TryGetValue(entry.GraphID, out LogGraph graph))
            {
                _graphsNeedSorting = true;
                graph = _graphScene.Instantiate<LogGraph>();
                graph.Initialize(
                    entry.GraphID,
                    entry.Min,
                    entry.Max,
                    entry.Length,
                    entry.Color,
                    areEntriesFromPhysicsFrames,
                    entry.GraphBehavior);
                logGraphs.Add(entry.GraphID, graph);
                AddChild(graph);
            }

            graph.PushGraphPoint(entry.Value);
        }

        // Remove graphs with no entries.
        foreach (string graphID in pendingRemoval)
        {
            logGraphs[graphID].QueueFree();
            logGraphs.Remove(graphID);
        }

        graphEntries.Clear();
    }
}
