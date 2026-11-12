using SmartX.Core.DataStructures;
using SmartX.Core.Models;

int passed = 0;
int failed = 0;
Guid a = Guid.Parse("00000000-0000-0000-0000-000000000001");
Guid b = Guid.Parse("00000000-0000-0000-0000-000000000002");
Guid c = Guid.Parse("00000000-0000-0000-0000-000000000003");
Guid d = Guid.Parse("00000000-0000-0000-0000-000000000004");
DateTime origin = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

void Check(string name, Action test)
{
    try
    {
        test();
        passed++;
        Console.WriteLine($"PASS {name}");
    }
    catch (Exception error)
    {
        failed++;
        Console.WriteLine($"FAIL {name}: {error.Message}");
    }
}

void Assert(bool condition, string message)
{
    if (!condition) { throw new Exception(message); }
}

StreamTelemetryItem Reading(Guid sensorId, int minute) =>
    new() { SensorId = sensorId, Timestamp = origin.AddMinutes(minute), Metric = "Temperature", Value = minute.ToString() };

MeshNetworkGraph Triangle()
{
    MeshNetworkGraph graph = new();
    graph.AddLink(a, b, 2);
    graph.AddLink(b, c, 3);
    graph.AddLink(a, c, 10);
    return graph;
}

Check("AVL sorts reverse-ordered insertions", () =>
{
    TelemetryAvlTree tree = new();
    for (int i = 100; i >= 0; i--) { tree.Insert(Reading(a, i)); }
    List<StreamTelemetryItem> results = tree.SearchRange(origin, origin.AddMinutes(100));
    Assert(tree.Count == 101 && results.Count == 101, "Missing readings");
    Assert(results.Select(x => x.Timestamp).SequenceEqual(results.Select(x => x.Timestamp).OrderBy(x => x)), "Results not chronological");
});

Check("AVL handles ascending insertions and rotations", () =>
{
    TelemetryAvlTree tree = new();
    for (int i = 0; i < 1000; i++) { tree.Insert(Reading(a, i)); }
    Assert(tree.SearchRange(origin.AddMinutes(400), origin.AddMinutes(500)).Count == 101, "Range lookup failed after balancing");
});

Check("AVL preserves duplicate timestamps", () =>
{
    TelemetryAvlTree tree = new();
    tree.Insert(Reading(a, 1));
    tree.Insert(Reading(b, 1));
    Assert(tree.Count == 2 && tree.SearchRange(origin.AddMinutes(1), origin.AddMinutes(1)).Count == 2, "Duplicate timestamp lost");
});

Check("AVL range is inclusive and filters sensor ID", () =>
{
    TelemetryAvlTree tree = new();
    tree.Insert(Reading(a, 1));
    tree.Insert(Reading(b, 2));
    tree.Insert(Reading(a, 3));
    Assert(tree.SearchRange(origin.AddMinutes(1), origin.AddMinutes(3), a).Count == 2, "Sensor filter or inclusive bounds incorrect");
    Assert(tree.SearchRange(origin.AddMinutes(2), origin.AddMinutes(2)).Count == 1, "Equal range bounds not inclusive");
});

Check("AVL empty range returns no readings", () =>
{
    TelemetryAvlTree tree = new();
    tree.Insert(Reading(a, 5));
    Assert(tree.SearchRange(origin, origin.AddMinutes(1)).Count == 0, "Unexpected result");
});

Check("AVL rejects reversed date range", () =>
{
    TelemetryAvlTree tree = new();
    try { tree.SearchRange(origin.AddMinutes(2), origin); throw new Exception("Reversed range was accepted"); }
    catch (ArgumentException) { }
});

Check("BFS chooses fewest hops rather than minimum weight", () =>
{
    List<Guid> path = Triangle().FindRoute(a, c);
    Assert(path.SequenceEqual(new[] { a, c }), "Direct one-hop route expected");
});

Check("BFS reaches every connected node", () =>
{
    List<Guid> visited = Triangle().BreadthFirstSearch(a);
    Assert(visited.Count == 3 && visited.Contains(a) && visited.Contains(b) && visited.Contains(c), "Connected nodes missing");
});

Check("Prim MST chooses weights 2 and 3", () =>
{
    List<MeshLink> mst = Triangle().MinimumSpanningTree();
    Assert(mst.Count == 2 && Math.Abs(mst.Sum(x => x.Weight) - 5) < 0.00001, "Expected MST total weight 5");
});

Check("Offline node is excluded from BFS and MST", () =>
{
    MeshNetworkGraph graph = Triangle();
    HashSet<Guid> offline = new() { b };
    Assert(graph.FindRoute(a, c, offline).SequenceEqual(new[] { a, c }), "Route should bypass B");
    Assert(!graph.BreadthFirstSearch(a, offline).Contains(b), "Offline B was visited");
    Assert(graph.MinimumSpanningTree(offline).Count == 1 && graph.MinimumSpanningTree(offline)[0].Weight == 10, "MST should use A-C only");
});

Check("Disconnected graph produces minimum spanning forest", () =>
{
    MeshNetworkGraph graph = Triangle();
    graph.AddNode(d);
    Assert(graph.FindRoute(a, d).Count == 0, "Disconnected node unexpectedly reachable");
    Assert(graph.MinimumSpanningTree().Count == 2, "Forest should have two links and isolated D");
});

Check("Missing or offline source returns no route", () =>
{
    MeshNetworkGraph graph = Triangle();
    Assert(graph.FindRoute(Guid.NewGuid(), a).Count == 0, "Unknown source should fail");
    Assert(graph.FindRoute(a, a, new HashSet<Guid> { a }).Count == 0, "Offline source should fail");
});

Check("Graph rejects invalid links", () =>
{
    MeshNetworkGraph graph = new();
    foreach (double weight in new[] { 0, -1, double.NaN, double.PositiveInfinity })
    {
        bool rejected = false;
        try { graph.AddLink(a, b, weight); }
        catch (ArgumentException) { rejected = true; }
        Assert(rejected, $"Invalid weight {weight} accepted");
    }
    bool selfLinkRejected = false;
    try { graph.AddLink(a, a, 1); }
    catch (ArgumentException) { selfLinkRejected = true; }
    Assert(selfLinkRejected, "Self-link accepted");
});

Check("Undirected graph avoids duplicate link entries", () =>
{
    MeshNetworkGraph graph = Triangle();
    graph.AddLink(b, a, 4);
    Assert(graph.GetLinks().Count == 3, "Link was duplicated");
    Assert(graph.GetLinks().Any(x => x.Weight == 4), "Updated link weight not retained");
});

Console.WriteLine($"\nAlgorithm tests: {passed} passed, {failed} failed.");
return failed == 0 ? 0 : 1;
