# GDLog
![GDLog logo](https://github.com/MagdielM/GDLog/assets/56076033/842731c9-a576-421d-9106-9c1e9483246d)

GDLog is a logging and graphing tool written in C# for Godot 4.2 that lets you quickly and easily track real-time data in your projects. The plugin was designed for simplicity and ease of use: the entire API surface is just two methods!

## How to Use

The `Log` class is a `Control` node that the plugin registers as an autoload singleton that displays all logged values. It can be toggled with the <c>=</c> key on QWERTY keyboards, or whichever key is in the equivalent location in other keyboard layouts. This class also provides both methods needed to log either text or graph values: namely, `Text()` and `Graph()`.

The methods provided by this class are meant to be used similarly to immediate mode GUI commands. Therefore, text logs are only displayed for one frame, and graphs only persist so long as new values are logged to them each frame. Calls from `_PhysicsProcess()` or from methods called by `_PhysicsProcess()` are handled automatically: the logging methods will still work as expected.

Do also note that since this plugin is meant to be used for debugging purposes, its functionality is disabled in release builds.

### `Text()`

The `Text()` method takes only two parameters: the text to be logged and the category under which to log the text. Logs under the same category will be sorted according to the default comparer for strings in .NET. For most cases, this means they will be sorted alphanumerically, with numbers taking precedence over letters, and numbers being  disambiguated one digit at a time, like so:

```"0", "000", "001", "05", "050", "10", "100", "101", "50", "500", "a", "aaa", "baa", "bbb", "ccc", "ddd"```

### `Graph()`

`Graph()` takes the following parameters:

- "value" (double): The value of the point in the graph.
- "graphName" (string): The name of the graph. All points pushed to the same category with the same graph name are pushed to the same graph.
- "min" (double): The lower bound of the graph.
- "max" (double): The upper bound of the graph.
- "color" (Color): The color of the graph line.
- "length" (uint): The length of the X axis of the graph, measured in points.
- "category" (string): The category under which to place the graph.

As mentioned earlier, graphs only persist in the window so long as new values are being pushed to them every frame. All values logged with the same `graphName` and `category` will be pushed to the same graph. The configuration parameters for the graph are only acknowledged when the graph is logged to for the first time. To reconfigure the graph, allow it to be removed by not calling the method with the desired `graphName` and `category` for at least one frame, and then push new values with the new graph configuration.

### Usage in GDScript vs. C#

In the current dev release of Godot 4.2 (v4.2-dev5 as of the time of this writing), it's not possible to call C# static methods from GDScript. However, [a fix for this](https://github.com/godotengine/godot/pull/81783) has already been merged into master, which means the following usage instructions may change (for the better) soon.

Because of the aforementioned limitation, `Text()` and `Graph()` are declared as instance methods. Since `Log` is an autoload singleton, these methods can still be accessed through the type name in GDScript, like so:

```python
Log.Text("Here's some text.", "Uncategorized")
```

You can also access them by getting the autoload from the root of the scene tree and calling the methods from the instance:

```python
var log_node = $"/root/Log";
log_node.Text("Here's some text.", "Uncategorized")
```

However, you can currently only access the methods from an instance in C#, which means you have to get the autoload from the root of the scene tree in all cases:

```cs
Log logNode = GetNode<Log>("/root/Log");
logNode.Text("Here's some text.", "Uncategorized");
```
