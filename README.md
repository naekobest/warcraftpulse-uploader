# WarcraftPulse Uploader

A lightweight Windows desktop app that watches your WoW combat log directory and automatically uploads logs to [warcraftpulse.com](https://warcraftpulse.com).

Supports **WoW Classic Era** and **Season of Discovery**.

## Features

- **Auto-watch** — detects new log files the moment WoW finishes writing them
- **Manual upload** — pick any `.txt` log file via the file dialog or tray menu
- **Duplicate detection** — skips files you've already uploaded (based on file hash)
- **Upload history** — lists past uploads with zone, encounter count, size, and a direct link to the report
- **Secure token storage** — API token is encrypted at rest via Windows DPAPI
- **Minimize to tray** — keeps running in the background without cluttering the taskbar
- **Start with Windows** — optional autostart via the Windows registry

## Requirements

- Windows 10 or later
- .NET 8 Desktop Runtime
- A [warcraftpulse.com](https://warcraftpulse.com) account with an API token

## Getting started

1. Download and run the installer.
2. Open **Settings** and paste your API token (from your account page on warcraftpulse.com).
3. The app auto-detects your WoW log directory. If detection fails, set the path manually in Settings.
4. Make sure **Advanced Combat Logging** is enabled in WoW (`/combatlog` in-game).
5. Play — the uploader handles the rest.

## Building from source

```
dotnet build WarcraftPulseUploader.sln
```

Requires the .NET 8 SDK with Windows targeting enabled.

## Project structure

```
WarcraftPulseUploader/
  Forms/          WinForms UI (MainForm, SettingsForm)
  Parser/         Combat log parser (CombatLogParser, CombatLogData)
  Services/       App logic (UploadService, LogWatcher, UploadHistory, AppSettings)
  Native/         Dark mode helpers (DarkMode, SetWindowTheme)

WarcraftPulseUploader.Tests/
  Parser/         Unit tests for the combat log parser
```
