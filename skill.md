# Deployment & Release Management CI/CD Tool — Engineering Skill

## 1. Project Overview

Build a cross-platform CI/CD deployment and release-management tool that can be deployed and executed on both:

- Windows
- Linux

The system is intended for controlled application deployment rather than being only a generic CI/CD runner.

The primary goals are:

1. Safely deploy application files to target servers.
2. Automatically create a backup before every deployment.
3. Compare source and target files before deployment.
4. Provide a visual file-difference experience similar to WinMerge.
5. Support rollback to a previous deployment version.
6. Maintain a Git-like release/version history for deployments.
7. Allow operators to define backup retention policies.
8. Prevent accidental deployment, overwrite, or rollback.
9. Provide a complete audit trail of deployment operations.
10. Support both Windows and Linux target environments.

---

# 2. Core Design Philosophy

The system MUST follow these principles:

### 2.1 Backup Before Deployment

A deployment MUST NOT modify the target environment until the pre-deployment backup has completed successfully.

The normal deployment sequence is:

```text
Prepare
  ↓
Validate
  ↓
Calculate Diff
  ↓
User Confirmation
  ↓
Create Backup
  ↓
Verify Backup
  ↓
Deploy
  ↓
Verify Deployment
  ↓
Create Release Record
  ↓
Apply Retention Policy
```

If backup creation fails:

```text
STOP DEPLOYMENT
```

Never continue deployment when the required backup cannot be verified.

---

# 3. Major Features

## 3.1 Deployment

The system SHALL support:

- Local deployment
- Remote deployment
- Windows target
- Linux target
- Multiple environments
- Multiple applications
- Multiple deployment targets

Example environments:

```text
Development
Testing
Staging
Production
```

Example target configuration:

```text
Application: ERP
Environment: Production
OS: Windows
Host: server01
Deployment Path: C:\ERP
```

or:

```text
Application: ERP
Environment: Production
OS: Linux
Host: server01
Deployment Path: /opt/erp
```

---

# 4. Deployment Package

A deployment MUST be represented by an immutable deployment package.

Example:

```text
release/
├── manifest.json
├── files/
├── checksums/
└── metadata/
```

The manifest SHOULD contain:

```json
{
  "application": "ERP",
  "version": "1.4.2",
  "buildId": "20260903.1420",
  "commitId": "abc123",
  "createdAt": "2026-09-03T14:20:00+08:00",
  "createdBy": "operator",
  "files": []
}
```

Every deployment package MUST have a unique release identifier.

Example:

```text
ERP-1.4.2
ERP-1.4.3
ERP-1.5.0
```

---

# 5. Git-like Release Management

The system SHOULD provide Git-like release concepts without requiring the deployment engine itself to be Git.

Conceptually:

```text
Repository
    │
    ├── Release 1.0.0
    ├── Release 1.1.0
    ├── Release 1.1.1
    ├── Release 1.2.0
    │
    └── Current Production
```

Each release MUST contain:

```text
Release ID
Version
Source Commit
Build ID
Created Time
Created By
Files
Checksum
Deployment History
Rollback Information
Change Summary
```

Example:

```text
Production
│
├── v1.0.0
├── v1.1.0
├── v1.1.1
└── v1.2.0 ← CURRENT
```

The operator SHALL be able to:

- View release history
- Compare releases
- Deploy a release
- Roll back to a previous release
- View deployment logs
- View file differences
- View who deployed the release
- View deployment time
- View deployment result

---

# 6. Deployment Version State

Each target SHALL maintain a deployment state.

Example:

```json
{
  "application": "ERP",
  "environment": "Production",
  "target": "server01",
  "currentRelease": "1.4.2",
  "previousRelease": "1.4.1",
  "deploymentId": "DEP-20260903-001",
  "lastDeploymentAt": "2026-09-03T14:30:00+08:00"
}
```

The system MUST NOT determine the current version solely by scanning files.

The deployment state MUST be explicitly recorded.

---

# 7. Backup System

Before deployment, the system MUST create a backup of the target state.

Example:

