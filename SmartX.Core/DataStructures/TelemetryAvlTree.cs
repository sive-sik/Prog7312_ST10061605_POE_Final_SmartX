using SmartX.Core.Models;

namespace SmartX.Core.DataStructures;

public class TelemetryAvlTree
{
    private TelemetryAvlNode? root;

    public int Count { get; private set; }

    public void Insert(StreamTelemetryItem reading)
    {
        ArgumentNullException.ThrowIfNull(reading);
        root = InsertNode(root, reading);
        Count++;
    }

    public List<StreamTelemetryItem> SearchRange(DateTime start, DateTime end, Guid? sensorId = null)
    {
        if (start > end)
        {
            throw new ArgumentException("The start date cannot be after the end date.");
        }

        List<StreamTelemetryItem> results = new();
        SearchRange(root, start, end, sensorId, results);
        return results;
    }

    private static int Height(TelemetryAvlNode? node) => node?.Height ?? 0;

    private static void UpdateHeight(TelemetryAvlNode node)
    {
        node.Height = 1 + Math.Max(Height(node.Left), Height(node.Right));
    }

    private static int Balance(TelemetryAvlNode node) => Height(node.Left) - Height(node.Right);

    private static TelemetryAvlNode RotateRight(TelemetryAvlNode node)
    {
        TelemetryAvlNode newRoot = node.Left!;
        node.Left = newRoot.Right;
        newRoot.Right = node;
        UpdateHeight(node);
        UpdateHeight(newRoot);
        return newRoot;
    }

    private static TelemetryAvlNode RotateLeft(TelemetryAvlNode node)
    {
        TelemetryAvlNode newRoot = node.Right!;
        node.Right = newRoot.Left;
        newRoot.Left = node;
        UpdateHeight(node);
        UpdateHeight(newRoot);
        return newRoot;
    }

    private static TelemetryAvlNode InsertNode(TelemetryAvlNode? node, StreamTelemetryItem reading)
    {
        if (node == null)
        {
            return new TelemetryAvlNode(reading);
        }

        if (reading.Timestamp < node.Timestamp)
        {
            node.Left = InsertNode(node.Left, reading);
        }
        else if (reading.Timestamp > node.Timestamp)
        {
            node.Right = InsertNode(node.Right, reading);
        }
        else
        {
            // Multiple sensors can report at the same timestamp.
            node.Readings.Add(reading);
            return node;
        }

        UpdateHeight(node);
        int balance = Balance(node);

        // AVL rotations keep searches efficient even when readings arrive in time order.
        if (balance > 1)
        {
            if (Balance(node.Left!) < 0)
            {
                node.Left = RotateLeft(node.Left!);
            }

            return RotateRight(node);
        }

        if (balance < -1)
        {
            if (Balance(node.Right!) > 0)
            {
                node.Right = RotateRight(node.Right!);
            }

            return RotateLeft(node);
        }

        return node;
    }

    private static void SearchRange(
        TelemetryAvlNode? node,
        DateTime start,
        DateTime end,
        Guid? sensorId,
        List<StreamTelemetryItem> results)
    {
        if (node == null)
        {
            return;
        }

        // Skip entire subtrees outside the requested timestamp range.
        if (start < node.Timestamp)
        {
            SearchRange(node.Left, start, end, sensorId, results);
        }

        if (node.Timestamp >= start && node.Timestamp <= end)
        {
            foreach (StreamTelemetryItem reading in node.Readings)
            {
                if (!sensorId.HasValue || reading.SensorId == sensorId.Value)
                {
                    results.Add(reading);
                }
            }
        }

        if (end > node.Timestamp)
        {
            SearchRange(node.Right, start, end, sensorId, results);
        }
    }
}
