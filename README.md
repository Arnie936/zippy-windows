# Zippy For Windows

A native WinForms desktop assistant for Windows.

Zippy is an always-on companion that sits next to your cursor, sees your screens, listens to your voice, and can hand off work to local CLI tools.

## Features

- always-on companion beside the mouse cursor, can drive to on-screen targets
- captures all screens and sends them to Claude for screen-aware replies
- microphone recording with ElevenLabs speech-to-text or local Whisper
- ElevenLabs TTS playback
- one-shot handoffs to local Codex (`nimm codex ...`), Claude Code (`nimm claude code ...`), and OpenClaw (`nimm openclaw ...`)
- Codex can receive attached screenshots via `nimm codex mit screen ...`
- tray icon and global push-to-talk hotkey
- uses `playground/` as the default workspace for Codex-generated files
- run logs stored in `codex output/`

## Quick Start

1. Clone the repository.
2. Open the [`windows`](windows) folder.
3. Follow [`windows/README.md`](windows/README.md) for build and setup.

## Local-First Direction

Zippy is designed so it can evolve toward a fully local setup.

Already local today:

- speech-to-text via local Whisper
- Codex, Claude Code, and OpenClaw one-shot handoffs

Still cloud-backed:

- the main screenshot-aware assistant flow (Anthropic)
- TTS playback (ElevenLabs)

The intended direction is a fully local stack: local Whisper + a local vision-capable chat model (e.g. Ollama) + a local TTS engine.

## Project Layout

```text
windows/
  Clicky.Windows.cs
  Build-Clicky.cmd
  Start-Clicky.cmd
  .env.example
  README.md
SOUL.md
```

`SOUL.md` is an optional personality file. If present, Zippy loads it and injects it into the Anthropic system prompt.

## Known Limitations

- Windows-only
- no installer yet
- Codex, Claude Code, and OpenClaw handoffs are one-shot background runs, not persistent sessions
- handoff triggers are tuned for German speech variants (e.g. `nimm codex`, `nimm claude code`, `nimm openclaw`)
- speech and vision features depend on external API availability

## Credits

The core idea of an **always-on companion sitting next to the mouse cursor** is inspired by [farzaa/clicky](https://github.com/farzaa/clicky), a macOS/Swift menu-bar assistant by Farza Majeed (MIT). Thanks for that spark.

Zippy For Windows is largely a rewrite in C# / WinForms with its own architecture and a much larger feature set — notably orchestration of three local CLI agents (Codex, Claude Code, OpenClaw), direct Anthropic and ElevenLabs integrations, local Whisper STT, and a tray + hotkey workflow for Windows. The repo originally started from a local clone of the upstream, so small remnants (folder names, minor snippets) may still trace back to it.

See [`NOTICE.md`](NOTICE.md) for details on origin and attribution.

## License

MIT. See [`LICENSE`](LICENSE) and [`NOTICE.md`](NOTICE.md) for licensing and provenance notes.
