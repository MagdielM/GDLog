using Godot;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;

public partial class CategorySection : VBoxContainer
{
	public string Category;

	private Label CategoryLabel { get; set; }
	private Label EntryText { get; set; }
	private HSeparator Divider { get; set; }

	public List<LogText> TextEntries { get; private set; } = new();
	public List<LogGraphPoint> ProcessGraphEntries { get; private set; } = new();
	public List<LogGraphPoint> PhysicsProcessGraphEntries { get; private set; } = new();

	private readonly Dictionary<string, LogGraph> processLogGraphs = new();
	private readonly Dictionary<string, LogGraph> physicsProcessLogGraphs = new();

	private bool graphsNeedSorting = false;

	[Export] PackedScene graphScene;

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
		RegisterGraphEntries(ProcessGraphEntries, processLogGraphs, false);

		EntryText.Visible = !string.IsNullOrEmpty(EntryText.Text);

		Divider.Visible = EntryText.Visible && (processLogGraphs.Count > 0 || physicsProcessLogGraphs.Count > 0);

		if (graphsNeedSorting)
		{
			IEnumerable<LogGraph> graphs = processLogGraphs.Values.Concat(physicsProcessLogGraphs.Values);

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
		RegisterGraphEntries(PhysicsProcessGraphEntries, physicsProcessLogGraphs, true);
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
	public void RegisterGraphEntries(List<LogGraphPoint> graphEntries, Dictionary<string, LogGraph> logGraphs, bool areEntriesFromPhysicsFrames)
	{
		IEnumerable<LogGraphPoint> GraphEntries = ProcessGraphEntries.Concat(PhysicsProcessGraphEntries);
		List<string> pendingRemoval = logGraphs.Keys.ToList();

		foreach (LogGraphPoint entry in graphEntries)
		{
			pendingRemoval.Remove(entry.GraphID); // There's an entry for the graph, it persists.

			if (!logGraphs.TryGetValue(entry.GraphID, out LogGraph graph))
			{
				graphsNeedSorting = true;
				graph = graphScene.Instantiate<LogGraph>();
				graph.Initialize(entry.GraphID, entry.Min, entry.Max, entry.Length, entry.Color, areEntriesFromPhysicsFrames);
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
