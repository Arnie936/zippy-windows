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

## Requirements

- Windows
- an Anthropic API key for screen-aware chat
- an ElevenLabs API key and voice ID for speech features
- optional: a local Whisper Python setup if you want to use `STT_PROVIDER=whisper`
- optional: a local Codex CLI install if you want to use the `nimm codex ...` handoff flow

## Quick Start

1. Clone the repository.
2. Open [`windows`](/C:/Users/Arnold/Desktop/clip%20advanced/clicky/windows).
3. Copy `.env.example` to `.env`.
4. Fill in the required API keys.
5. Run `Build-Clicky.cmd`.
6. Run `Start-Clicky.cmd`.

Without Codex installed, Zippy still works for normal screenshot + voice workflows.
Without local Whisper installed, Zippy still works if `STT_PROVIDER=elevenlabs`.

## Project Layout

```text
windows/
  Clicky.Windows.cs
  Build-Clicky.cmd
  Start-Clicky.cmd
  .env.example
  README.md
```

## Configuration

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

## `.env` Variables

- `ANTHROPIC_API_KEY`
  Required for the main screenshot + vision assistant flow.
- `ELEVENLABS_API_KEY`
  Required for speech-to-text with ElevenLabs and for TTS playback.
- `ELEVENLABS_VOICE_ID`
  Required for ElevenLabs TTS playback.
- `STT_PROVIDER`
  Optional. Use `elevenlabs` or `whisper`.
- `CODEX_COMMAND`
  Optional. Path or command name for the local Codex CLI PowerShell entry point.
- `CODEX_WORKDIR`
  Optional. Defaults to `playground/`.
- `CODEX_TIMEOUT_SECONDS`
  Optional. Timeout for one-shot Codex runs.
- `WHISPER_PYTHON`
  Optional. Python command used for local Whisper.
- `WHISPER_MODEL`
  Optional. Whisper model name, for example `base`.
- `WHISPER_LANGUAGE`
  Optional. Speech language hint, currently defaulting to `de`.
- `PUSH_TO_TALK_KEY`
  Optional. Global hotkey, default `F8`.

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

## With And Without Codex

- If `codex.ps1` is available locally, prompts that start with `nimm codex ...` are handed off to a one-shot local Codex run.
- If Codex is not installed, the normal Zippy assistant still works. Only the Codex handoff flow is unavailable.

## Known Limitations

- Windows-only
- no installer yet
- the Codex handoff is currently a one-shot background run, not a persistent multi-turn session
- the Codex trigger is optimized for German speech variants around `nimm codex`
- speech and vision features depend on external API availability

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
- The repository is licensed under MIT. See `LICENSE` and `NOTICE.md` for current licensing and provenance notes.