```text
backups/
└── ERP/
    └── Production/
        └── server01/
            ├── 20260903-143000-v1.4.1/
            ├── 20260902-180000-v1.4.0/
            └── 20260901-090000-v1.3.9/
```

Each backup MUST contain metadata.

Example:

```json
{
  "backupId": "BKP-20260903-143000",
  "application": "ERP",
  "environment": "Production",
  "target": "server01",
  "sourceRelease": "1.4.1",
  "createdAt": "2026-09-03T14:30:00+08:00",
  "createdBy": "operator",
  "fileCount": 1250,
  "checksum": "..."
}
```

---

# 8. Backup Verification

Backup creation MUST be followed by verification.

At minimum:

1. Verify backup directory exists.
2. Verify expected files exist.
3. Verify file count.
4. Verify checksums where applicable.
5. Verify backup metadata.
6. Verify backup is readable.

Only after successful verification may deployment continue.

---

# 9. Backup Retention Policy

Operators MUST be able to configure backup retention.

Supported policies SHOULD include:

### Retain by days

```text
Keep backups for:
7 days
14 days
30 days
60 days
90 days
180 days
365 days
```

### Retain by number

```text
Keep latest 5 backups
Keep latest 10 backups
Keep latest 20 backups
```

### Combined policy

Example:

```text
Keep backups for 30 days
AND
always keep the latest 5 backups
```

The retention engine MUST protect backups that are still required for rollback.

For example:

```text
Current Release: 1.5.0

Rollback candidates:
1.4.9
1.4.8
1.4.7
```

The system MUST NOT delete a backup that is explicitly marked as protected.

---

# 10. Backup Protection

An operator SHALL be able to mark a backup as:

```text
Protected
```

Protected backups MUST NOT be automatically deleted by retention cleanup.

Example:

```text
BKP-001  1.4.0  30 days   NORMAL
BKP-002  1.4.1  30 days   NORMAL
BKP-003  1.4.2  365 days  PROTECTED
```

---

# 11. Diff Engine

The system MUST provide file and directory comparison before deployment.

The functionality should resemble WinMerge.

Comparison MUST support:

```text
Source
vs
Target
```

Example:

```text
Source Release: v1.4.2
Target Release: v1.4.1
```

The result MUST identify:

```text
Added
Modified
Deleted
Unchanged
Renamed (if detectable)
```

Example:

```text
+ config/new.json
M bin/ERP.exe
M bin/ERP.dll
- old/test.dll
= README.md
```

---

# 12. File Difference Details

For text files, provide line-level differences.

Example:

```text
OLD:
ConnectionTimeout=30

NEW:
ConnectionTimeout=60
```

For binary files:

```text
Binary files differ
Size:
OLD: 1.2 MB
NEW: 1.4 MB

SHA256:
OLD: xxxx
NEW: yyyy
```

The system MUST NOT attempt to display binary files as text unless explicitly supported.

---

# 13. Diff Approval

Before production deployment, the operator SHOULD be shown:

```text
Release: v1.4.2
Target: Production
Files Added: 12
Files Modified: 38
Files Deleted: 4
Files Unchanged: 1250
```

The operator MUST explicitly confirm deployment.

Example:

```text
[View Diff]

[Confirm Deployment]

[Cancel]
```

For production environments, deployment confirmation SHOULD be mandatory.

---

# 14. Deployment Transaction Model

Deployment SHOULD behave as a transaction.

Conceptually:

```text
START
  ↓
Create Backup
  ↓
Verify Backup
  ↓
Stage New Files
  ↓
Validate
  ↓
Commit Deployment
  ↓
Verify
  ↓
SUCCESS
```

If an error occurs:

```text
Deployment Error
      ↓
Restore Previous State
      ↓
Verify Rollback
      ↓
FAILED + ROLLED BACK
```

The system SHOULD avoid directly overwriting production files whenever possible.

Preferred approach:

```text
Target
├── releases/
│   ├── 1.4.1/
│   └── 1.4.2/
│
└── current → releases/1.4.2
```

