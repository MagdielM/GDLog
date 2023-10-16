using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// A debug window that provides methods for logging text and graphing values in real time. The
/// window's visibility can be toggled using the <c>=</c> key on QWERTY keyboards, or whichever key
/// is in the equivalent location in other keyboard layouts.
/// </summary>
/// <remarks>
/// <para>
/// The API is meant to be used in a similar fashion to immediate mode GUI commands. Therefore, text
/// logs are only displayed for one frame, and graphs only persist so long as new values are logged
/// to them each frame. Calls from <c>_PhysicsProcess</c> or from methods called by
/// <c>_PhysicsProcess</c> are handled automatically: the logging methods will work as expected.
/// </para>
/// <para>
/// All methods of the class check for <c>OS.HasFeature("editor")</c> before performing any of their
/// actual functionality, so the class will effectively do nothing in release builds.
/// </para>
/// </remarks>
public partial class Log : ScrollContainer
{
    public const string ToggleVisibilityAction = "toggle_log_window";

    [Export] private PackedScene _categorySectionScene;

    private VBoxContainer _logContainer;
    private Label _noLogMessageLabel;

    private bool _categoriesNeedSorting = false;

    private readonly List<ILogData> _processEntries = new();
    private readonly List<ILogData> _physicsProcessEntries = new();

    private readonly Dictionary<string, CategorySection> _categorySections = new();

    public static Log Instance
    {
        get => instance;
        set
        {
            if (instance is not null) throw new InvalidOperationException("Only the single autoloaded instance of "
                + $"{typeof(Log)} may exist.");

            instance = value;
        }
    }
    private static Log instance;

    public override void _EnterTree()
    {
        if (!OS.HasFeature("editor"))
        {
            QueueFree(); // Immediately remove in release builds.
            return;
        }

        Instance = this;

        InputEventKey toggleEvent = new()
        {
            PhysicalKeycode = Key.F4,
        };
        InputMap.AddAction(ToggleVisibilityAction, 0.0f);
        InputMap.ActionAddEvent(ToggleVisibilityAction, toggleEvent);
    }

    public override void _ExitTree()
    {
        if (!OS.HasFeature("editor")) return; // Do nothing in release builds.

        InputMap.EraseAction(ToggleVisibilityAction);
    }

    public override void _Ready()
    {
        if (!OS.HasFeature("editor")) return; // Do nothing in release builds.

        // _PhysicsProcess runs before _Process, so physics entries would be cleared before they can
        // be displayed. Ensure the methods run before all others so entries can be accumulated from
        // either calling context. This does unfortunately introduce a one-frame delay.
        // ProcessPriority = int.MinValue;
        // ProcessPhysicsPriority = int.MinValue;
        _logContainer = GetNode<VBoxContainer>("%Log Container");
        _noLogMessageLabel = GetNode<Label>("%No Log Message");
    }

    public override void _UnhandledKeyInput(InputEvent @event)
    {
        if (!OS.HasFeature("editor")) return; // Do nothing in release builds.

        if (!@event.IsActionPressed(ToggleVisibilityAction, exactMatch: true))
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
        List<ILogData> entries = _processEntries.Concat(_physicsProcessEntries).ToList();

        // Assume all current categories are to be removed until entries prove otherwise.
        List<string> pendingRemoval = _categorySections.Keys.ToList();

        HandleEntries(_processEntries, pendingRemoval, false);
        HandleEntries(_physicsProcessEntries, pendingRemoval, true);

        // Remove categories with no entries.
        foreach (string category in pendingRemoval)
        {
            _categorySections[category].QueueFree();
            _categorySections.Remove(category);
        }

        IEnumerable<CategorySection> sections = _categorySections.Values;

        if (_categoriesNeedSorting)
        {
            foreach (CategorySection section in sections)
            {
                _logContainer.RemoveChild(section);
            }
            sections = sections.OrderBy((section) => section.Category);
            foreach (CategorySection section in sections)
            {
                _logContainer.AddChild(section);
            }

            _categoriesNeedSorting = false;
        }

        _processEntries.Clear();

        // Toggle no logs message.
        _noLogMessageLabel.Visible = entries.Count <= 0;

        void HandleEntries(List<ILogData> entries, List<string> pendingRemoval, bool areEntriesFromPhysicsFrames)
        {
            foreach (ILogData entry in entries)
            {
                pendingRemoval.Remove(entry.Category); // There's an entry for the category, it persists.

                if (!_categorySections.TryGetValue(entry.Category, out CategorySection section))
                {
                    _categoriesNeedSorting = true;
                    section = _categorySectionScene.Instantiate<CategorySection>();
                    section.Category = entry.Category;
                    _logContainer.AddChild(section);
                    _categorySections.Add(entry.Category, section);
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
        foreach (ILogData entry in _physicsProcessEntries)
        {
            if (!_categorySections.TryGetValue(entry.Category, out CategorySection section))
            {
                _categoriesNeedSorting = true;
                section = _categorySectionScene.Instantiate<CategorySection>();
                section.Category = entry.Category;
                _logContainer.AddChild(section);
                _categorySections.Add(entry.Category, section);
            }

            if (entry is LogGraphPoint point) section.PhysicsProcessGraphEntries.Add(point);
        }

        _physicsProcessEntries.Clear();
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
    public static void Text(string text, string category)
    {
        Instance?.TextImpl(text, category);
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
    /// <param name="graphBehavior">
    /// The behavior of the graph display when values exceed the given <paramref name="min"/> and
    /// <paramref name="max"/> values. See <see cref="GraphBehaviorOptions"/> for details.
    /// </param>
    /// <param name="category">The category under which to place the graph.</param>
    
    public static void Graph(
        double value,
        string graphName,
        double min,
        double max,
        Color color,
        uint length = 100u,
        GraphBehaviorOptions graphBehavior = GraphBehaviorOptions.Default,
        string category = "Uncategorized")
    {
        Instance?.GraphImpl(value, graphName, min, max, color, length, graphBehavior, category);
    }

    private void TextImpl(string text, string category)
    {
        if (!OS.HasFeature("editor")) return; // Do nothing in release builds.

        if (Engine.IsInPhysicsFrame())
        {
            _physicsProcessEntries.Add(new LogText(category, text));
        }
        else
        {
            _processEntries.Add(new LogText(category, text));
        }
    }

    private void GraphImpl(
        double value,
        string graphName,
        double min,
        double max,
        Color color,
        uint length,
        GraphBehaviorOptions graphBehavior,
        string category)
    {
        if (!OS.HasFeature("editor")) return; // Do nothing in release builds.

        if (Engine.IsInPhysicsFrame())
        {
            _physicsProcessEntries.Add(new LogGraphPoint(category, graphName, value, min, max, length, color, graphBehavior));
        }
        else
        {
            _processEntries.Add(new LogGraphPoint(category, graphName, value, min, max, length, color, graphBehavior));
        }
    }
}
