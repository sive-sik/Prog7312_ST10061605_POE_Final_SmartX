namespace SmartX.Client.Services;

public sealed class CommandStreamState
{
    public string Search { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public bool AlertOnly { get; set; }
    public Guid? SelectedDeviceId { get; set; }
}
