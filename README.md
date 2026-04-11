# Zippy For Windows

This repo now contains only the Windows version of Zippy.

It is a native WinForms desktop assistant that:

- shows an always-on companion beside the mouse cursor
- can move the companion to a detected on-screen destination
- captures all screens
- records from the microphone and transcribes speech
- can transcribe with ElevenLabs speech-to-text or local Whisper
- sends screenshots directly to Anthropic for screen-aware replies
- plays TTS directly through ElevenLabs
- can hand off one-shot tasks to local Codex when you say `nimm codex`
- uses `playground/` as the default workspace for Codex-generated files, projects, and experiments
- stores Codex run logs in `codex output/`
- shows a tray icon
- supports a global push-to-talk hotkey

## Project Layout

```text
windows/
  Clicky.Windows.cs
  Build-Clicky.cmd
  Start-Clicky.cmd
  .env.example
  README.md
```

## Setup

1. Open `windows/`.
2. Copy `.env.example` to `.env`.
3. Fill in:
   - `ANTHROPIC_API_KEY`
   - `ELEVENLABS_API_KEY`
   - `ELEVENLABS_VOICE_ID`
   - optional: `STT_PROVIDER` (`elevenlabs` or `whisper`)
   - optional: `CODEX_COMMAND`
   - optional: `CODEX_WORKDIR`
   - optional: `CODEX_TIMEOUT_SECONDS`
   - optional: `WHISPER_PYTHON`
   - optional: `WHISPER_MODEL`
   - optional: `WHISPER_LANGUAGE`
   - optional: `PUSH_TO_TALK_KEY`
4. Run `Build-Clicky.cmd`.
5. Run `Start-Clicky.cmd`.

## Current Scope

What works:

- always-on cursor companion with state changes
- companion navigation to visible windows, controls, and other Claude-detected targets
- screenshot + Claude vision flow
- speech input with ElevenLabs speech-to-text or local Whisper
- local settings
- ElevenLabs playback
- local Codex one-shot handoff via `nimm codex ...`
- Codex writes its generated files into `playground/` by default
- Codex output logs written to `codex output/`
- tray app
- hold-to-talk button and hotkey

What is not built yet:

- richer companion art and higher-end animation polish
- packaging or installer flow

## Notes

- Secrets live in `windows/.env`
- Local settings are stored in `windows/data/settings.json`
- Codex uses `playground/` as its default working directory unless `CODEX_WORKDIR` is set
- Codex run logs are written to `codex output/zippy-codex-YYYYMMDD-HHMMSS.txt`
- The executable is built from `windows/Clicky.Windows.cs`
- `windows/Clicky.Windows.exe` is generated locally and should not be committed