This release-directory strategy SHOULD be preferred over destructive in-place replacement when the target application supports it.

---

# 15. Rollback

Rollback MUST be a first-class operation.

The operator SHALL be able to select:

```text
Rollback to:
v1.4.1
v1.4.0
v1.3.9
```

Rollback sequence:

```text
Select Release
      ↓
Show Diff
      ↓
Confirm Rollback
      ↓
Create Current-State Backup
      ↓
Restore Selected Release
      ↓
Verify
      ↓
Record Rollback
```

A rollback MUST also create a backup of the current state before changing the target.

Therefore:

```text
Deploy:
Backup → Deploy

Rollback:
Backup → Restore
```

---

# 16. Rollback Safety

Rollback MUST NOT silently delete the current state.

Before rollback:

```text
Current:
v1.4.2

Rollback:
v1.4.1
```

The system creates:

```text
Backup:
v1.4.2
```

Then restores:

```text
v1.4.1
```

This allows the operator to recover from a failed rollback.

---

# 17. Deployment Lock

Only one deployment operation SHOULD be allowed for the same target at a time.

Example:

```text
Production / server01 / ERP
```

If deployment is already running:

```text
Deployment Locked

DEP-20260903-001
Operator: admin
Status: DEPLOYING
```

Another operator MUST NOT be allowed to start a conflicting deployment.

---

# 18. Idempotency

Deployment operations SHOULD be idempotent.

Deploying the same release twice SHOULD NOT create an inconsistent target state.

Example:

```text
Current = v1.4.2
Requested = v1.4.2
```

The system should report:

```text
Already deployed
```

rather than blindly replacing all files.

---

# 19. Checksum Verification

The system MUST support checksums.

Recommended:

```text
SHA-256
```

Every deployment package SHOULD contain checksums.

Example:

```text
ERP.exe    SHA256: ...
ERP.dll    SHA256: ...
config.json SHA256: ...
```

After deployment:

```text
Source checksum
        vs
Target checksum
```

must be compared.

---

# 20. Windows Support

Windows deployment MUST support:

- Windows Server
- Windows application directories
- Windows services
- IIS applications where applicable
- PowerShell
- UNC paths where applicable
- NTFS permissions
- Windows service stop/start
- File locking detection

Windows paths must correctly support:

```text
C:\Application
D:\Deploy\ERP
\\server\share\application
```

The system MUST NOT hard-code `/` as the only path separator.

---

# 21. Linux Support

Linux deployment MUST support:

- Linux servers
- SSH
- SCP/SFTP where applicable
- POSIX file permissions
- Owner/group
- Symbolic links
- systemd services
- Shell scripts

Examples:

```text
/opt/erp
/var/www/erp
/home/deploy/releases
```

Deployment MUST preserve permissions where required.

Example:

```text
chmod
chown
```

should be handled through a platform abstraction rather than Windows-specific logic.

---

# 22. Platform Abstraction

The core deployment engine MUST NOT contain excessive OS-specific logic.

Use an abstraction such as:

```text
IDeploymentTarget
    ├── WindowsDeploymentTarget
    └── LinuxDeploymentTarget
```

Possible interfaces:

```text
IFileSystem
IProcessExecutor
IServiceManager
IFileTransfer
IPermissionManager
IBackupProvider
```

Example:

```text
DeploymentEngine
       │
       ├── WindowsTarget
       │      ├── PowerShell
       │      └── Windows Service
       │
       └── LinuxTarget
              ├── SSH
              └── systemd
```

---

# 23. Deployment State Machine

Deployment states SHOULD be explicit.

Recommended states:

```text
CREATED
VALIDATING
DIFF_READY
WAITING_APPROVAL
BACKING_UP
BACKUP_VERIFIED
DEPLOYING
VERIFYING
COMPLETED
FAILED
ROLLING_BACK
ROLLED_BACK
CANCELLED
```

Invalid state transitions MUST be rejected.

Example:

