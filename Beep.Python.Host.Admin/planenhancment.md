# Beep.Python Host Admin – Enhancement Plan

## Goals
- Add a first-class fine-tuning area that reuses the existing LLM/task infrastructure.
- Make user management optional so deployments can run fully local or delegate to an external IdP.
- Add optional IdentityServer authentication via OpenID Connect without breaking current session/RBAC flows.

## Principles
- Feature-flag everything; keep defaults backward-compatible.
- Reuse current services (tasks, audit logging, RBAC) instead of inventing new primitives.
- Make UI/UX obvious: only show controls that are enabled by configuration.
- Ship incrementally with migrations and API docs updated in lockstep.

## Phase 0: Baseline & Toggles
- Add config flags: `ENABLE_FINETUNE`, `ENABLE_INTERNAL_USER_MGMT`, `ENABLE_IDENTITYSERVER_AUTH`.
- Wire flags into template context and navigation rendering so disabled areas disappear cleanly.
- Add setup validation that surfaces misconfiguration (e.g., IdentityServer endpoints missing when enabled).

## Setup Wizard (DB/Admin/Auth)
- Ensure first-run always redirects to the setup wizard until config is valid; block other routes until configured.
- Wizard steps: pick database provider/URI (with live connection test), create initial admin (hash password), choose auth mode (local vs IdentityServer), and set secret key/session settings.
- Persist wizard state safely so it can be resumed; log initialization events to the audit log.
- Post-setup checklist: force admin password change on first login and prompt for HTTPS/secret key review.

## Phase 1: Fine-Tuning Module
- Data layer: migrations for `finetune_jobs`, `finetune_datasets`, `finetune_events`; link to users and models.
- Service layer: job planner that validates datasets, selects base model/adapter strategy, and emits tasks to the existing task manager.
- Storage: define dataset root under `BEEP_PYTHON_HOME/finetune` with checksum/version metadata; support local uploads first.
- Training backends: start with HuggingFace Transformers + PEFT/QLoRA preset; allow custom training image/env via config.
- Scheduling & lifecycle: queue/cancel/restart jobs, stream logs/metrics to WebSocket, and push status into audit log.
- API/UI: new `/api/v1/finetune/*` endpoints; dashboard page with dataset list, job history, live job view, and artifact downloads.
- Security: new permissions (`finetune.read`, `finetune.manage`, `finetune.datasets.write`); guard uploads and job control.
- Documentation: user guide for data prep, quotas, and GPU/CPU requirements; admin guide for storage cleanup and retention.

## Phase 2: Optional Internal User Management
- Feature flag `ENABLE_INTERNAL_USER_MGMT` controls availability of the current user/role CRUD UI and APIs.
- When disabled: hide UI, block internal CRUD endpoints, and rely on external identity mapping for roles; keep audit log readable.
- Bootstrap: CLI/admin API to create a fallback local admin when internal management is on; document rotation/reset flow.
- Data: migration to mark users as `external` and to store external subject identifiers for mapping.
- Permissions/UI: ensure role assignment, group handling, and session cleanup work whether users are local or external.

## Phase 3: IdentityServer (OIDC) Integration
- Config: `IDENTITYSERVER_AUTHORITY`, `IDENTITYSERVER_CLIENT_ID`, `IDENTITYSERVER_CLIENT_SECRET`, `IDENTITYSERVER_SCOPES`, `IDENTITYSERVER_LOGOUT_REDIRECT`.
- Auth flow: OIDC code flow with PKCE; use IdentityServer for login/logout while retaining Flask session for app state.
- Claim mapping: map `sub` to user record, roles from `roles`/`role` claim, email/display name from standard claims; fall back to guest role when unmapped.
- Session hardening: token expiry checks, refresh handling, CSRF protection on callback, and audience/issuer validation.
- UI: show external identity provider badge, disable password fields, and provide “switch account” using IdP logout.
- Docs: integration guide for IdentityServer with sample configuration and troubleshooting for common OIDC errors.

## Phase 4: Quality, Ops, and Rollout
- Tests: unit tests for feature flags and claim mapping; integration tests for fine-tune job lifecycle; UI smoke tests for enabled/disabled states.
- Telemetry: log fine-tune job durations, failure reasons, and IdentityServer login events; add minimal metrics to existing monitoring.
- Migration plan: database migrations gated by feature flags; provide rollback notes and seed data updates.
- Rollout: ship behind flags, enable in staging with IdentityServer, then enable fine-tuning in GPU-enabled environments.

## Cross-Platform Distribution & Embedded Python
- Packaging strategy: ship self-contained builds per OS; prefer one-folder layouts over one-file to reduce AV false positives and start time.
- Windows (.exe): use the existing `setup_embedded_python.bat` to stage the embeddable runtime, freeze deps with `pip install -r requirements.txt`, then build with PyInstaller (spec file that bundles templates/static/instance defaults). Add a launcher that sets `PYTHONHOME`/`PYTHONPATH` to the embedded runtime.
- macOS (.app/.dmg): build with PyInstaller `--windowed` targeting universal2 Python 3.11; create a minimal `.app` wrapper that starts `run.py`; notarize/sign optional. Bundle a `.command` script for CLI start.
- Linux (AppDir/AppImage or tarball): PyInstaller one-folder with a wrapper script; optional AppImage using appimagetool on the PyInstaller output for portability.
- Config defaults: place writable app data under `%LOCALAPPDATA%/BeepPython` (Win) or `~/.local/share/beep-python` (Unix); keep `instance/` seeded but writable.
- Automation: add `create_distribution.bat`/`.sh` to orchestrate: clean build dir, prepare embedded runtime, run PyInstaller, smoke-test `--help`, and package artifacts.
- Docs: add a distribution guide covering prerequisites (VC++ redist on Windows), build commands per OS, and how to update the embedded runtime when Python or dependencies change.
- Artifacts added: `beep_python_host.spec` (data paths baked in) and launchers `launch_beep_python_admin.cmd` / `launch_beep_python_admin.sh` that set defaults and start the bundled binary.

## Deliverables
- Code: flagged features, migrations, new services/endpoints, and UI surfaces.
- Docs: updated README/config samples, new fine-tuning guide, IdentityServer setup guide, and admin runbooks.
- Operations: storage paths, retention defaults, health checks for OIDC, and cleanup scripts for artifacts/logs.
