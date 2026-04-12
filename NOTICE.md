# Notice

## Origin & Attribution

Zippy For Windows began as a clone of [farzaa/clicky](https://github.com/farzaa/clicky), a macOS/Swift menu-bar AI assistant by Farza Majeed, licensed under MIT.

The original project provided the initial inspiration and concept:

- an always-on cursor/menu-bar companion
- push-to-talk voice capture
- screenshot-aware chat with Claude
- ElevenLabs text-to-speech playback

This repository has since been substantially reshaped into a native Windows desktop assistant written from scratch in C# / WinForms. It replaces the macOS/Swift codebase, the Cloudflare Worker proxy architecture, and the AssemblyAI transcription pipeline with:

- a native WinForms desktop app (`Clicky.Windows.cs`)
- direct Anthropic and ElevenLabs integrations from the client
- ElevenLabs or local Whisper for speech-to-text
- local Codex, Claude Code, and OpenClaw one-shot handoffs
- a local `.env` + `data/settings.json` configuration model

## Third-Party Code & Assets

The original clicky project is licensed under MIT. Its copyright notice is retained in [`LICENSE`](LICENSE) alongside the current project's copyright. If any third-party code, assets, or notices from the original project are still present in this repository, their original attribution and license terms continue to apply.

## License

This repository is distributed under the MIT License. See [`LICENSE`](LICENSE).
