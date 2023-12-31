extends Node3D

var physics_time: float;
var should_log_pos: bool;

@onready var Log: CSharpScript = load("res://addons/gd_log/Log.cs");

func _input(event: InputEvent) -> void:
	if event.is_action_pressed("ui_accept"):
		should_log_pos = not should_log_pos;
	pass;

func _process(_delta: float) -> void:
	Log.Text("ffffff", "Uncategorized");
	Log.Text("cccccc", "Uncategorized");
	
	var num := sin(Time.get_ticks_msec() * 0.01) * 100 + 50.0;
	Log.Text(str(num), "Graph Test");
	Log.Graph(num, "Process Graph", 0.0, 100.0, Color.LIGHT_CORAL, 100, GraphBehavior.DEFAULT, "Graph Test");

	if should_log_pos:
		#Log.Graph(25.0, "A Graph", 0.0, 100.0, Color.WHITE, 100, "Node3D");
		#Log.Graph(25.0, "A Graph", 0.0, 100.0, Color.WHITE, 100, "Uncategorized");
		Log.Text("Position: " + str(position), "Node3D");

	pass;

func _physics_process(delta: float) -> void:
	physics_time += delta;
	var num := sin(physics_time * 10) * 25 + 50.0;
	Log.Graph(num, "Physics Process Graph", 0.0, 100.0, Color.LIGHT_SKY_BLUE, 100, GraphBehavior.DEFAULT, "Physics Test");
	Log.Text(str(num), "Physics Test");
