# Real-Time CAN Telemetry Application

WPF application for receiving, decoding, and displaying UGRacing CAN telemetry in real time. It supports Peak PCAN USB, a wireless TCP receiver, and a selectable TEST mode with simulated track-style data.

## Quick Start

1. Open `WPF CAN Tool/WPF CAN Tool.csproj` in Visual Studio.
2. Press F5.
3. At startup, choose:
   - **Yes** for TEST mode with simulated data.
   - **No** for normal hardware mode.
4. In normal mode, choose:
   - **Yes** for Peak PCAN USB.
   - **No** for wireless datalogger.

TEST mode sets the window title to `[TEST MODE]` and drives all pages with simulated CAN frames.

## Current Data Flow

```text
CAN Receiver (Peak / Wireless / Test)
  -> CanFrame
  -> MainWindow filter by CAN ID
  -> CanFrameDecoder decodes DBC signals
  -> MainWindow maps decoded signal names to CarStateViewModel
  -> WPF bindings update the pages
```

CAN IDs select the DBC message in `Models/CanFrameDecoder.cs`. Application-specific processing is mostly by decoded signal name in `MainWindow.xaml.cs`, especially `UpdateCarState`, `UpdateTsState`, and `UpdateSdcState`.

## DBC Loading

`Models/DbcFileManager.cs` tries to load a DBC in this order:

1. Download from `UGRacing-Electronics/UGR_CANDBCFile`.
2. Use local cached `CAN_Config.dbc` / `UGR_Main_Bus.dbc`.
3. Ask user to manually select a DBC.

`CAN_Config.dbc` and `PCANBasic.dll` are copied to the build output by the project file.

## Supported Messages

| Message | Signals Used | Status |
|---------|--------------|--------|
| `Accelerator_Percentage` | `Accelerator_Percentage` | Mapped |
| `Brake_Percentage` | `Brake_Percentage`, `BOTS_Status`, `APPS_Status` | Mapped |
| `Wheelspeed` | `FL/FR/RL/RR_Wheelspeed`, `FL/FR/RL/RR_BrakeTemp` | Mapped, multiplexed |
| `Inverter_Return` | `N_actual` | Mapped |
| `Inverter_Command` | `Torque_Command`, `Speed_Command` | Shown on TS Overview |
| `Energy_Setting` | `Energy_Setting_Request` | Mapped |
| `SDC_Node_Failed` | `SDC_Node_Number` | Mapped to SDC UI order |
| `TS_Placeholder` (`0x710`) | TS overview placeholder values | Injected only if real TS signals are missing |
| `SDC_Placeholder` (`0x711`) | `SDC_Node_Number` | Injected only if the DBC does not include `SDC_Node_Number` |

The decoder supports signed/unsigned values, Intel and Motorola byte order, and multiplexed messages.

## TS Overview Signals

The TS Overview page displays:

| UI Value | Preferred Signal | Accepted Aliases |
|----------|------------------|------------------|
| Accumulator voltage | `AccumulatorVoltage` | `Accumulator_Voltage`, `PackVoltage`, `Pack_Voltage`, `TS_Voltage` |
| Accumulator current | `AccumulatorCurrent` | `Accumulator_Current`, `PackCurrent`, `Pack_Current`, `TS_Current` |
| Min cell temp | `MinCellTemp` | `Min_Cell_Temp`, `Minimum_Cell_Temp` |
| Avg cell temp | `AvgCellTemp` | `Average_Cell_Temp`, `Avg_Cell_Temp` |
| Max cell temp | `MaxCellTemp` | `Max_Cell_Temp`, `Maximum_Cell_Temp` |
| Hottest cell ID | `HottestCellId` | `Hottest_Cell_Id`, `Max_Cell_Id` |
| Torque command | `Torque_Command` | none |
| Speed command | `Speed_Command` | none |

`PackPower`, `AccumulatorStatus`, and `HottestCellInfo` are derived in `CarStateViewModel.cs`.

## SDC Node Mapping

The SDC page is driven by `SDC_Node_Failed` / `SDC_Node_Number`. It is not expecting individual node messages.

`SDC_Node_Number` is the failed node index in UI order:

| Value | Node |
|-------|------|
| 0 | LVMS |
| 1 | BSPD |
| 2 | Kill Switch |
| 3 | Inertia Switch |
| 4 | BOTS |
| 5 | APPS |
| 6 | LHS SB |
| 7 | RHS SB |
| 8 | Latchboard |
| 9 | DC |
| 10 | HVD |
| 11 | MC |
| 12 | Self Latch |
| 13 | TSAL Switch |
| 14 | TSMS |
| 15 | Precharge |

Any value outside `0..15` is treated as no failed node and all SDC nodes are shown green.

## Placeholder Messages

If the DBC does not contain the required TS or SDC signals, `MainWindow.xaml.cs` injects these in-memory placeholder definitions so TEST mode and UI development still work:

```text
BO_ 1808 TS_Placeholder: 8
  SG_ AccumulatorVoltage : 0|16@1+  (0.1,0) "V"
  SG_ AccumulatorCurrent : 16|16@1- (0.1,0) "A"
  SG_ MinCellTemp        : 32|8@1+  (1,0) "degC"
  SG_ AvgCellTemp        : 40|8@1+  (1,0) "degC"
  SG_ MaxCellTemp        : 48|8@1+  (1,0) "degC"
  SG_ HottestCellId      : 56|8@1+  (1,0) ""

BO_ 1809 SDC_Placeholder: 1
  SG_ SDC_Node_Number    : 0|8@1+ (1,0) ""
```

These are app-side placeholders. They do not need to be added to the real DBC unless you want the test messages documented externally.

## TEST Mode

`TestCanReceiver.cs` generates a simple track-style lap:

- Straights, a left-hand sweeper, and a right-hand hairpin/final bend.
- Acceleration on exits and straights.
- Braking before corners.
- Left/right wheel speed differences in corners.
- Brake temperatures rising under braking and cooling on straights.
- Pack voltage around 255 V and peak simulated power around 8 kW.
- `SDC_Node_Number` normally reports no failed node, with one node failing occasionally for UI testing.

## Peak PCAN Notes

`PCANReceiver.cs`:

- Initializes `PCAN_USBBUS1` at `TPCANBaudrate.PCAN_BAUD_500K`.
- Copies `PCANBasic.dll` to output via the project file.
- Logs PCAN init/read errors to the Debug Output window.
- Normalizes extended PCAN IDs by OR-ing `0x80000000`, matching Vector-style extended DBC IDs.

If Peak connects but frames do not update, check:

- PCAN drivers installed.
- Correct USB channel.
- Bus speed is 500 kbit/s.
- CAN bus termination.
- DBC IDs match actual bus traffic.
- `PCANBasic.dll` architecture matches the app runtime.

## Wireless Receiver

`WirelessCANReceiver.cs` currently uses hardcoded values:

```csharp
private const string DefaultHost = "192.168.1.100";
private const int DefaultPort = 5000;
```

Update these when the datalogger IP/port is known.

## CAN Logging and Playback

The left navigation includes CAN log controls:

- **Start Recording**: saves received raw CAN frames to a `.canlog` file.
- **Stop Recording**: closes the current log file.
- **Playback Log**: replays a saved log through the same decoder and UI update path.

The log format is CSV:

```text
ElapsedMs,IdHex,IsExtended,Dlc,DataHex
0,110,False,1,C0
52,330,False,4,0019003C
```

Playback uses the recorded timing, capped to short delays so long logs remain practical to review.

## Adding a New UI Signal

1. Add the signal to the DBC.
2. Add a property to `CarStateViewModel.cs`.
3. Map the decoded signal name in `MainWindow.xaml.cs`.
4. Bind the property in XAML.

The app does not require a `switch` on CAN ID for most UI values; prefer mapping decoded signal names.

## Build

```powershell
dotnet build "WPF CAN Tool\WPF CAN Tool.csproj"
```

If OneDrive locks generated files in `bin` or `obj`, build from Visual Studio or clean those generated folders after closing the app/IDE.

## Known Gaps

- TS cell/module heat map is marked TBD until module-level CAN details exist.
- Wireless host/port are not configurable in the UI.
- Only one DBC is active at a time.
