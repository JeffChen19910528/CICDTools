**English** | [繁體中文](GUI_操作手冊.md)

# deployctl Desktop — User Guide

**Version:** deployctl-desktop  
**Platform:** Windows 10 / 11

---

## Table of Contents

1. [Overview](#1-overview)
2. [Starting the Application](#2-starting-the-application)
3. [Main Window](#3-main-window)
4. [Manage Applications](#4-manage-applications)
   - 4.1 [Create an Application](#41-create-an-application)
   - 4.2 [Add an Environment](#42-add-an-environment)
   - 4.3 [Add a Deployment Target](#43-add-a-deployment-target)
   - 4.4 [Edit Backup Excludes](#44-edit-backup-excludes)
5. [Release Management](#5-release-management)
   - 5.1 [Create a Release](#51-create-a-release)
   - 5.2 [View Release Details](#52-view-release-details)
6. [Deployment](#6-deployment)
   - 6.1 [Diff](#61-diff)
   - 6.2 [Dry Run](#62-dry-run)
   - 6.3 [Deploy](#63-deploy)
   - 6.4 [Rollback](#64-rollback)
7. [Backup Management](#7-backup-management)
   - 7.1 [Create a Backup](#71-create-a-backup)
   - 7.2 [Protect / Unprotect a Backup](#72-protect--unprotect-a-backup)
   - 7.3 [Clean Up Old Backups](#73-clean-up-old-backups)
8. [History](#8-history)
   - 8.1 [Deployment History](#81-deployment-history)
   - 8.2 [Audit Log](#82-audit-log)
9. [Recovery](#9-recovery)
10. [FAQ](#10-faq)

---

## 1. Overview

deployctl Desktop is a graphical deployment management tool built with Avalonia UI. It covers release packaging, multi-environment deployment, backup and auditing — all through mouse clicks and drop-downs, with no commands to type.

| Module | Description |
|---|---|
| Manage Applications | Create and maintain applications, environments and deployment targets |
| Release Management | Package and manage application releases |
| Deployment | Diff, dry-run, deploy and rollback |
| Backup Management | Create backups, protect important ones and clean up old ones |
| History | View deployment history and the full audit trail |
| Recovery | Detect and fix deployments stuck in an in-progress state |

---

## 2. Starting the Application

Double-click **deployctl-desktop.exe** to launch the desktop GUI.

**First launch:**
- The local SQLite database is initialised automatically; no separate database installation is needed.
- After the main window opens, the left panel shows the navigation list and the right panel shows the selected feature.
- All data is stored in `%APPDATA%\deployctl\` by default.

> **Note:** `deployctl-desktop.exe` and `deployctl.exe` (the command-line tool) share the same data and can be used side by side.

---

## 3. Main Window

The main window has two areas: the **left navigation panel** (200 px wide) and the **right content area**.

```
┌──────────────────┬────────────────────────────────────────┐
│  deployctl       │                                        │
│                  │         (content area)                 │
│  ▸ Applications  │                                        │
│  ▸ Releases      │   Shows the UI for the selected        │
│  ▸ Deploy        │   feature (grids, filters, buttons)    │
│  ▸ Backups       │                                        │
│  ▸ History       │                                        │
│  ▸ Recovery      │                                        │
└──────────────────┴────────────────────────────────────────┘
```

| Area | Description |
|---|---|
| Left navigation panel | Shows the system title and the feature list; click an item to switch the right panel |
| Right content area | Shows the UI for the selected feature |

> **Note:** The default window size is 1100 × 720. Toolbars wrap onto multiple lines when the window is narrow, so nothing is hidden.

---

## 4. Manage Applications

Click **Applications** in the left panel. This page manages all applications, environments and deployment targets.

**Toolbar buttons:**

| Button | Action |
|---|---|
| Create Application | Add a new application |
| Add Environment | Add an environment under an existing application (e.g. Production, Staging) |
| Add Target | Add a deployment target under an existing environment |
| Edit Excludes | Change the backup exclude directory list for a target |
| Refresh | Reload the latest data from the database |

**Grid columns:**

| Column | Description |
|---|---|
| Application | Application name |
| Environments | All environment names under that application |
| Targets | Target summary per environment |

### 4.1 Create an Application

1. Click **Create Application**.
2. Enter an application name (required).
3. Optionally enter a description.
4. Click **OK**.

> A duplicate name is not allowed; the system will show an error.

### 4.2 Add an Environment

1. Click **Add Environment**.
2. Select the parent application.
3. Enter an environment name (e.g. Production, Staging, UAT).
4. Optionally tick **Require Approval**.
5. Click **OK**.

### 4.3 Add a Deployment Target

1. Click **Add Target**.
2. Select the parent application and environment.
3. Enter the target name, OS type (Windows / Linux).
4. Enter the host name (defaults to `localhost`).
5. Enter or **Browse…** to pick the deployment path.
6. Set the backup exclude directories (default `log;logs`; separate multiple entries with `;`).
7. Click **OK**.

> The deployment path is the folder on the target machine where the application files live.

### 4.4 Edit Backup Excludes

1. Click **Edit Excludes**.
2. Select the deployment target to edit.
3. Update the exclude list (e.g. `log;logs;temp`).
4. Click **OK**.

> Directories in the exclude list are skipped during backup, avoiding large log files being archived.

---

## 5. Release Management

Click **Releases** in the left panel. Each release is an immutable, packaged snapshot of a source directory.

**Grid columns:**

| Column | Description |
|---|---|
| Release ID | System-generated unique identifier |
| Version | Version string (e.g. `1.0.0`, `2024.11.01`) |
| Created | Creation timestamp |
| By | The Windows user who created it |

### 5.1 Create a Release

1. Select the target application from the drop-down at the top.
2. Click **Create Release**.
3. Fill in:
   - Version (required)
   - Source folder path — the directory to package (required)
   - Notes (optional)
4. Click **OK**. The source folder is compressed and a SHA-256 checksum is computed for every file.
5. The new release appears in the grid.

> **Tip:** Use semantic versioning (e.g. `1.2.3`) or a date format (e.g. `2024.11.01`). A release cannot be modified once created; create a new version number for any update.

### 5.2 View Release Details

**Double-click** any row in the release grid to open the detail dialog, which shows:
- Release ID and version
- File list with SHA-256 checksums
- Release notes

---

## 6. Deployment

Click **Deploy** in the left panel. This is the core page: diff, dry-run, deploy and rollback.

**Target selector (top of page):** Choose **Application → Environment → Target → Release** in order. The drop-downs cascade; all actions use the current selection.

**Buttons:**

| Button | Description |
|---|---|
| Diff | Compare versions; no changes made |
| Dry Run | Run the full deploy logic without touching any files |
| **Deploy** (blue) | Deploy for real |
| Rollback | Restore the target to a selected previous release |

### 6.1 Diff

Compares the selected release against what is currently deployed on the target. **No files are changed.**

1. Select application, environment, target and release.
2. Click **Diff**.
3. The result panel shows:
   - `+` — file added
   - `~` — file modified
   - `-` — file removed
4. A summary line shows the count of added, modified, deleted and unchanged files.

### 6.2 Dry Run

Runs the full deployment logic but **does not copy or change any files**. Use this to validate the process before a real deployment.

1. Select application, environment, target and release.
2. Click **Dry Run**.
3. The result panel shows a success message; a DryRun deployment record is written to the audit log.

> A dry run does not create a backup and does not modify files.

### 6.3 Deploy

Deploys the selected release to the target.

1. Select application, environment, target and release.
2. Click the blue **Deploy** button.
3. A confirmation dialog shows the version, application, environment, target and a backup notice. Click **Yes** to proceed.
4. When done, the result panel shows the deployment ID and a success message.

**Deployment steps:**
1. Validate the release and target configuration
2. Back up the current deployment directory
3. Extract the release archive to the target path
4. Update the target's current release record
5. Write the audit record

> If the target path does not exist it is created automatically. A file lock prevents concurrent deployments to the same target.

### 6.4 Rollback

Restores the target to a previous release.

1. Select the release to roll back to from the release drop-down.
2. Click **Rollback**.
3. Confirm the dialog and click **Yes**.
4. The result panel shows the rollback deployment ID and a success message.

> A rollback is a deployment of an older release; the current state is backed up first.

---

## 7. Backup Management

Click **Backups** in the left panel. Create backups manually, protect important ones, or clean up old ones.

**Grid columns:**

| Column | Description |
|---|---|
| Backup ID | Unique identifier |
| Release | The release version that was deployed when the backup was taken |
| Created | Creation timestamp |
| Files | Number of files backed up |
| Size | Total compressed size (B / KB / MB / GB) |
| Status | Completed / Failed |
| Protected | Yes / No. Protected backups are never removed by the retention policy |

### 7.1 Create a Backup

1. Select application, environment and target using the target selector.
2. Click **Create Backup**.
3. The target directory is compressed (excluding directories in the exclude list). A dialog confirms the backup ID and file count.

### 7.2 Protect / Unprotect a Backup

1. Click a row in the backup grid to select it.
2. Click **Protect Backup**.
3. Confirm the dialog. The **Protected** column toggles between Yes and No.

> Protect milestone backups that you never want cleaned up automatically.

### 7.3 Clean Up Old Backups

Removes old backups according to the retention policy.

1. Select a target.
2. Click **Clean Up Backups**.
3. Confirm the dialog. The grid refreshes after cleanup.

> Protected backups are never removed by cleanup.

---

## 8. History

Click **History** in the left panel. Two tabs: **Deployment History** and **Audit Log**.

### 8.1 Deployment History

All deployment records for a selected target (deployments, dry runs and rollbacks).

1. Stay on the **Deployment History** tab (the default).
2. Select application, environment and target.
3. The grid loads automatically.

**Grid columns:**

| Column | Description |
|---|---|
| Deployment ID | Unique identifier |
| Release | Version deployed |
| Status | Completed (green), Failed (red), Cancelled (grey), RolledBack (green) |
| Operator | User who ran the deployment |
| Started | Start timestamp |
| Completed | Completion timestamp |

### 8.2 Audit Log

Complete audit trail of every operation across the system.

1. Switch to the **Audit Log** tab.
2. Use the application and environment drop-downs to filter.
3. Click **Refresh** to reload.

**Grid columns:**

| Column | Description |
|---|---|
| Timestamp | When the event occurred (local time) |
| Event | Event type, e.g. `DEPLOY_STARTED`, `DEPLOY_COMPLETED`, `BACKUP_CREATED` |
| Target | Application / environment / target path |
| Operator | User who performed the action |
| Result | SUCCESS (green), FAILED (red), STARTED (green) |
| Details | Error message or description |

---

## 9. Recovery

Click **Recovery** in the left panel. Use this page after an unexpected system restart to fix deployments that are stuck in an in-progress state.

The top of the page shows how many incomplete deployments were found.

**Steps:**

1. The grid lists all incomplete deployments automatically.
2. Click a row to select it.
3. Click **Mark as Failed**.
4. Confirm the dialog. The status changes to Failed and an audit record is written.

> After marking as failed, the deployment lock is released and the target can be deployed to again.

---

## 10. FAQ

**Q: "Database initialisation failed" on startup?**  
A: Check that `%APPDATA%\deployctl\` is writable, or run deployctl-desktop.exe as administrator.

**Q: "Target is locked" when deploying?**  
A: Another deployment may be in progress, or the previous one crashed mid-flight. If no one else is deploying, go to the Recovery page, mark the stuck deployment as failed, and the lock will be released.

**Q: Diff shows nothing?**  
A: The selected release and the current deployment are identical; no deployment is needed.

**Q: Disk space not freed after backup cleanup?**  
A: The OS reclaims disk space after file deletion; it may take a few seconds to a few minutes.

**Q: "Source folder does not exist" when creating a release?**  
A: Verify the source path is correct and that the current user has read access to it.
