# Notice

## Origin & Attribution

Zippy For Windows is largely a rewrite in C# / WinForms. The core concept borrowed from [farzaa/clicky](https://github.com/farzaa/clicky) (macOS/Swift, by Farza Majeed, MIT) is the idea of an **always-on assistant that lives right next to the mouse cursor**. The repo originally started from a local clone of that project, so small remnants (folder names, minor snippets) may still trace back to it; the upstream MIT license therefore continues to apply to anything still derived from it.

Everything else — architecture, language, platform, and feature set — is original work:

- a native WinForms desktop app (`Clicky.Windows.cs`) instead of a macOS/Swift menu-bar app
- direct Anthropic and ElevenLabs integrations from the client (no Cloudflare Worker proxy)
- ElevenLabs or local Whisper for speech-to-text (no AssemblyAI pipeline)
- orchestration of three local CLI agents — Codex, Claude Code, and OpenClaw — including screenshot handoffs (`nimm codex mit screen ...`); the upstream project has no agent orchestration
- tray icon, global push-to-talk hotkey, and German speech trigger phrases
- a local `.env` + `data/settings.json` configuration model

## Third-Party Code & Assets

The original clicky project is licensed under MIT. Its copyright notice is retained in [`LICENSE`](LICENSE) alongside the current project's copyright. Where small remnants from the original project remain (e.g. folder names or minor snippets), their original MIT attribution and license terms continue to apply.

## License

This repository is distributed under the MIT License. See [`LICENSE`](LICENSE).
