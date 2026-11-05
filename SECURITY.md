# Security Policy

## Project scope

Taste Passport is a local, single-player Unity game. Player profile and progression are saved as JSON under Unity's `Application.persistentDataPath`; sound preferences use `PlayerPrefs`. The current project does not require an account, backend, online save service, or multiplayer connection. Android builds include a native text-to-speech bridge and rely on the device's installed speech service.

## Supported versions

There are no numbered or store releases yet. Security fixes are considered for the current `main` branch.

| Version | Support status |
| --- | --- |
| `main` | Supported on a best-effort basis |
| Unreleased snapshots and old commits | No separate support commitment |

## Report a vulnerability

Please report suspected security issues privately. If GitHub displays **Report a vulnerability** in this repository's **Security** tab, use that private reporting flow. If it is unavailable, contact the repository maintainer through a private GitHub channel. Do not post an unpatched vulnerability in a public issue or discussion.

Include the affected commit or Unity version, steps to reproduce, expected and actual behavior, and a concise impact description. Redact access tokens, personal information, and player save data from logs or screenshots.

The maintainer will review reports and coordinate any fix and disclosure with the reporter. This project does not promise a response time or a particular release schedule.