```text
CREATED
   ↓
VALIDATING
   ↓
DIFF_READY
   ↓
WAITING_APPROVAL
   ↓
BACKING_UP
   ↓
BACKUP_VERIFIED
   ↓
DEPLOYING
   ↓
VERIFYING
   ↓
COMPLETED
```

---

# 24. Audit Log

Every important operation MUST be recorded.

Audit events include:

```text
Release Created
Deployment Started
Diff Generated
Deployment Approved
Backup Created
Backup Verified
Deployment Completed
Deployment Failed
Rollback Started
Rollback Completed
Backup Deleted
Backup Protected
Configuration Changed
```

Example:

```json
{
  "event": "DEPLOYMENT_COMPLETED",
  "deploymentId": "DEP-20260903-001",
  "release": "1.4.2",
  "operator": "admin",
  "target": "server01",
  "timestamp": "...",
  "result": "SUCCESS"
}
```

Audit logs MUST NOT be silently overwritten.

---

# 25. Permission / RBAC

The system SHOULD support role-based access control.

Recommended roles:

```text
Viewer
Operator
ReleaseManager
Administrator
```

Example permissions:

### Viewer

```text
View releases
View deployment history
View diff
View logs
```

### Operator

```text
Viewer permissions
Deploy
Rollback
Create backup
```

### ReleaseManager

```text
Operator permissions
Create release
Approve production deployment
Manage retention
```

### Administrator

```text
Full system configuration
Manage users
Manage targets
Manage security settings
```

---

# 26. Production Protection

Production environments MUST support additional safeguards.

Recommended:

```text
Development
    ↓
Testing
    ↓
Staging
    ↓
Production
```

Production deployment SHOULD require:

1. Valid release
2. Successful validation
3. Diff generated
4. Backup completed
5. Backup verified
6. Explicit operator confirmation
7. Audit record

Optional:

```text
Two-person approval
```

---

# 27. Configuration

Configuration SHOULD be stored separately from application code.

Example:

```yaml
application: ERP

environments:
  production:
    targets:
      - name: server01
        os: windows
        path: C:\ERP

backup:
  enabled: true
  retention_days: 30
  minimum_count: 5
  compression: true

deployment:
  require_backup: true
  require_diff_confirmation: true
  production_approval: true
```

Linux example:

```yaml
targets:
  - name: linux01
    os: linux
    path: /opt/erp
```

---

# 28. CLI

The system SHOULD provide a CLI.

Example:

```bash
deployctl release list
```

```bash
deployctl release show 1.4.2
```

```bash
deployctl diff 1.4.2 production
```

```bash
deployctl backup create production
```

```bash
deployctl deploy 1.4.2 production
```

```bash
deployctl rollback 1.4.1 production
```

```bash
deployctl backup list production
```

```bash
deployctl backup cleanup production
```

```bash
deployctl history production
```

---

# 29. Suggested Web UI

A Web UI SHOULD provide:

## Dashboard

```text
Applications
Environments
Current Releases
Deployment Status
Recent Deployments
Backup Status
```

## Release Page

```text
Release
├── Version
├── Commit
├── Build
├── Created By
├── Files
├── Diff
└── Deployment History
```

## Deployment Page

```text
Release
Target
Diff Summary
Backup
Approval
Progress
Result
Logs
```

## Backup Page

```text
Backup ID
Release
Created
Size
Retention
Protected
Status
```

---

# 30. Diff UI

The Diff UI SHOULD provide a user experience similar to WinMerge.

Recommended layout:

```text
┌──────────────────────────────────────────────┐
│ Source: v1.4.2     Target: v1.4.1           │
├───────────────────┬──────────────────────────┤
│ Source            │ Target                   │
├───────────────────┼──────────────────────────┤
│ Connection=60     │ Connection=30            │
│                   │                          │
└───────────────────┴──────────────────────────┘
```

Directory tree:

```text
[+] Added
[~] Modified
[-] Deleted
[=] Unchanged
```

The operator MUST be able to select a changed file and inspect the difference.

---

# 31. Deployment Manifest

Every release MUST have a manifest.

Example:

