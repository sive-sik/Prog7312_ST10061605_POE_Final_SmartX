using SmartX.Core.DataStructures;
using SmartX.Core.Models;

namespace SmartX.Api.Services;

public class RealTimeProcessingStore
{
    private readonly object _lock = new();
    private readonly TelemetryAvlTree _telemetryTree = new();
    private readonly Queue<StreamTelemetryItem> _incomingTelemetry = new();
    private readonly PriorityQueue<StreamTelemetryItem, int> _criticalTelemetry = new();
    private readonly Stack<DeviceCommand> _commands = new();
    private readonly Dictionary<Guid, SensorDevice> _devices = new();
    private readonly SortedDictionary<DateTime, StreamTelemetryItem> _history = new();
    private readonly HashSet<Guid> _activeAlertSensors = new();
    private readonly Dictionary<Guid, int> _searchFrequency = new();
    private readonly Dictionary<Guid, string> _currentCommands = new();
    private readonly List<CommandHistoryEntry> _commandHistory = new();

    public void SynchroniseDevices(IEnumerable<SensorDevice> devices)
    {
        lock (_lock)
        {
            foreach (SensorDevice device in devices)
            {
                _devices[device.Id] = device;
                UpdateActiveAlert(device.Id);
            }
        }
    }

    public IReadOnlyList<SensorDevice> GetDevices()
    {
        lock (_lock) { return _devices.Values.ToList(); }
    }

    public SensorDevice? GetDevice(Guid sensorId)
    {
        lock (_lock) { return _devices.GetValueOrDefault(sensorId); }
    }

    public void EnqueueTelemetry(StreamTelemetryItem item)
    {
        lock (_lock)
        {
            if (item.IsCritical)
            {
                _criticalTelemetry.Enqueue(item, 0);
            }
            else
            {
                _incomingTelemetry.Enqueue(item);
            }
        }
    }

    public StreamTelemetryItem? ProcessNext()
    {
        lock (_lock)
        {
            // Critical alerts are always processed before normal FIFO packets.
            if (!_criticalTelemetry.TryDequeue(out StreamTelemetryItem? item, out _)
                && !_incomingTelemetry.TryDequeue(out item))
            {
                return null;
            }

            DateTime historyTime = item.Timestamp;
            // Preserve packets that arrive with the same timestamp.
            while (_history.ContainsKey(historyTime))
            {
                historyTime = historyTime.AddTicks(1);
            }
            _history.Add(historyTime, item);
            _telemetryTree.Insert(item);
            UpdateActiveAlert(item.SensorId);
            return item;
        }
    }

    private void UpdateActiveAlert(Guid sensorId)
    {
        // Check the latest reading for each metric so one normal metric cannot clear another alert.
        bool hasAlert = _history.Values.Where(item => item.SensorId == sensorId)
            .GroupBy(item => item.Metric, StringComparer.OrdinalIgnoreCase)
            .Any(group => group.OrderByDescending(item => item.Timestamp).First().AlertState != "Normal");
        string status = _devices[sensorId].Status;
        bool disconnected = status.Equals("Disconnected", StringComparison.OrdinalIgnoreCase)
            || status.Equals("Offline", StringComparison.OrdinalIgnoreCase);

        if (hasAlert || disconnected) { _activeAlertSensors.Add(sensorId); }
        else { _activeAlertSensors.Remove(sensorId); }
    }

    public IReadOnlyList<StreamTelemetryItem> GetHistoricalRange(DateTime start, DateTime end, Guid? sensorId = null)
    {
        lock (_lock) { return _telemetryTree.SearchRange(start, end, sensorId); }
    }

    public IReadOnlyList<StreamTelemetryItem> GetHistory()
    {
        lock (_lock) { return _history.Values.ToList(); }
    }

    public IReadOnlyList<StreamTelemetryItem> GetRecentHistory(int count)
    {
        lock (_lock) { return _history.Values.TakeLast(Math.Max(0, count)).ToList(); }
    }

    public IReadOnlyList<Guid> GetActiveAlertSensors()
    {
        lock (_lock) { return _activeAlertSensors.ToList(); }
    }

    public DeviceCommand? IssueCommand(Guid sensorId, string command)
    {
        lock (_lock)
        {
            if (!_devices.TryGetValue(sensorId, out SensorDevice? device)) { return null; }
            DeviceCommand entry = new()
            {
                SensorId = sensorId,
                DeviceName = device.Name,
                Command = command,
                PreviousCommand = _currentCommands.GetValueOrDefault(sensorId, string.Empty)
            };
            _commands.Push(entry);
            _currentCommands[sensorId] = command;
            AddCommandHistory(entry, "Issued");
            return entry;
        }
    }

