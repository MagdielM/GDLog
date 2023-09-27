#if TOOLS
using Godot;

[Tool]
public partial class GDLog : EditorPlugin
{
    public override void _EnterTree()
    {
        // Initialization of the plugin goes here.
        AddAutoloadSingleton(nameof(Log), "res://addons/gd_log/log.tscn");
    }

    public override void _ExitTree()
    {
        // Clean-up of the plugin goes here.
        RemoveAutoloadSingleton(nameof(Log));
    }
}
#endif
