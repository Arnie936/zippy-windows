# Zippy - Agent Instructions

`CLAUDE.md` points to this file.

## Overview

This repository is now Windows-only.

The app is a WinForms desktop client in `windows/` that:

- reads API secrets from `windows/.env`
- shows an always-on cursor companion overlay with state-based visuals
- can move the companion to detected on-screen targets from Claude point tags
- records microphone audio and transcribes it with ElevenLabs speech-to-text or a local Whisper Python install
- calls Anthropic directly for screenshot + vision chat
- calls ElevenLabs directly for TTS
- can route one-shot requests to a local Codex CLI run when the prompt contains `nimm codex`
- can route one-shot requests to a local Claude Code CLI run when the prompt contains `nimm claude code`
- can attach screenshots to Codex runs for prompts like `nimm codex mit screen`
- uses `playground/` in the repo root as the default Codex working directory
- writes Codex run logs to `codex output/` in the repo root
- stores non-secret local settings in `windows/data/settings.json`
- uses a tray icon, a hold-to-talk button, and a global push-to-talk hotkey

## Key Files

| File | Purpose |
|------|---------|
| `windows/Clicky.Windows.cs` | Main Windows app source. User-facing name is Zippy even though the file name still says Clicky. |
| `windows/Build-Clicky.cmd` | Builds `Clicky.Windows.exe` with the installed .NET Framework C# compiler. |
| `windows/Start-Clicky.cmd` | Launches the app, building first if needed. |
| `windows/.env.example` | Template for local API secrets. |
| `windows/README.md` | Windows app usage notes. |
| `NOTICE.md` | Short provenance note for the restarted Zippy repository. |
| `.gitignore` | Ignores local secrets, generated state, and Windows build artifacts. |

## Build & Run

```cmd
cd windows
Build-Clicky.cmd
Start-Clicky.cmd
```

## Conventions

- Keep the repo focused on the Windows app only.
- Do not reintroduce macOS, Xcode, Worker, or Cloudflare-specific code unless the user explicitly asks for it.
- Keep secrets out of source files. Use `windows/.env`.
- Keep generated local state in `windows/data/`.
- Keep `playground/` out of git.
- Keep `codex output/` out of git.
- Keep `windows/Clicky.Windows.exe` and other generated local artifacts out of git.

## Self-Update

When the Windows app structure changes materially, update this file.