    public DeviceCommand? UndoLastCommand()
    {
        lock (_lock)
        {
            if (!_commands.TryPop(out DeviceCommand? command)) { return null; }
            command.WasUndone = true;
            _currentCommands[command.SensorId] = command.PreviousCommand;
            AddCommandHistory(command, "Undone");
            return command;
        }
    }

    private void AddCommandHistory(DeviceCommand command, string status)
    {
        _commandHistory.Add(new CommandHistoryEntry
        {
            CommandId = command.Id,
            SensorId = command.SensorId,
            DeviceName = command.DeviceName,
            Command = command.Command,
            Timestamp = DateTime.UtcNow,
            Status = status
        });
    }

    public IReadOnlyList<CommandHistoryEntry> GetCommandHistory()
    {
        lock (_lock) { return _commandHistory.ToList(); }
    }

    public string GetCurrentCommand(Guid sensorId)
    {
        lock (_lock) { return _currentCommands.GetValueOrDefault(sensorId, string.Empty); }
    }

    public IReadOnlyList<SensorDevice> SearchDevices(string? search, string? category, bool alertOnly)
    {
        lock (_lock)
        {
            IEnumerable<SensorDevice> results = _devices.Values;
            if (!string.IsNullOrWhiteSpace(search))
            {
                string text = search.Trim();
                results = results.Where(device =>
                    device.Name.Contains(text, StringComparison.OrdinalIgnoreCase)
                    || device.MacAddress.Contains(text, StringComparison.OrdinalIgnoreCase)
                    || device.Category.Contains(text, StringComparison.OrdinalIgnoreCase)
                    || device.DeploymentLocation.Contains(text, StringComparison.OrdinalIgnoreCase)
                    || device.Id.ToString().Contains(text, StringComparison.OrdinalIgnoreCase));
            }
            if (!string.IsNullOrWhiteSpace(category))
            {
                results = results.Where(device => device.Category.Equals(category.Trim(), StringComparison.OrdinalIgnoreCase));
            }
            if (alertOnly) { results = results.Where(device => _activeAlertSensors.Contains(device.Id)); }

            List<SensorDevice> matches = results.ToList();
            if (!string.IsNullOrWhiteSpace(search) || !string.IsNullOrWhiteSpace(category) || alertOnly)
            {
                foreach (SensorDevice device in matches)
                {
                    _searchFrequency[device.Id] = _searchFrequency.GetValueOrDefault(device.Id) + 1;
                }
            }
            return matches;
        }
    }

    public IReadOnlyList<SuggestedAction> GetSuggestedActions()
    {
        lock (_lock)
        {
            List<SuggestedAction> suggestions = new();
            foreach (SensorDevice device in _devices.Values)
            {
                StreamTelemetryItem? alert = _history.Values
                    .Where(item => item.SensorId == device.Id)
                    .GroupBy(item => item.Metric, StringComparer.OrdinalIgnoreCase)
                    .Select(group => group.OrderByDescending(item => item.Timestamp).First())
                    .Where(item => item.AlertState != "Normal")
                    .OrderByDescending(item => item.IsCritical).FirstOrDefault();
                int searches = _searchFrequency.GetValueOrDefault(device.Id);
                bool active = _activeAlertSensors.Contains(device.Id);
                if (!active && searches == 0) { continue; }

                string metric = alert?.Metric ?? device.Category;
                string command = "Inspect Device";
                if (metric.Contains("moisture", StringComparison.OrdinalIgnoreCase)) { command = "Activate Water Pump"; }
                else if (metric.Contains("power", StringComparison.OrdinalIgnoreCase)) { command = "Disable Smart Relay"; }
                else if (metric.Contains("temperature", StringComparison.OrdinalIgnoreCase)) { command = "Enable Cooling"; }
                else if (metric.Contains("actuator", StringComparison.OrdinalIgnoreCase)) { command = "Toggle Actuator"; }

                suggestions.Add(new SuggestedAction
                {
                    SensorId = device.Id,
                    DeviceName = device.Name,
                    Reason = alert != null ? $"{alert.AlertState}: {alert.Metric} = {alert.Value} {alert.Unit}"
                        : active ? "Device is disconnected." : $"Appeared in {searches} searches.",
                    SuggestedCommand = command,
                    Score = (active ? (alert?.IsCritical == true ? 200 : 100) : 0) + Math.Min(searches, 99)
                });
            }
            return suggestions.OrderByDescending(item => item.Score).ToList();
        }
    }

    public object GetStatistics()
    {
        lock (_lock)
        {
            return new
            {
                DeviceCount = _devices.Count,
                NormalQueueCount = _incomingTelemetry.Count,
                CriticalQueueCount = _criticalTelemetry.Count,
                ProcessedCount = _history.Count,
                ActiveAlertCount = _activeAlertSensors.Count,
                UndoableCommandCount = _commands.Count,
                CommandHistoryCount = _commandHistory.Count,
                SearchCount = _searchFrequency.Values.Sum()
            };
        }
    }
}
