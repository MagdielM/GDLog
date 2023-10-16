# GDLog
![GDLog logo](https://github.com/MagdielM/GDLog/assets/56076033/842731c9-a576-421d-9106-9c1e9483246d)

GDLog is a logging and graphing tool written in C# for Godot 4.2 that lets you quickly and easily track real-time data in your projects. The plugin was designed for simplicity and ease of use: the entire API surface is just two methods!

## How to Use

The `Log` class is a `Control` node that the plugin registers as an autoload singleton that displays all logged values. It can be toggled with the <c>F4</c> key on QWERTY keyboards, or whichever key is in the equivalent location in other keyboard layouts. This class also provides both methods needed to log either text or graph values: namely, `Text()` and `Graph()`.

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
- "graphBehavior (int): The behavior of the graph display when values exceed the given min and max values.
- "category" (string): The category under which to place the graph.

As mentioned earlier, graphs only persist in the window so long as new values are being pushed to them every frame. All values logged with the same `graphName` and `category` will be pushed to the same graph. The configuration parameters for the graph are only acknowledged when the graph is logged to for the first time. To reconfigure the graph, allow it to be removed by not calling the method with the desired `graphName` and `category` for at least one frame, and then push new values with the new graph configuration.

#### Graph Behavior

The `graphBehavior` parameter can take the following options:

- Default (0): The graph line will go off-screen when graph values exceed limits.
- Clip (1): The graph line will be capped at the borders when graph values exceed limits.
- AutoScale (2): The graph limits will automatically adjust when graph values exceed current limits.

Any values outside of these will be treated as Default by the graph.

Since the native C# enum where these options are defined, `GraphBehaviorOptions`, is inaccessible to GDScript, a GDScript type named `GraphBehavior` with identical options is also provided. Note that in the GDScript type the options are named in SCREAMING_CAMEL_CASE to better suit GDScript style guidelines.

### Usage in GDScript vs. C#

GDLog uses an autoload singleton for nearly all of its functionality. This autoload is automatically added and removed as the plugin is enabled and disabled respectively. With the autoload in place, the logging methods can be called with similar syntax from either language:

```python
# Note that the optional arguments from the C# method do not transfer over to GDScript: they are all mandatory.
Log.Text("Here's some text from GDScript.", "Uncategorized")
```
```cs
Log.Text("Here's some text from C#.", "Uncategorized");
```

However, in GDScript's case, this calling syntax will break if the plugin is disabled or the autoload is removed, because it's calling the methods through the instance of the autoload instead of as a static method. If the autoload is not present, the GDScript parser will refuse to compile the script. To avoid this, load the `Log` class into a variable at runtime and call the methods from there, like so:

```python

var Log: CSharpScript = load("res://addons/gd_log/Log.cs") # Do note that this cannot be done in a fully type-safe manner at present.

# Elsewhere, within the same scope as the Log variable.
Log.Text("Here's some text from GDScript.", "Uncategorized")
```