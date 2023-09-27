using Godot;

public readonly partial record struct LogGraphPoint(string Category, string GraphID, double Value, double Min, double Max, uint Length, Color Color) : ILogData;
