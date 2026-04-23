# KSP Scientific Calculator

A small scientific calculator plugin for Kerbal Space Program 1.

## Features

- Toolbar button via ToolbarController
- Click-through-safe window via ClickThroughBlocker
- Available in Flight, VAB, and SPH
- Scientific functions:
  - `sin`, `cos`, `tan`
  - `sinh`, `cosh`, `tanh`
  - `asin`, `acos`, `atan`
  - `sqrt`, `ln`, `log`, `abs`, `exp`
  - `floor`, `ceil`, `round`
- Constants: `pi`, `e`, `Ans`
- DEG/RAD toggle
- Calculation history
- Persistent window position and basic UI settings
- F10 hotkey to toggle the window

## Source layout

```text
KSPScientificCalculator/
├─ Source/
│  ├─ AssemblyInfo.cs
│  └─ KSPScientificCalculator.cs
├─ KSPScientificCalculator.csproj
├─ README.md
├─ CHANGELOG.md
├─ LICENSE.txt
└─ GameData/
   └─ KSPScientificCalculator/
      ├─ Plugins/
      │  └─ KSPScientificCalculator.dll
      ├─ PluginData/
      │  ├─ settings.cfg
      │  └─ Textures/
      │     ├─ icon_24.png
      │     └─ icon_38.png
      └─ KSPScientificCalculator.version
```

## Notes

- The delete/backspace button is labeled `Del`.
- No implicit multiplication yet. Use `2*pi`, not `2pi`.
- Hyperbolic functions use standard .NET implementations.
