# Contributing to PLC_FX

Thanks for your interest! Bug reports, features and docs are all welcome.

## Getting started

1. Install [Visual Studio 2019/2022](https://visualstudio.microsoft.com/) with the
   **.NET desktop development** workload (this includes .NET Framework 4.7.2 and MSBuild).
2. Fork and clone the repository.
3. Open **`PLC_FX.sln`** and build. NuGet restores `HslCommunication` and `Newtonsoft.Json`
   automatically; from the command line run `nuget restore PLC_FX.sln` first.

You do not need a physical PLC to build or to work on the UI. Testing live control, of course,
needs an FX3S-14MT and a serial cable.

## Where things live

| Area | Location |
|---|---|
| App entry point | `PLC_FX/Program.cs` |
| Window logic (connect, polling, controls) | `PLC_FX/Form1.cs` |
| UI layout (designer-generated) | `PLC_FX/Form1.Designer.cs` |
| Button/indicator images | `PLC_FX/Images/` |
| Dependencies | `PLC_FX/packages.config` |

Keep the hand-written logic in `Form1.cs`. Change `Form1.Designer.cs` through the Visual Studio
designer rather than by hand where you can.

## Pull requests

- Keep each PR focused on one change.
- Make sure the solution still **builds in Release** before you push.
- If you change the window, add a screenshot to the PR description.
- Use clear commit messages, e.g. `Guard against missing COM port on Connect`.

## Reporting bugs

Open an issue with your Windows version, the PLC model, the COM port / cable you use, the steps to
reproduce, and what you expected to happen.
