# SmartX — PROG7312

SmartX is a student IoT simulation built with .NET 10. It contains:

- `SmartX.Api`: ASP.NET Core controllers and in-memory stores.
- `SmartX.Client`: the Blazor WebAssembly interface.
- `SmartX.Core`: shared models and data structures for Parts 1–3.

## Build and Run

Install the .NET 10 SDK. From the repository root, build the solution:

```powershell
dotnet build SmartX.slnx
```

Start the API in one terminal:

```powershell
dotnet run --project SmartX.Api --launch-profile http
```

Start the client in another terminal:

```powershell
dotnet run --project SmartX.Client --launch-profile http
```

Open `http://localhost:5281`. The configured client uses the API at `http://localhost:5182`.
Keep both applications running. Registered devices, telemetry and commands are stored in memory and reset when the API restarts.

## Part 1: Sensor Data and Device Health

Open **Sensor Data** at `/telemetry` to register devices with a unique MAC address, name, deployment location and category. Select a registered sensor when submitting telemetry. Part 1 supports double, integer and boolean readings, as well as sensor attachments.

Open **Device Health** at `/health` to review readings, warnings and troubleshooting guidance. Use its refresh button after submitting Part 1 telemetry. A device with no readings, or no recent readings for more than two minutes, is reported as critical.

The existing Part 1 demonstration API includes:

- `GET /api/demo/operator-overloading`: combines and compares power readings.
- `GET /api/demo/telemetry-batches`: processes jagged telemetry arrays into a list.
- `GET /api/demo/recursive-validation`: validates a nested deployment using recursion.

Part 1 telemetry and Part 2 stream telemetry use separate stores. Part 2 shares the registered sensors, but its demo packets do not populate the Part 1 Device Health page.

## Part 2: Real-Time Command Stream

Open **Real-Time Command Stream** from navigation or the active Part 2 Home card. Its route is `/command-stream` (`http://localhost:5281/command-stream`).

The dashboard contains:

- Queue statistics for registered devices, the standard queue, priority alerts, active alerts and command history.
- Asynchronous device search with category and active-alert filters. Choose **Apply Filters** to update results without reloading the page.
- Device selection and manual override commands for pumps, smart relays, actuators and device restart.
- **Undo Last Command** to restore the previous command for the last commanded device.
- The latest 20 processed telemetry readings, displayed chronologically with Normal, Warning and Critical badges.
- Active alerts identifying sensors that need attention.
- **Automated Insights** showing recommended commands, reasons and priority scores.

Use **Seed Part 2 Demo Data** to enqueue example readings and **Process Next Telemetry** to process one packet at a time. Seeding refreshes the dashboard, but packets only enter history and influence telemetry alerts after processing. **Refresh Dashboard** loads current data manually; there are no timers or SignalR connections. Commands update simulated device state rather than controlling physical hardware.

## Testing the Part 2 Data Structures

The collections are declared and used in `SmartX.Api/Services/RealTimeProcessingStore.cs`.

### Queue<T>

1. Register at least one sensor in **Sensor Data**.
2. Open `/command-stream` and click **Seed Part 2 Demo Data** once with empty queues.
3. Confirm **Standard Queue** increases. Non-critical readings, including warnings, enter `Queue<StreamTelemetryItem>`.
4. Use **Process Next Telemetry** until **Priority Alerts** reaches zero, then continue processing.
5. Observe the processed-packet messages: standard packets leave in first-in-first-out (FIFO) order. For one seed batch, the standard readings are soil moisture `45 %`, power `3500 W`, then temperature `25 °C`.

### PriorityQueue

1. Seed demo data and confirm **Priority Alerts** is greater than zero.
2. Click **Process Next Telemetry** while both queues contain packets.
3. Confirm the result message identifies a **Critical** packet and the priority count decreases while the standard count stays unchanged.
4. Continue until all critical packets have been processed. Only then will standard packets be processed.

`PriorityQueue<StreamTelemetryItem, int>` holds critical telemetry. `ProcessNext()` checks it before the FIFO queue. Critical packets have equal priority to each other, so their relative processing order is not guaranteed.

### Stack<T>

1. Select a device.
2. Issue **Activate Water Pump**, then **Disable Water Pump**.
3. Click **Undo Last Command**.
4. Confirm the message says **Disable Water Pump** was undone and **Activate Water Pump** was restored. Review the **Undone** entry in command history.

`Stack<DeviceCommand>` uses last-in-first-out (LIFO) behaviour: the most recently issued command is undone first. The stack is shared across devices, so undo always applies to the latest command overall.

### Dictionary<Guid, SensorDevice>

Register sensors in **Sensor Data**, then refresh the command stream dashboard. Confirm they appear, select one and issue a command. Registered devices are synchronised from the Part 1 `SensorStore` into `Dictionary<Guid, SensorDevice>` for fast lookup by sensor ID. The controller also uses this lookup to validate telemetry and command targets.

### SortedDictionary<DateTime, StreamTelemetryItem>

Seed packets and process several readings. Review **Live Telemetry History** and confirm timestamps are displayed in chronological order. `SortedDictionary<DateTime, StreamTelemetryItem>` keeps processed history sorted by timestamp, even when critical packets were processed first. A timestamp collision receives a slightly adjusted dictionary key so both packets are retained. Use the processed-packet message, rather than the history row order, to demonstrate processing priority.

