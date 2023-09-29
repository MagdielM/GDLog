using Godot;

/// <summary>
/// <para>
/// Options for the behavior of the graph display when the point values exceed the minimum or maximum
/// values of the graph.
/// </para>
/// <list>
///     <item>
///         <term>Default</term>
///         <description>The graph line will go off-screen when graph values exceed limits.</description>
///     </item>
///     <item>
///         <term>Clip</term>
///         <description>The graph line will be capped at the borders when graph values exceed limits.</description>
///     </item>
///     <item>
///         <term>AutoScale</term>
///         <description>The graph limits will automatically adjust when graph values exceed current limits.</description>
///     </item>
/// </list>
/// </summary>
public enum GraphBehaviorOptions { Default = 0, Clip = 1, AutoScale = 2 }