```json
{
  "releaseId": "ERP-1.4.2",
  "version": "1.4.2",
  "commitId": "abc123",
  "buildId": "BUILD-1002",
  "files": [
    {
      "path": "bin/ERP.exe",
      "size": 123456,
      "sha256": "..."
    },
    {
      "path": "bin/ERP.dll",
      "size": 45678,
      "sha256": "..."
    }
  ]
}
```

The manifest provides deterministic deployment verification.

---

# 32. Deployment Journal

Every deployment SHOULD maintain a journal.

Example:

```text
DEP-20260903-001

1. Validation       SUCCESS
2. Diff             SUCCESS
3. Approval         SUCCESS
4. Backup           SUCCESS
5. Backup Verify    SUCCESS
6. Deployment       SUCCESS
7. Verification     SUCCESS
```

If failure occurs:

```text
1. Validation       SUCCESS
2. Diff             SUCCESS
3. Approval         SUCCESS
4. Backup           SUCCESS
5. Backup Verify    SUCCESS
6. Deployment       FAILED
7. Rollback         SUCCESS
```

This journal is essential for troubleshooting and auditability.

---

# 33. Failure Handling

The system MUST fail safely.

Examples:

### Backup failure

```text
STOP
DO NOT DEPLOY
```

### Diff failure

```text
STOP
DO NOT DEPLOY
```

### File transfer failure

```text
STOP
START ROLLBACK
```

### Checksum mismatch

```text
DEPLOYMENT FAILED
START ROLLBACK
```

### Service restart failure

```text
DEPLOYMENT FAILED
START ROLLBACK
```

---

# 34. No Silent Destructive Operations

The system MUST NOT silently:

- Delete production files
- Delete backups
- Overwrite protected backups
- Remove release history
- Roll back without recording it
- Deploy without backup when backup is required
- Skip verification

Destructive operations MUST be logged.

---

# 35. Security

Credentials MUST NOT be stored in plain text.

Do NOT store:

```text
password
private key
API token
SSH private key
```

directly inside ordinary configuration files.

Use a secure credential abstraction:

```text
ICredentialProvider
```

Possible implementations:

```text
Windows Credential Manager
Linux Secret Service
Environment variables
Encrypted local secret store
External secret manager
```

Secrets MUST NOT appear in:

```text
CLI output
deployment logs
audit logs
error messages
diff output
```

---

# 36. Concurrency

The system MUST protect against concurrent deployments.

Lock scope SHOULD be:

```text
Application + Environment + Target
```

Example:

```text
ERP + Production + server01
```

Multiple independent targets MAY deploy concurrently.

Example:

```text
ERP Production server01
ERP Production server02
```

may run concurrently if explicitly supported.

---

# 37. Atomicity Strategy

Where possible, deployment should use an atomic release switch.

Preferred:

```text
/releases/v1.4.1
/releases/v1.4.2

current → v1.4.2
```

Instead of:

```text
copy file
overwrite file
delete old file
copy next file
...
```

This minimizes partially deployed states.

For applications that cannot use directory switching, use:

```text
Backup
Stage
Stop Service
Deploy
Verify
Start Service
Verify
Commit
```

---

# 38. Logging

Use structured logging.

Recommended levels:

```text
TRACE
DEBUG
INFO
WARN
ERROR
FATAL
```

Every deployment log should include:

```text
deploymentId
application
environment
target
release
operator
timestamp
operation
result
```

Example:

```text
[INFO]
deploymentId=DEP-20260903-001
target=server01
release=1.4.2
operation=BACKUP
result=SUCCESS
```

---

# 39. Testing Requirements

The project MUST include automated tests.

Minimum test categories:

```text
Unit Tests
Integration Tests
Cross-platform Tests
Deployment Tests
Rollback Tests
Backup Tests
Diff Tests
Retention Tests
Security Tests
Failure Recovery Tests
```

Critical scenarios:

### Test 1

```text
Backup succeeds
Deployment succeeds
```

Expected:

```text
SUCCESS
```

### Test 2

```text
Backup fails
```

Expected:

```text
Deployment MUST NOT START
```

### Test 3

