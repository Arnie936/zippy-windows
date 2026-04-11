# Zippy For Windows

This is the native Windows client for Zippy. It calls Anthropic and ElevenLabs directly, supports ElevenLabs speech-to-text or local Whisper for speech input, and can hand one-shot tasks to a local Codex CLI run.

## Requirements

- Windows
- an Anthropic API key
- an ElevenLabs API key and voice ID
- optional: local Whisper if you want `STT_PROVIDER=whisper`
- optional: local Codex CLI if you want the `nimm codex ...` handoff flow

## What works

- system tray app
- global push-to-talk hotkey
- always-on mouse companion that follows the cursor
- companion can drive to a detected on-screen target window or control when Claude returns a `[POINT:...]` tag
- capture all screens and send them directly to Claude
- speech input with microphone recording + ElevenLabs speech-to-text or local Whisper
- show the answer in-app
- play TTS directly through ElevenLabs when enabled
- route prompts containing `nimm codex` to a local Codex one-shot run
- attach screenshots to Codex for prompts like `nimm codex mit screen ...`
- route prompts containing `nimm claude code` to a local Claude Code one-shot run
- write Codex-generated files into `playground/` by default
- write Codex run logs to `codex output/`
- keep a short conversation history in memory

## What does not exist yet

- richer companion art and higher-end animation polish
- onboarding video and polished desktop overlay behavior from the macOS app

## Build It

Double-click:

`Build-Clicky.cmd`

Or from `cmd.exe`:

```cmd
C:\Users\Arnold\Desktop\clip advanced\clicky\windows\Build-Clicky.cmd
```

This compiles:

- `Clicky.Windows.cs`

into:

- `Clicky.Windows.exe`

## Start It

Double-click:

`Start-Clicky.cmd`

Or run:

- `Clicky.Windows.exe`

## First Run

1. Copy `.env.example` to `.env` in the same folder.
2. Fill in:
   - `ANTHROPIC_API_KEY`
   - `ELEVENLABS_API_KEY`
   - `ELEVENLABS_VOICE_ID`
   - optional: `STT_PROVIDER` (`elevenlabs` or `whisper`)
   - optional: `CODEX_COMMAND`
   - optional: `CLAUDE_CODE_COMMAND`
   - optional: `CODEX_WORKDIR`
   - optional: `CODEX_TIMEOUT_SECONDS`
   - optional: `WHISPER_PYTHON`
   - optional: `WHISPER_MODEL`
   - optional: `WHISPER_LANGUAGE`
   - optional: `PUSH_TO_TALK_KEY`
3. Start the app.
4. Click `reload .env`.
5. Click `test apis`.
6. Either type a prompt like `where is the button i should click?` and click `ask about my screen`, or hold `hold to talk`.
7. For speech mode: hold the button while speaking, then release to transcribe.
8. Or use the global key from `.env`, default `F8`: hold to speak, release to transcribe.
9. To hand off a one-shot local Codex task, start the prompt with `nimm codex ...`.
10. To hand off a Codex task with screenshots attached, use a phrase like `nimm codex mit screen ...`.
11. To hand off a one-shot local Claude Code task, use a phrase like `nimm claude code ...`.

Without Codex installed, Zippy still works for normal assistant tasks.
Without local Whisper installed, Zippy still works if `STT_PROVIDER=elevenlabs`.

## Required Keys

- `ANTHROPIC_API_KEY`
- `ELEVENLABS_API_KEY`
- `ELEVENLABS_VOICE_ID`

## Optional Whisper Settings

- `STT_PROVIDER`
Default: `whisper`
- `CODEX_COMMAND`
Default: `codex.cmd`
- `CLAUDE_CODE_COMMAND`
Default: `claude`
- `CODEX_WORKDIR`
Default: `playground/` in the repo root above `windows/`
- `CODEX_TIMEOUT_SECONDS`
Default: `900`
- `WHISPER_PYTHON`
Default: `python`
- `WHISPER_MODEL`
Default: `base`
- `WHISPER_LANGUAGE`
Default: `de`
- `PUSH_TO_TALK_KEY`
Default: `F8`

## Notes

- Settings are stored in `windows/data/settings.json`
- Secrets live in `windows/.env` next to the executable
- Codex uses `playground/` as its default working directory unless `CODEX_WORKDIR` is set
- If the global push-to-talk key cannot be registered, Zippy still works with the on-screen hold button
- If direct ElevenLabs playback fails, the app falls back to local Windows speech when possible
- The first Whisper run can take longer if the selected model still needs to be downloaded locally
- Codex runs are saved to `codex output/zippy-codex-YYYYMMDD-HHMMSS.txt`
- The app is built with the classic .NET Framework compiler already available on this machine
- `Clicky.Windows.exe` is a local build artifact and should stay out of git

## Known Limitations

- Windows-only
- no installer yet
- Codex runs are one-shot background jobs
- speech and vision features depend on external API availability
