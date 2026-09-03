# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is a cross-platform CI/CD deployment and release-management tool (`deployctl`) targeting both Windows and Linux. The full engineering specification is in `skill.md`. The tool is **not** a generic CI runner — it is an application deployment system with safety-first design: backup-before-deploy, immutable releases, atomic rollback, and complete audit trails.

## Commands

```bash
# Build the full solution
dotnet build

# Run all tests
dotnet test

# Run a single test class
dotnet test --filter "FullyQualifiedName~DeploymentServiceTests"

# Run the CLI (development)
dotnet run --project src/Deployment.CLI -- <command>

# Create a migration after schema changes
dotnet ef migrations add <Name> --project src/Deployment.Infrastructure --startup-project src/Deployment.CLI

# Data directory (default: %APPDATA%/deployctl or ~/.config/deployctl on Linux)
# Override with: $env:DEPLOYCTL_DATA = "C:\my\data"
#                $env:DEPLOYCTL_BACKUPS = "C:\my\backups"
#                $env:DEPLOYCTL_RELEASES = "C:\my\releases"
```

## Architecture

Use a layered architecture with these project modules:

```
Deployment.Domain          — Entities: Release, Deployment, Backup, Target, RetentionPolicy
Deployment.Application     — Services: ReleaseService, DeploymentService, BackupService,
                             RollbackService, DiffService, RetentionService,
                             VerificationService, AuditService, LockService, CredentialService
Deployment.Infrastructure  — Platform adapters, file system, SSH/SFTP, DB, credential store
Deployment.CLI             — deployctl command-line interface
Deployment.API             — REST API layer
Deployment.Web             — Web UI (Phase 2)
Deployment.Tests           — All tests
```

OS-specific logic must live behind interfaces, never in the core engine:

```
IDeploymentTarget → WindowsDeploymentTarget | LinuxDeploymentTarget
IFileSystem, IProcessExecutor, IServiceManager, IFileTransfer,
IPermissionManager, IBackupProvider, ICredentialProvider
```

## Deployment State Machine

States must transition in order. Reject invalid transitions.

```
CREATED → VALIDATING → DIFF_READY → WAITING_APPROVAL →
BACKING_UP → BACKUP_VERIFIED → DEPLOYING → VERIFYING →
COMPLETED | FAILED → ROLLING_BACK → ROLLED_BACK
```

## Non-Negotiable Safety Rules

1. **No verified backup = no production deployment.** If backup creation or verification fails, stop immediately — do not deploy.
2. **Failed deployment = attempt rollback.** Never leave the target in an indeterminate state without trying to restore.
3. **Protected backup = never automatically delete.**
4. **Release created = release immutable.** New changes require a new version.
5. **Every deployment operation = audit record required.**
6. **Production deployment = explicit operator confirmation required.**
7. **Concurrent deployment to same Application+Environment+Target = blocked** (deployment lock).
8. **Checksum mismatch = deployment failure + start rollback.**
9. **Unknown recovery state = do not continue automatically** — surface options (Resume / Rollback / Mark Failed / Inspect).

## Deployment Sequence

```
Prepare → Validate → Calculate Diff → User Confirmation →
Create Backup → Verify Backup → Deploy → Verify Deployment →
Create Release Record → Apply Retention Policy
```

Rollback also creates a backup of the current state before restoring.

## Key Design Constraints

- **Atomicity:** Prefer release-directory switching (`current → releases/v1.4.2`) over destructive in-place file replacement.
- **Checksums:** SHA-256 on every file in every release manifest. Compare source vs. target after deployment.
- **Streaming:** Diff and checksum operations must stream; never load large files into memory.
- **Credentials:** Use `ICredentialProvider` abstraction. Secrets must never appear in logs, audit records, CLI output, or diff output.
- **State is explicit:** Deployment state is recorded metadata, never inferred by scanning files.
- **Metadata is authoritative:** Never parse state from filenames (e.g., `backup_v1.4.2.zip`). Use IDs as references.
- **Deployment history is independent of backup retention:** Deleting old backups must not delete deployment history.
- **Hard-coded paths and credentials are prohibited.**

## CLI Commands

```bash
deployctl release list
deployctl release show <version>
deployctl diff <version> <environment>
deployctl deploy <version> <environment> [--dry-run] [--yes]
deployctl rollback <version> <environment>
deployctl backup create <environment>
deployctl backup list <environment>
deployctl backup cleanup <environment>
deployctl history <environment>
```

Production-modifying commands must prompt `Continue? [y/N]` unless `--yes` is passed. Production policy may prohibit bypassing confirmation.

## Retention Policy

Retention cleanup must check all of the following before deleting a backup:
1. Is it protected?
2. Is it referenced by a rollback candidate?
3. Is it within the retention period?
4. Does removing it violate the minimum-count guarantee?

If uncertain → **do not delete**.

## Implementation Phases

**Phase 1 (MVP — build this first):** Local deployment, backup, restore, diff, release management, retention policy, CLI, audit log. Validate on both Windows and Linux before moving on.

**Phase 2:** Web UI, RBAC, remote deployment (SSH/SFTP), Windows Service and systemd support.

**Phase 3:** Approval workflow, multi-target parallel deployment, notifications, metrics, external secret managers.

## Definition of Done

A feature is complete only when it has: implementation + unit tests + integration tests + error handling + logging + audit trail + Windows validation + Linux validation. Happy-path demo alone is not sufficient.

## Failure Handling Rules

| Failure | Action |
|---|---|
| Backup fails | STOP — do not deploy |
| Diff fails | STOP — do not deploy |
| File transfer fails | STOP — start rollback |
| Checksum mismatch | Deployment FAILED — start rollback |
| Service restart fails | Deployment FAILED — start rollback |
| Process crash mid-deploy | On next startup, detect incomplete deployment and surface recovery options |
