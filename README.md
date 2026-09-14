# SmartX – IoT Data Ingestion and Validation Gateway

## PROG7312 / AAPD7112 – Portfolio of Evidence Part 1

**Student Number:** ST10061605  
**Project:** SmartX  
**Technology:** .NET 10, ASP.NET Core Web API, Blazor WebAssembly, C#

---

## 1. Project Overview

SmartX is an IoT data ingestion and monitoring system developed for Part 1 of the PROG7312 Portfolio of Evidence.

The application allows users to register simulated IoT sensor devices, submit telemetry readings, monitor device health, attach supporting device files, and demonstrate advanced C# programming concepts required by the assignment.

The solution is divided into three main projects:

- `SmartX.Api` – ASP.NET Core Web API
- `SmartX.Client` – Blazor WebAssembly frontend
- `SmartX.Core` – Shared domain models, services, data structures, and reusable logic

SmartX currently focuses on the Part 1 requirement:

> Sensor Data Ingestion and Telemetry

Future sections such as command history and mesh routing are reserved for later parts of the Portfolio of Evidence.

---

## 2. Main Features

SmartX includes the following functionality:

- Sensor registration using a MAC address, name, deployment location, and category
- Duplicate MAC address validation
- Display of registered IoT devices
- Telemetry submission using multiple data types
- Generic telemetry packets using `TelemetryPacket<T>`
- Support for:
  - `double` telemetry
  - `int` telemetry
  - `bool` telemetry
- Device health monitoring
- Healthy, Warning, and Critical health states
- Context-aware troubleshooting recommendations
- Sensor attachment upload
- Support for TXT, LOG, JSON, CSV, JPG, JPEG, PNG, and PDF files
- Maximum upload size of 10 MB
- Operator overloading using power readings
- Jagged array processing
- Conversion of raw telemetry batches into `List<double>`
- Recursive validation of nested deployment structures
- SmartX C# concept demonstration page
- Realistic IoT demo data seeding

---

## 3. Solution Structure

```text
SmartX
│
├── SmartX.Api
│   ├── Controllers
│   │   ├── SensorsController.cs
│   │   ├── TelemetryController.cs
│   │   ├── HealthController.cs
│   │   └── DemoController.cs
│   │
│   ├── Services
│   │   ├── SensorStore.cs
│   │   ├── TelemetryStore.cs
│   │   └── AttachmentStore.cs
│   │
│   └── Program.cs
│
├── SmartX.Client
│   ├── Pages
│   │   ├── Home.razor
│   │   ├── Telemetry.razor
│   │   ├── Health.razor
│   │   └── Demo.razor
│   │
│   ├── Layout
│   │   └── NavMenu.razor
│   │
│   └── Program.cs
│
├── SmartX.Core
│   ├── Models
│   │   ├── SensorDevice.cs
│   │   ├── TelemetryPacket.cs
│   │   ├── PowerReading.cs
│   │   ├── DeploymentNode.cs
│   │   └── DeviceHealthStatus.cs
│   │
│   ├── Services
│   │   └── DeploymentValidator.cs
│   │
│   └── DataStructures
│       └── TelemetryBatchStore.cs
│
└── SmartX.sln
