namespace SmartX.Core.DataStructures;

public class TelemetryBatchStore
{
    private readonly double[][] _rawTelemetryBatches;

    public List<double> ProcessedTelemetry { get; } = new();

    public TelemetryBatchStore(double[][] rawTelemetryBatches)
    {
        _rawTelemetryBatches = rawTelemetryBatches;
    }

    public void ProcessBatches()
    {
        ProcessedTelemetry.Clear();

        foreach (double[] batch in _rawTelemetryBatches)
        {
            foreach (double reading in batch)
            {
                ProcessedTelemetry.Add(reading);
            }
        }
    }

    public double[][] GetRawBatches()
    {
        return _rawTelemetryBatches;
    }
}