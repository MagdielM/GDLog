using Godot;
using System;

public partial class CSharpTest : Node3D
{
    public override void _Process(double delta)
    {
        Log.Text("Hello!", "C# Test");
    }
}
