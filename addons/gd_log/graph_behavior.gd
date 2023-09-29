## Options for the behavior of the graph display when the point values exceed the minimum or maximum
## values of the graph.
##
## - Default: The graph line will go off-screen when graph values exceed limits.
## - Clip: The graph line will be capped at the borders when graph values exceed limits.
## - AutoScale: The graph limits will automatically adjust when graph values exceed current limits.
##
## Note: There is also a C# enum type which mirrors this one. This GDScript equivalent is exists
## to provide more intuitive usage via named constants.
class_name GraphBehavior
enum {
	DEFAULT = 0,
	CLIP = 1,
	AUTO_SCALE = 2,
}
