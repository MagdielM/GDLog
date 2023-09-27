using Godot;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// <para>
/// A debug window that provides methods for logging text and graphing values in real time. The window's visibility
/// can be toggled using the <c>=</c> key on QWERTY keyboards, or whichever key is in the equivalent location in other
/// keyboard layouts.
/// </para>
///
/// <para>
/// The API is meant to be used in a similar fashion to immediate mode GUI commands. Therefore, text logs are only
/// displayed for one frame, and graphs only persist so long as new values are logged to them each frame. Calls from
/// <c>_PhysicsProcess</c> or from methods called by <c>_PhysicsProcess</c> are handled automatically: the logging
/// methods will work as expected.
/// </para>
///
/// <para>
/// All methods of the class check for <c>OS.HasFeature("editor")</c> before performing any of their actual functionality,
/// so the class will effectively do nothing in release builds.
/// </para>
/// </summary>
public partial class Log : ScrollContainer
{
	private const string InputActionName = "toggle_log_window";

	private VBoxContainer logContainer;
	private Label noLogMessageLabel;
	private readonly List<ILogData> processEntries = new();
	private readonly List<ILogData> physicsProcessEntries = new();
	private readonly Dictionary<string, CategorySection> categorySections = new();
	private bool categoriesNeedSorting = false;

	[Export] private PackedScene categorySectionScene;


	public override void _Ready()
	{
		if (!OS.HasFeature("editor")) return; // Do nothing in release builds.

		// _PhysicsProcess runs before _Process, so physics entries would be cleared before they can be displayed.
		// Ensure the methods run before all others so entries can be accumulated from either calling context.
		// This does unfortunately introduce a one-frame delay.
		ProcessPriority = int.MinValue;
		ProcessPhysicsPriority = int.MinValue;
		logContainer = GetNode<VBoxContainer>("%Log Container");
		noLogMessageLabel = GetNode<Label>("%No Log Message");

		InputEventKey toggleEvent = new()
		{
			PhysicalKeycode = Key.Equal,
		};
		InputMap.AddAction(InputActionName, 0.0f);
		InputMap.ActionAddEvent(InputActionName, toggleEvent);
	}

	public override void _Input(InputEvent @event)
	{
		if (!OS.HasFeature("editor")) return; // Do nothing in release builds.

		if (!@event.IsActionPressed(InputActionName, exactMatch: true))
		{
			return;
		}

		Visible = !Visible;
	}