```text
Deployment partially fails
```

Expected:

```text
Rollback automatically starts
```

### Test 4

```text
Checksum mismatch
```

Expected:

```text
Deployment FAILED
Rollback STARTED
```

### Test 5

```text
Retention = 30 days
```

Expected:

```text
Backups older than retention period are deleted
```

except protected backups.

### Test 6

```text
Same release deployed twice
```

Expected:

```text
No inconsistent state
```

---

# 40. Cross-Platform Testing

The CI pipeline MUST test both:

```text
Windows
Linux
```

At minimum:

```text
Windows Server
Ubuntu Linux
```

Platform-specific functionality MUST be tested independently.

The core business logic MUST remain platform-independent.

---

# 41. Recommended Architecture

Use a layered architecture.

```text
┌─────────────────────────────────────────────┐
│                Presentation                 │
│                                             │
│       Web UI / CLI / API                   │
└──────────────────────┬──────────────────────┘
                       │
┌──────────────────────▼──────────────────────┐
│              Application Layer              │
│                                             │
│ Release Manager                             │
│ Deployment Manager                          │
│ Backup Manager                              │
│ Rollback Manager                            │
│ Diff Manager                                │
│ Retention Manager                           │
└──────────────────────┬──────────────────────┘
                       │
┌──────────────────────▼──────────────────────┐
│                 Domain Layer                │
│                                             │
│ Release                                     │
│ Deployment                                  │
│ Backup                                      │
│ Target                                      │
│ DeploymentState                             │
│ RetentionPolicy                             │
└──────────────────────┬──────────────────────┘
                       │
┌──────────────────────▼──────────────────────┐
│              Infrastructure                 │
│                                             │
│ File System                                 │
│ SSH/SFTP                                    │
│ PowerShell                                  │
│ Windows Service                             │
│ systemd                                     │
│ Database                                    │
│ Credential Store                            │
└─────────────────────────────────────────────┘
```

---

# 42. Recommended Core Components

The implementation SHOULD be divided into:

```text
Deployment.Core
Deployment.Application
Deployment.Domain
Deployment.Infrastructure
Deployment.CLI
Deployment.API
Deployment.Web
Deployment.Tests
```

Core services:

```text
ReleaseService
DeploymentService
BackupService
RollbackService
DiffService
RetentionService
VerificationService
AuditService
LockService
CredentialService
```

---

# 43. Database Model

Recommended entities:

```text
Application
Environment
DeploymentTarget
Release
ReleaseFile
Deployment
DeploymentStep
Backup
BackupFile
Rollback
RetentionPolicy
AuditEvent
User
Role
CredentialReference
```

Important relationships:

```text
Application
    │
    └── Environment
            │
            └── Target
                    │
                    ├── Deployment
                    ├── Backup
                    └── Release
```

---

# 44. Release Immutability

Once a release has been created, its contents MUST NOT be modified.

If changes are required:

```text
v1.4.2
```

must become:

```text
v1.4.3
```

Never modify:

```text
v1.4.2
```

after it has been released.

This guarantees reliable rollback.

---

# 45. Backup Immutability

Backups SHOULD also be immutable after creation.

Allowed metadata changes:

```text
Protected
Retention override
Description
```

The actual backup files SHOULD NOT be modified.

---

# 46. Deployment Approval

Deployment approval SHOULD be represented as a separate state.

Example:

```text
WAITING_APPROVAL
```

The system SHOULD record:

```text
Approved By
Approved At
Approval Comment
```

For production:

```text
ReleaseManager approval
```

may be required.

---

# 47. Recovery

The system MUST support recovery after the deployment engine itself crashes.

Example:

```text
DEPLOYING
```

If process crashes, the next startup should detect:

```text
Incomplete Deployment
```

and provide recovery options:

```text
Resume
Rollback
Mark Failed
Inspect
```

Do not assume that process termination means deployment failure or success.

---

# 48. Dry Run

The system SHOULD support:

```bash
deployctl deploy 1.4.2 production --dry-run
```

Dry-run MUST:

