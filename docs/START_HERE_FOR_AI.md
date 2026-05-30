# Start Here for AI Agents

Every agent must read this file before acting in this repository.

## Roles

- Claude works normally on branch `claude-work`.
- Codex Executor works on branch `codex-exec` only when Claude is limited or blocked.
- Codex Auditor is read-only and does not edit files.

## Required Flow

- Before any change, run `tools\ai_start_work.ps1`.
- At the end of any work round, run `tools\ai_finish_work.ps1`.
- Never run two executor agents at the same time.
- Do not publish, do not push, and do not touch other mods.

## Current Next Action

Current next action: Phase 2 CenterOnPlayer, only after a fresh `last-full-validation` Overall PASS.