	public override void _Process(double delta)
	{
		if (!OS.HasFeature("editor")) return; // Do nothing in release builds.

		// Physics entries may persist across multiple render frames.
		// Ensure these are always accounted for when determining which categories may be empty.
		List<ILogData> entries = processEntries.Concat(physicsProcessEntries).ToList();

		// Assume all current categories are to be removed until entries prove otherwise.
		List<string> pendingRemoval = categorySections.Keys.ToList();

		HandleEntries(processEntries, pendingRemoval, false);
		HandleEntries(physicsProcessEntries, pendingRemoval, true);

		// Remove categories with no entries.
		foreach (string category in pendingRemoval)
		{
			categorySections[category].QueueFree();
			categorySections.Remove(category);
		}

		IEnumerable<CategorySection> sections = categorySections.Values;

		if (categoriesNeedSorting)
		{
			foreach (CategorySection section in sections)
			{
				logContainer.RemoveChild(section);
			}
			sections = sections.OrderBy((section) => section.Category);
			foreach (CategorySection section in sections)
			{
				logContainer.AddChild(section);
			}

			categoriesNeedSorting = false;
		}

		processEntries.Clear();

		// Toggle no logs message.
		noLogMessageLabel.Visible = entries.Count <= 0;

		void HandleEntries(List<ILogData> entries, List<string> pendingRemoval, bool areEntriesFromPhysicsFrames)
		{
			foreach (ILogData entry in entries)
			{
				pendingRemoval.Remove(entry.Category); // There's an entry for the category, it persists.

				if (!categorySections.TryGetValue(entry.Category, out CategorySection section))
				{
					categoriesNeedSorting = true;
					section = categorySectionScene.Instantiate<CategorySection>();
					section.Category = entry.Category;
					logContainer.AddChild(section);
					categorySections.Add(entry.Category, section);
				}

				switch (entry)
				{
					case LogText logText:
						section.TextEntries.Add(logText);
						break;

					case LogGraphPoint graphPoint:
						// Graph entries from physics frames, unlike text entries, need to stack when render rate falls
						// behind physics rate. They must also not be entered more than once if they persist across multiple
						// render frames, so they have to be handled in _PhysicsProcess().
						if (!areEntriesFromPhysicsFrames)
						{
							section.ProcessGraphEntries.Add(graphPoint);
						}
						break;

					default:
						break;
				}
			}
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		foreach (ILogData entry in physicsProcessEntries)
		{
			if (!categorySections.TryGetValue(entry.Category, out CategorySection section))
			{
				categoriesNeedSorting = true;
				section = categorySectionScene.Instantiate<CategorySection>();
				section.Category = entry.Category;
				logContainer.AddChild(section);
				categorySections.Add(entry.Category, section);
			}

			if (entry is LogGraphPoint point) section.PhysicsProcessGraphEntries.Add(point);
		}

		physicsProcessEntries.Clear();
	}

	/// <summary>
	/// <para>
	/// Logs the given <paramref name="text"/> string under the specified <paramref name="category"/>.
	/// </para>
	///
	/// <para>
	/// Logs under the same category will be sorted according to the default comparer for strings. For most cases, this
	/// means they will be sorted alphanumerically, with numbers taking precedence over letters, and numbers being
	/// disambiguated one digit at a time, like so:
	/// </para>
	///
	/// <c> "0", "000", "001", "05", "050", "10", "100", "101", "50", "500", "a", "aaa", "baa", "bbb", "ccc", "ddd" </c>
	///
	/// <para>
	/// NOTE: This method does nothing in release builds.
	/// </para>
	/// </summary>
	/// <param name="text">The text to be logged.</param>
	/// <param name="category">The name of the category under which to log the text.</param>
	public void Text(string text, string category = "Uncategorized")
	{
		if (!OS.HasFeature("editor")) return; // Do nothing in release builds.

		if (Engine.IsInPhysicsFrame())
		{
			physicsProcessEntries.Add(new LogText(category, text));
		}
		else
		{
			processEntries.Add(new LogText(category, text));
		}
	}

	/// <summary>
	/// <para>
	/// Graphs a point to the graph with the specified <paramref name="graphName"/>, or creates a new graph if one does
	/// not exist.
	/// </para>
	///
	/// Graph configuration data is packed into each data point to relieve you of the burden of having to manually
	/// initialize the graph and store references to it. Once a graph is created, all configuration data in subsequent
	/// graph points pushed to the same graph are ignored.
	///
	/// <para>
	/// NOTE: This method does nothing in release builds.
	/// </para>
	/// </summary>
	/// <param name="value">The value of the point in the graph.</param>
	/// <param name="graphName">
	/// The name of the graph. All points pushed to the same category with the same graph name are pushed to the same graph.
	/// </param>
	/// <param name="min">The lower bound of the graph.</param>
	/// <param name="max">The upper bound of the graph.</param>
	/// <param name="color">The color of the graph line.</param>
	/// <param name="length">The length of the X axis of the graph, measured in points.</param>
	/// <param name="category">The category under which to place the graph.</param>
	public void Graph(double value, string graphName, double min, double max, Color color, uint length = 100u, string category = "Uncategorized")
	{
		if (!OS.HasFeature("editor")) return; // Do nothing in release builds.

		if (Engine.IsInPhysicsFrame())
		{
			physicsProcessEntries.Add(new LogGraphPoint(category, graphName, value, min, max, length, color));
		}
		else
		{
			processEntries.Add(new LogGraphPoint(category, graphName, value, min, max, length, color));
		}
	}
}