### HashSet<Guid>

Seed and process a critical packet, then repeat seeding and processing for the same registered sensors. Review **Active Alerts**: each sensor appears only once, even if it has repeated alerts or alerts for several metrics. `HashSet<Guid>` stores unique active alert or disconnected sensor IDs and prevents duplicate IDs. The latest processed reading for each metric determines whether a telemetry alert remains active.

## Predictive Action Engine

Automated Insights uses simple rules based on actual processed alerts and device search history; it does not generate random recommendations or use a machine learning model.

- Each device returned by a filtered search increases its counter in `Dictionary<Guid, int>`. Refreshing with filters applied also counts the matching results. An unfiltered device listing does not increase counters.
- Frequently searched devices can become recommendations even without an active alert. One matching search is enough to make a device eligible.
- Active alerts influence recommendations and receive higher scores than search-only recommendations.
- Critical alerts receive a base score of `200`; other active alerts receive `100`. Search frequency adds up to `99`, so critical suggestions rank above other alerts, and alerts rank above search-only suggestions.
- Recommendations show the device, reason, suggested command and score. They do not execute automatically.

Example alert recommendations:

| Reading | Suggested command |
| --- | --- |
| Low soil moisture | Activate Water Pump |
| High power | Disable Smart Relay |
| High temperature | Enable Cooling |

Part 2 thresholds are soil moisture below `20` critical / below `30` warning, temperature above `40` critical / above `35` warning, and power above `4500` critical / above `3000` warning. Other numeric values are normal. The demo seed includes normal temperature; to demonstrate **Enable Cooling**, submit a temperature reading above `35` to `POST /api/realtime/telemetry` using a registered sensor ID, then process it:

```json
{
  "sensorId": "REPLACE_WITH_REGISTERED_SENSOR_ID",
  "metric": "Temperature",
  "value": "42",
  "unit": "°C"
}
```

## Part 2 Demo Flow

1. Start the API using the command above.
2. Start the Client and open `http://localhost:5281`.
3. Register at least two or three sensors in **Sensor Data**, for example moisture, power and temperature sensors with distinct MAC addresses.
4. Open **Real-Time Command Stream**.
5. Click **Seed Part 2 Demo Data** once.
6. Observe **Standard Queue** and **Priority Alerts**. With at least two sensors and initially empty queues, expect three standard packets and two critical packets.
7. Click **Process Next Telemetry** repeatedly, reading each result message.
8. Confirm critical telemetry processes first, followed by FIFO standard packets.
9. Search/filter devices using **Apply Filters**. Try a category and **Active alerts only**.
10. Select a device from the results.
11. Issue two manual commands.
12. Click **Undo Last Command** and confirm the second command was undone.
13. Review **Command History**, including the issued and undone statuses.
14. Review **Active Alerts** and confirm each affected sensor appears once.
15. Review **Automated Insights**, including reasons, commands and scores.

## Part 3: Network Topology and Mesh Routing

Open **Network Topology & Mesh Routing** at `/network-topology` or use the active Part 3 Home card. This module operates on registered devices and simulated weighted wireless links.

- **AVL history:** `SmartX.Core/DataStructures/TelemetryAvlTree.cs` indexes processed Part 2 telemetry by timestamp. Duplicate timestamps are grouped; inclusive range searches optionally filter by sensor ID. The API endpoint is `GET /api/telemetry-history/range?start=...&end=...&sensorId=...` (sensor ID optional).
- **Graph:** `SmartX.Core/DataStructures/MeshNetworkGraph.cs` stores undirected weighted links using adjacency dictionaries.
- **BFS:** `FindRoute` returns a shortest-hop route; `BreadthFirstSearch` returns reachable nodes. BFS does not minimise the sum of weights.
- **Prim:** `MinimumSpanningTree` returns a minimum spanning forest when the mesh is disconnected. The dashboard highlights its links and shows total weight.
- **Failure simulation:** The API excludes manually marked offline nodes from routing and MST calculations. Links and failure states are in memory, not physical ESP32 connectivity.

### Part 3 Demonstration

1. Register three devices (A, B, C) on Sensor Data with distinct MAC addresses.
2. Open Network Topology and add links A–B (weight 2), B–C (weight 3), and A–C (weight 10).
3. Confirm three nodes and three links are shown. The blue MST edges should be A–B and B–C, with total weight 5.
4. Find a route A to C. BFS should choose the direct one-hop link A–C, even though it has a higher weight than A–B–C.
5. Simulate failure of B and confirm the MST and reachable paths update. Restore B.
6. In Real-Time Command Stream, seed and process telemetry packets. Back on Network Topology, search a UTC range containing the processed timestamps to verify the AVL index.
7. Restarting the API clears all in-memory data. Register devices and links again if needed.

### Mesh API

- `GET /api/mesh/topology`
- `GET /api/mesh/route?fromId=...&toId=...`
- `GET /api/mesh/connectivity?fromId=...`
- `POST /api/mesh/links` with JSON `{"fromId":"...","toId":"...","weight":2}`
- `POST /api/mesh/devices/{deviceId}/status` with JSON `{"offline":true}`

### Scope and Future Improvements

The mesh is a simulation: links and weights are entered by the user, not discovered from real ESP32 radios. Consider persistent storage, authenticated administrative actions, real link-quality measurements, automated tests and live updates for a production system.

