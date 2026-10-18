# Coding-agent memory

Adopted at the user's request on 2026-10-18: [akitaonrails/ai-memory](https://github.com/akitaonrails/ai-memory), native Windows release **3.1.1**. This is development tooling, classified as **Others / Support**.

## Installation and connection

The official Windows installer verified the release's published SHA-256 checksum. The executable is at `C:\Users\Elian\AppData\Local\Programs\ai-memory\ai-memory.exe` and its directory is on the user's PATH. `ai-memory setup --yes --agent codex` initialized local storage, registered the hidden per-user logon service, and installed Codex lifecycle hooks and six globally managed `ai-memory-*` skills.

The global Codex MCP entry uses the package's native `mcp-bridge --no-session-id`, with explicit `--data-dir C:\Users\Elian\AppData\Local\ai-memory` and `--server-url http://127.0.0.1:49374`. The bridge reads the private token file itself. The token is neither copied into this repository nor required in an environment variable. The explicit data directory avoids ambiguity between native and packaged desktop process paths.

The server binds to loopback and has no configured LLM or embedding provider. Capture, local history import, full-text retrieval and handoffs work without an external model API. The package stores its wiki, index, observations and credentials outside the project; they are not game assets or committed repository content.

## Project use

`.ai-memory.toml` pins `workspace = "default"` and `project = "elianmelo-project-mecha"`, matching the package's repository-derived identity. Codex uses a static MCP connection, so pass both values on each project-scoped call. This prevents concurrent chats or worktrees from selecting another project's memory.

The package's managed block in `AGENTS.md` routes relevant work to its installed skills. Retrieve prior decisions and known pitfalls when they affect the task. Treat recalled content as historical evidence, check it against the current checkout, and follow current user and repository instructions. `AGENTS.md` and maintained project documents remain the canonical architecture rules; routine activity is captured through hooks rather than duplicated manually as permanent notes.

The initial bounded local history import selected 25 recent project sessions: **20 sessions imported**, producing **20 searchable session pages** and **29,813 imported events**. Five sessions were skipped because a native transcript record exceeded the package's 1 MiB record limit. Their original histories are untouched. No forced retry or transcript rewriting was performed.

Future capture requires **user approval of the installed hooks in Codex**: restart Codex, open `/hooks`, review the ai-memory hooks, and trust them. The installer and doctor explicitly report that untrusted hooks are skipped; installation does not grant that trust.

## Validation and maintenance

Verified the native executable version, service health, authenticated MCP initialization, tool discovery and the installed Codex MCP configuration. A scoped MCP briefing reported the 20 imported sessions/pages; a scoped `memory_query` for camera history returned existing project session evidence. The global skills and project routing are installed. No game files were modified and no automated tests were created, modified or run. No Unity label refresh is required because the additions are repository configuration/documentation outside `Assets`.

Useful commands from the project directory:

```powershell
& "$env:LOCALAPPDATA/Programs/ai-memory/ai-memory.exe" status
& "$env:LOCALAPPDATA/Programs/ai-memory/ai-memory.exe" doctor
& "$env:LOCALAPPDATA/Programs/ai-memory/ai-memory.exe" search "camera"
```

Use `ai-memory upgrade` for package updates. After an update, refresh managed instructions/skills with `ai-memory install-instructions --target AGENTS.md --compact --skills-scope global --skills-agent agents`. Review any changed hook commands in Codex when prompted. Do not bypass hook trust.
