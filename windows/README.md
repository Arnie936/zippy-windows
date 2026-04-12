# Zippy For Windows — Build & Setup

Build, configure, and run the native Windows client. For a feature overview, see the [root README](../README.md).

## Requirements

- Windows
- an Anthropic API key
- an ElevenLabs API key and voice ID
- optional: local Whisper (Python) if you want `STT_PROVIDER=whisper`
- optional: local Codex CLI for `nimm codex ...`
- optional: local Claude Code CLI for `nimm claude code ...`
- optional: local OpenClaw CLI for `nimm openclaw ...`

## Build

Double-click `Build-Clicky.cmd`, or from `cmd.exe`:

```cmd
path\to\clicky\windows\Build-Clicky.cmd
```

This compiles `Clicky.Windows.cs` into `Clicky.Windows.exe` using the classic .NET Framework compiler already available on Windows.

## First Run

1. Copy `.env.example` to `.env` in this folder.
2. Fill in the required keys (see below).
3. Run `Start-Clicky.cmd` (or `Clicky.Windows.exe` directly).
4. Click `reload .env`, then `test apis`.
5. Either type a prompt and click `ask about my screen`, or hold the `hold to talk` button / push-to-talk hotkey (default `F8`).
6. To hand off a one-shot task, start the prompt with `nimm codex ...`, `nimm codex mit screen ...`, `nimm claude code ...`, or `nimm openclaw ...`.

Without Codex/Claude Code/OpenClaw installed, the normal assistant still works — only the matching handoff flow is unavailable. Without local Whisper, set `STT_PROVIDER=elevenlabs`.

## `.env` Variables

### Required

- `ANTHROPIC_API_KEY` — screenshot + vision assistant flow
- `ELEVENLABS_API_KEY` — STT and TTS
- `ELEVENLABS_VOICE_ID` — TTS voice

### Optional

| Variable | Default | Purpose |
|---|---|---|
| `STT_PROVIDER` | `whisper` | `elevenlabs` or `whisper` |
| `CODEX_COMMAND` | `codex.cmd` | Path or name of local Codex CLI |
| `CLAUDE_CODE_COMMAND` | `claude` | Path or name of local Claude Code CLI |
| `CODEX_WORKDIR` | `playground/` (repo root) | Working dir for Codex runs |
| `CODEX_TIMEOUT_SECONDS` | `900` | Timeout for Codex runs |
| `OPENCLAW_COMMAND` | `openclaw` | Path or name of local OpenClaw CLI |
| `OPENCLAW_SESSION_KEY` | `main` | Agent id / session key for OpenClaw |
| `OPENCLAW_TIMEOUT_SECONDS` | `120` | Timeout for OpenClaw runs |
| `WHISPER_PYTHON` | `python` | Python command for local Whisper |
| `WHISPER_MODEL` | `base` | Whisper model name |
| `WHISPER_LANGUAGE` | `de` | Speech language hint |
| `PUSH_TO_TALK_KEY` | `F8` | Global push-to-talk hotkey |

## Notes

- Secrets live in `windows/.env` next to the executable — never commit this file
- Local settings are stored in `windows/data/settings.json`
- Codex writes generated files to `playground/` unless `CODEX_WORKDIR` is set
- Run logs: `codex output/zippy-codex-*.txt`, `zippy-claude-code-*.txt`, `zippy-openclaw-*.txt`
- If the global push-to-talk key cannot be registered, the on-screen hold button still works
- If direct ElevenLabs playback fails, the app falls back to local Windows speech when possible
- The first Whisper run can take longer if the selected model still needs to download
- `Clicky.Windows.exe` is a local build artifact and should stay out of git