- Validate configuration
- Generate diff
- Check permissions
- Check connectivity
- Estimate changes

But MUST NOT:

- Modify target files
- Create production backup unless explicitly requested
- Restart services
- Delete files

Example output:

```text
DRY RUN

Target: Production/server01
Release: 1.4.2

Added:     12
Modified:  38
Deleted:    4

No changes were applied.
```

---

# 49. Preview Before Deployment

The standard workflow SHOULD be:

```text
Select Release
       ↓
Select Target
       ↓
Connectivity Check
       ↓
Generate Diff
       ↓
Review Diff
       ↓
Confirm
       ↓
Backup
       ↓
Deploy
       ↓
Verify
```

The UI MUST make the destructive point obvious.

---

# 50. CLI Safety

CLI commands that modify production MUST require explicit confirmation.

Example:

```bash
deployctl deploy 1.4.2 production
```

Output:

```text
WARNING:
You are about to deploy release 1.4.2
to PRODUCTION / server01.

38 files will be modified.
4 files will be deleted.

A backup will be created before deployment.

Continue? [y/N]
```

Automation mode may use:

```bash
--yes
```

but production policy MAY prohibit bypassing confirmation.

---

# 51. API Design

Provide REST APIs where applicable.

Example:

```text
GET    /api/releases
GET    /api/releases/{id}
GET    /api/releases/{id}/diff
POST   /api/deployments
GET    /api/deployments/{id}
POST   /api/deployments/{id}/cancel
POST   /api/deployments/{id}/rollback
GET    /api/backups
POST   /api/backups
DELETE /api/backups/{id}
POST   /api/backups/{id}/protect
GET    /api/audit
```

Long-running deployment operations SHOULD return a deployment ID.

Example:

```json
{
  "deploymentId": "DEP-20260903-001",
  "status": "BACKING_UP"
}
```

---

# 52. Event-Based Architecture

Deployment SHOULD emit events.

Example:

```text
DeploymentCreated
DiffGenerated
ApprovalGranted
BackupStarted
BackupCompleted
DeploymentStarted
FileDeployed
VerificationStarted
VerificationCompleted
DeploymentCompleted
DeploymentFailed
RollbackStarted
RollbackCompleted
```

This allows future integration with:

```text
Email
Teams
Slack
Webhook
Monitoring
SIEM
```

---

# 53. Observability

Provide:

```text
Deployment duration
Backup duration
Transfer size
File count
Success rate
Failure rate
Rollback count
```

Possible metrics:

```text
deployment_success_total
deployment_failure_total
deployment_rollback_total
backup_success_total
backup_failure_total
deployment_duration_seconds
```

---

# 54. Performance

The system SHOULD avoid copying unchanged files.

Use:

```text
Checksum
File size
Modification metadata
```

to identify unchanged files.

Deployment should preferably transfer:

```text
Added files
Modified files
Deleted files
```

instead of the entire application.

---

# 55. Large File Handling

The system MUST consider applications containing large files.

Avoid loading entire files into memory.

Diff and checksum implementations SHOULD support streaming.

Example:

```text
1 GB file
```

must be processed using streaming rather than reading the entire file into RAM.

---

# 56. Retention Cleanup Safety

Retention cleanup MUST be conservative.

Before deleting a backup:

```text
Check:
1. Is it protected?
2. Is it referenced by a release?
3. Is it required by rollback policy?
4. Is it within retention period?
5. Is it the minimum retained backup?
```

If uncertain:

```text
DO NOT DELETE
```

---

# 57. Architecture Rule: Never Couple Release to Backup Filename

Do not rely on parsing filenames such as:

```text
backup_20260903_v1.4.2.zip
```

for system state.

Metadata MUST be authoritative.

Use:

```text
Backup ID
Release ID
Target ID
```

as explicit references.

---

# 58. Deployment History

The system MUST retain deployment history independently from backup retention.

Deleting an old backup MUST NOT delete deployment history.

Example:

