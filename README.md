# UtilityBelt

A small, **WPF "tray app" template** intended to be **copied and customized** into your own utility launcher.

This repo is meant to be a clean starting point:
- configurable button grid
- optional icon + label rendering
- always-on-top toolbar style UI
- JSON-based configuration (`appsettings.json`)

> If you’re looking for a “finished” app with a locked-in feature set, this is probably not it, treat this project as a **starter template**.

---

## Quick start

### Prerequisites
- Windows
- .NET SDK 8.x

### Build

```powershell
# from repo root
cd .\src\UtilityBelt.App

dotnet build -c Release
```

### Run

```powershell
# from repo root
cd .\src\UtilityBelt.App

dotnet run
```

---

## Configuration

On first run, the app will create a user config at:

- `%AppData%\UtilityBelt\appsettings.json`

Defaults come from:

- `src/UtilityBelt.App/appsettings.default.json`

### UI settings

Common knobs:
- `UI.Width` – window width
- `UI.Columns` / `UI.MaxRows` – grid layout
- `UI.AlwaysOnTop` – keep toolbar above other windows
- `UI.ButtonMinHeight` – minimum button height
- `UI.ButtonMinWidth` – minimum button width (set `0` for “auto”)
- `UI.ButtonPaddingX` / `UI.ButtonPaddingY` – button padding
- `UI.ButtonIconSize` / `UI.ButtonIconOnlySize` – icon sizing
- `UI.ButtonTextFontSize` / `UI.ButtonTextWithIconFontSize` – text sizing

### Buttons
Buttons are configured in `appsettings.json` (exact schema may evolve). Typically you’ll provide:
- a label
- optional icon (`PathData`)
- a command/action
- colors

---

## Using this repo as a template (recommended)

### Option A: “Copy and rename” (simple)
1. Copy the repository folder.
2. Rename the solution and project(s).
3. Search/replace namespaces (e.g., `UtilityBelt.App`).
4. Update `appsettings.default.json` defaults.

### Option B: GitHub template repo
If you publish on GitHub, you can mark the repository as a **Template repository** so others can click **Use this template**.

---

## Publish / release checklist

Before you publish a release, consider:
- **Versioning**: set an app version (if you haven’t already).
- **Signing** (optional): code-signing certificate if distributing broadly.
- **Self-contained** (optional): publish with the .NET runtime included.
- **Config migration**: if you change config schema, handle backward compatibility.
- **Security**: avoid shipping any secrets in `appsettings.default.json`.

Example publish (framework-dependent):

```powershell
cd .\src\UtilityBelt.App

dotnet publish -c Release -r win-x64
```

---

## License

MIT License. See [LICENSE](./LICENSE).
