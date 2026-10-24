<h1 align="center">PLC_FX</h1>

<p align="center">
  A small, friendly <b>Windows</b> desktop app to monitor and control a
  <b>Mitsubishi FX3S-14MT</b> PLC over its serial programming port — written in C# with WinForms.
</p>

<p align="center">
  <a href="https://github.com/shaxboz1011/C-and-Mitsubishi/actions/workflows/ci.yml"><img src="https://github.com/shaxboz1011/C-and-Mitsubishi/actions/workflows/ci.yml/badge.svg" alt="CI" /></a>
  <a href="LICENSE"><img src="https://img.shields.io/badge/license-MIT-blue.svg" alt="MIT license" /></a>
  <img src="https://img.shields.io/badge/.NET%20Framework-4.7.2-512BD4" alt=".NET Framework 4.7.2" />
  <img src="https://img.shields.io/badge/PLC-Mitsubishi%20FX3S--14MT-E30613" alt="Mitsubishi FX3S-14MT" />
</p>

<p align="center">
  <b>English</b> · <a href="README.uz.md">O'zbekcha</a>
</p>

![PLC_FX — main window](docs/screenshots/app.png)

## What it does

PLC_FX connects to a Mitsubishi FX3S-14MT over the serial programming port and gives you a
live control panel:

- **Live I/O monitor.** All eight inputs (`X00`–`X07`) and six outputs (`Y00`–`Y05`) are shown
  as round indicators that turn **green** when on and **red** when off.
- **Laser detectors.** Two indicators show the left/right light barriers (lit red when the beam
  is broken).
- **Conveyor control.** Move left, move right, or stop.
- **Lamps & auxiliaries.** Toggle the red lamp, and the green lamp + cooler together.
- **Safety status.** The emergency-stop and key-switch states are read straight from the PLC.
- **Simple serial setup.** Pick a COM port, press **Connect**, and the app polls the PLC in the
  background without freezing the UI.

It is built on the open-source [HslCommunication](https://github.com/dathlin/HslCommunication)
library, which speaks the Mitsubishi FX serial protocol.

## How it works

```
┌─────────────┐    RS-422 / USB-serial    ┌──────────────────┐
│   PLC_FX    │ ◀───── 9600 7E1 ─────────▶ │  FX3S-14MT PLC   │
│  (WinForms) │     MELSEC FX protocol     │  X00–X07 inputs  │
└─────────────┘                            │  Y00–Y05 outputs │
                                           └──────────────────┘
```

A background thread polls the PLC every ~100 ms and marshals the results back to the UI thread,
so the window stays responsive and the serial line is not flooded.

### I/O map

| Address | Role                         | Shown as / controlled by |
|---------|------------------------------|--------------------------|
| `X00`   | Key switch                   | **Key** status           |
| `X03`   | Emergency stop (NC)          | **Emergency Button** status |
| `X04`   | Left laser detector          | **Laser · Left**         |
| `X05`   | Right laser detector         | **Laser · Right**        |
| `X01`,`X02`,`X06`,`X07` | General inputs | Input indicators         |
| `Y00`   | Green lamp                   | **Green Lamp** button    |
| `Y04`   | Cooler                       | toggled with green lamp  |
| `Y01`,`Y02`,`Y03`,`Y05` | General outputs | Output indicators    |
| `M16`   | Conveyor **left**            | **◀** button             |
| `M32`   | Conveyor **right**           | **▶** button             |
| `M48`   | Red lamp                     | **Red Lamp** button      |

The **Stop** (■) button clears `M16` and `M32`.

## Requirements

- **Windows 10/11**
- **[.NET Framework 4.7.2](https://dotnet.microsoft.com/download/dotnet-framework/net472)** (preinstalled on modern Windows)
- A **Mitsubishi FX3S-14MT** PLC and a serial / USB-to-serial programming cable
- [Visual Studio 2019/2022](https://visualstudio.microsoft.com/) with the *.NET desktop development* workload (to build)

### Serial settings

The app uses the fixed FX programming-port parameters, so there is nothing to configure:

| Setting   | Value |
|-----------|-------|
| Baud rate | 9600  |
| Data bits | 7     |
| Parity    | Even  |
| Stop bits | 1     |

## Build & run

```bash
git clone https://github.com/shaxboz1011/C-and-Mitsubishi.git
cd C-and-Mitsubishi
```

Open **`PLC_FX.sln`** in Visual Studio and press **F5**, or from a *Developer Command Prompt*:

```bat
nuget restore PLC_FX.sln
msbuild PLC_FX.sln /p:Configuration=Release
PLC_FX\bin\Release\PLC_FX.exe
```

NuGet restores the two dependencies automatically:

- **HslCommunication** — the Mitsubishi FX serial driver
- **Newtonsoft.Json**

## Using the app

1. Connect the programming cable and power on the PLC.
2. Launch the app, pick your **COM Port**, and click **Connect**.
   (If the list is empty, plug in the cable and reopen the app.)
3. The indicators go live. Use the **Conveyor**, **Lamp** and other buttons to drive the outputs.
4. Click **Disconnect** (or just close the window) to release the port.

## Troubleshooting

| Problem | Fix |
|---------|-----|
| COM Port list is empty | Plug in the USB-serial cable and reopen the app; check Device Manager for the port. |
| "Could not open COMx" | Another program (e.g. GX Works) is holding the port, or the wrong port is selected. |
| Connects but nothing updates | Check the cable wiring and that the PLC is in RUN; confirm it is an FX-series serial port. |

## Contributing

Issues and pull requests are welcome — see [CONTRIBUTING.md](CONTRIBUTING.md).

## License

[MIT](LICENSE) © shaxboz1011 and contributors