```text
Deployment History:

2026-08-01 v1.2.0 SUCCESS
2026-08-15 v1.3.0 SUCCESS
2026-08-20 v1.3.1 FAILED → ROLLED BACK
2026-09-03 v1.4.0 SUCCESS
```

Even if the physical backup for v1.2.0 is deleted, the history remains.

---

# 59. Minimum Viable Product

The first implementation SHOULD prioritize:

### Phase 1

```text
Cross-platform file deployment
Backup
Restore
Diff
Release management
Retention policy
CLI
Audit log
```

### Phase 2

```text
Web UI
RBAC
Remote deployment
SSH/SFTP
Windows Service support
systemd support
```

### Phase 3

```text
Approval workflow
Multi-target deployment
Parallel deployment
Notifications
Metrics
External secret managers
```

---

# 60. Critical Acceptance Criteria

The implementation is NOT considered complete unless all of the following work:

```text
[ ] Windows deployment works
[ ] Linux deployment works
[ ] Pre-deployment backup works
[ ] Backup verification works
[ ] Diff works
[ ] Text diff works
[ ] Binary difference detection works
[ ] Release history works
[ ] Rollback works
[ ] Rollback creates current-state backup
[ ] Retention policy works
[ ] Protected backups are preserved
[ ] Deployment locking works
[ ] Checksum verification works
[ ] Deployment failure recovery works
[ ] Audit logging works
[ ] Dry-run works
[ ] Production confirmation works
[ ] Deployment state recovery works after process crash
[ ] Automated tests pass on Windows
[ ] Automated tests pass on Linux
```

---

# 61. Non-Negotiable Safety Rules

These rules MUST NOT be violated:

### Rule 1

```text
No verified backup
        =
No production deployment
```

### Rule 2

```text
Failed deployment
        =
Attempt rollback
```

### Rule 3

```text
Protected backup
        =
Never automatically delete
```

### Rule 4

```text
Release created
        =
Release immutable
```

### Rule 5

```text
Deployment operation
        =
Audit record required
```

### Rule 6

```text
Production deployment
        =
Explicit confirmation required
```

### Rule 7

```text
Concurrent deployment to same target
        =
Blocked
```

### Rule 8

```text
Checksum mismatch
        =
Deployment failure
```

### Rule 9

```text
Unknown recovery state
        =
Do not continue automatically
```

---

# 62. Engineering Expectations

When implementing this project:

1. Prefer clean architecture over tightly coupled scripts.
2. Keep OS-specific functionality behind interfaces.
3. Do not duplicate Windows/Linux deployment logic.
4. Do not implement deployment as one giant function.
5. Every important operation should have a service boundary.
6. Every destructive operation must have a recovery strategy.
7. Every state-changing operation must be auditable.
8. Prefer immutable release artifacts.
9. Prefer atomic deployment mechanisms.
10. Write tests before implementing risky deployment logic.
11. Do not use hard-coded paths.
12. Do not hard-code credentials.
13. Do not silently swallow deployment errors.
14. Never report deployment success before verification succeeds.
15. Never report rollback success before the restored state has been verified.

---

# 63. Definition of Done

A feature is considered complete only when:

```text
Implementation
    +
Unit Tests
    +
Integration Tests
    +
Error Handling
    +
Logging
    +
Audit Trail
    +
Documentation
    +
Windows Validation
    +
Linux Validation
```

are completed.

Do not consider a feature complete merely because the happy-path demo works.

---

# 64. Recommended First Development Goal

Before implementing the Web UI, build the following complete vertical slice:

```text
Release
  ↓
Diff
  ↓
Approval
  ↓
Backup
  ↓
Backup Verification
  ↓
Deployment
  ↓
Checksum Verification
  ↓
Deployment Record
  ↓
Rollback
```

This vertical slice must work on:

```text
Windows
Linux
```

Only after this workflow is reliable should the project expand into:

```text
Web UI
RBAC
Approval Workflow
Notifications
Metrics
Advanced CI/CD Integration
```

The most important objective is not to create a feature-rich deployment UI.

The most important objective is:

> **Make deployment safe, reproducible, auditable, versioned, and reversible.**