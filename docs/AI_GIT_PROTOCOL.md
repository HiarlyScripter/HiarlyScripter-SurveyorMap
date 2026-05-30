# AI Git Protocol

This repository uses local Git checkpoints for controlled handoff between AI agents.

## Meaning

- Commit = checkpoint.
- Tag = stable milestone.
- Branch `claude-work` = Claude.
- Branch `codex-exec` = Codex Executor.

## Before Editing

- The working tree must be clean, or there must be a checkpoint commit first.
- Run `tools\ai_start_work.ps1`.
- Codex must not assume work without reading `docs\HANDOFF_CURRENT.md` and `docs\NEXT_ACTIONS.md`.

## After Editing

- Validate the work, or confirm that the required validation report already exists.
- Update or confirm `docs\HANDOFF_CURRENT.md`.
- Run `tools\ai_finish_work.ps1 -Message "<checkpoint message>"`.
- Do not push.

## Handoff Rules

- Claude must not continue after Codex executes without reading `docs\HANDOFF_BACK_TO_CLAUDE.md`.
- Codex must not assume work without reading `docs\HANDOFF_CURRENT.md` and `docs\NEXT_ACTIONS.md`.

