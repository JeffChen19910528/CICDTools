**English** | [繁體中文](README.md)

# deployctl

A cross-platform (Windows / Linux) **application deployment and release management tool**.

`deployctl` is not a general-purpose CI runner. It focuses on one job: *safely deploying a set of files to a server*.
A backup is always taken before every deployment, every release is immutable once created, failures trigger an automatic recovery attempt, and every step is recorded in an audit trail.

It is meant for managing production deployments of internal systems, websites or services, replacing manual file copying, forgotten backups, and the "the deployment failed halfway and nobody knows what to do" situation.

## What it does

- **Releases**: packages a source directory into an immutable release and records the SHA-256 hash of every file.
- **Deploy**: deploys a chosen release to a chosen application / environment / target (for example `MyApp / production / web1`).
- **Diff before deploying**: shows exactly which files will be added, modified or removed before anything is changed.
- **Automatic backup**: the current state is always backed up before a deployment or rollback, and the backup is verified; if the backup fails, the deployment does not proceed.
- **Backup exclude directories**: each deployment target can list subdirectories to skip when backing up (`log` and `logs` by default), so bulky log files that are not part of a release are not backed up.
- **One-step rollback**: if a deployment turns out to be bad, go straight back to a previous release; the current state is backed up before rolling back.
- **Retention policy**: expired backups are cleaned up automatically, except those that are protected, referenced as rollback candidates, or needed to meet the minimum retention count.
- **History / Audit**: every deployment, rollback and backup leaves a complete record of when, who and the result.
- **Recovery**: if the program is interrupted mid-deployment, the next start lists the unfinished deployment so you can decide what to do; nothing is guessed automatically.
- **Interactive menu**: double-click the executable to get a text menu, with no commands to memorize; **English / Traditional Chinese** can be switched at any time.
- **Desktop GUI**: a separate `deployctl-gui` desktop app does everything with mouse clicks, drop-downs and folder pickers, with no paths or commands to type. Good for people who are not comfortable with a terminal.
- **Command line still available**: if you prefer commands (for example in automation scripts), keep using `deployctl <command> ...`. The text menu, the GUI and the command line share the same data, so you can mix them.

## For non-developers: how to use it

You do not need to install any development tools. Just get the **single executable** for your operating system:

- Windows → `deployctl.exe`
- Linux → `deployctl`

If you received the source code and want to build it yourself, see "For developers: building" below. After building, copy the produced file to any computer running the same type of system; **no separate .NET installation is needed**.

### Starting the tool

- **Windows**: **double-click** `deployctl.exe`.
- **Linux**: run in a terminal:
  ```bash
  chmod +x ./deployctl      # needed the first time only
  ./deployctl
  ```

Started with no arguments, the tool opens the **text menu**. Use the arrow keys to choose an item, Enter to confirm, and type names or paths when prompted. No commands to remember.

On the first run you are asked to choose a language (English / 繁體中文). The choice is remembered and used on every later start; you can change it any time from "Language Settings" in the main menu.

The main menu covers each item in "What it does": Manage Applications, Release Management, Deploy & Rollback, Backup Management, History & Audit, Recovery and Language Settings. Wherever you need to pick an application / environment / target, the existing ones are listed so you do not have to type full names.

> If the tool is started without a terminal window (for example a script that redirects output to a file), it prints the command help instead of trying to open the menu.

### Desktop GUI

If you would rather not use the terminal menu, use the desktop GUI `deployctl-gui` (`deployctl-gui.exe` on Windows). It offers the same features as the text menu (Manage Applications, Release Management, Deploy & Rollback, Backup Management, History & Audit, Recovery, Language Settings). The differences:

- Everywhere you choose an application / environment / target / release, you use a drop-down with the mouse.
- The deployment path of a target and the source folder of a release can be picked with the **Browse…** button and the system folder picker, so there is nothing to type and no typos.
- When adding a deployment target you can fill in the "backup exclude directories" (default `log;logs`). Later, on the "Manage Applications" page, click **Edit Backup Excludes**, pick the target, edit the list and press OK.
- Risky operations such as a real deployment, rollback or backup cleanup always show a confirmation dialog first.
- Toolbars and selectors wrap onto several lines when the window is narrow, so nothing is cut off.

The GUI, the text menu and the command line share one database, one backup directory and one release directory (controlled by the same `DEPLOYCTL_DATA` / `DEPLOYCTL_BACKUPS` / `DEPLOYCTL_RELEASES` environment variables). You can create an app in the GUI today and deploy it from the command line tomorrow.

## For developers: building

Requirement: [.NET SDK 8.0 or later](https://dotnet.microsoft.com/download).

### Build and test (development)

```bash
dotnet build          # build the whole solution
dotnet test           # run all unit tests
```

### Building a Windows executable

Open PowerShell on Windows and run from the repository root:

```powershell
.\build-windows.ps1
```

When it finishes, `publish\windows\deployctl.exe` is a single executable you can hand to other Windows users (it bundles the .NET runtime, so nothing else needs to be installed).

### Building a Linux executable

On a machine with the .NET SDK (Windows can cross-compile the Linux build too):

```bash
./build-linux.sh
```

When it finishes, `publish/linux/deployctl` is a single executable you can hand to other Linux (x64) users.

> You can also run the matching `dotnet publish` command yourself; both scripts call the standard
> `dotnet publish -r <win-x64|linux-x64> --self-contained -p:PublishSingleFile=true`.

## Where data is stored

`deployctl` keeps the database, releases and backups in the following places (each can be overridden with an environment variable):

| Purpose | Environment variable | Default location |
|---|---|---|
| Database and settings | `DEPLOYCTL_DATA` | Windows: `%APPDATA%\deployctl`  Linux: `~/.config/deployctl` |
| Backups | `DEPLOYCTL_BACKUPS` | `<DEPLOYCTL_DATA>\backups` |
| Releases | `DEPLOYCTL_RELEASES` | `<DEPLOYCTL_DATA>\releases` |

To keep data on another disk or path, set the environment variable before running, for example:

```powershell
$env:DEPLOYCTL_DATA = "D:\deployctl-data"
.\deployctl.exe app list
```

## Usage guide (command line)

> This section shows the `deployctl <command> ...` command-line style, which suits automation scripts.
> If you use the **interactive menu** (by double-clicking the executable), you navigate with menus instead; the features are the same, so you can skip this section.

The following example walks through the whole flow from registering an application to deploying and rolling back.

### 1. Register an application, environment and deployment target

```bash
# Create an application
deployctl app create MyApp --description "Company intranet"

# Add an environment (for example production); --require-approval means this environment needs extra approval
deployctl app add-env MyApp production --require-approval

# Add a deployment target (the machine and path that will receive the files)
deployctl app add-target MyApp production web1 /var/www/myapp --os linux --host 192.168.1.10

# Choose the subdirectories to skip when backing up (semicolon-separated; default is "log;logs")
deployctl app add-target MyApp production web2 /var/www/myapp --exclude "logs;tmp;cache"

# Change the backup exclude list of an existing target (an empty string "" means exclude nothing)
deployctl app set-excludes MyApp production web1 "logs;uploads/temp"

# List all registered applications, environments and targets
deployctl app list
```

### 2. Create a release

Put the files to deploy in a folder, then package them as a release:

```bash
deployctl release create MyApp 1.0.0 ./dist --operator alice --commit a1b2c3d --notes "First go-live"

# List all releases of an application
deployctl release list MyApp

# Show one release in detail (file list and SHA-256)
deployctl release show MyApp-1.0.0
```

> A release cannot be modified once created. If the code changes, create a new version number (for example `1.0.1`).

### 3. Check the differences before deploying

Before a real deployment, check which files will be affected:

```bash
# Compare the new release with what is currently deployed on the target
deployctl deploy diff MyApp production web1 1.0.0

# Add --show-unchanged to also list files that did not change
deployctl deploy diff MyApp production web1 1.0.0 --show-unchanged
```

In the output:
- green `+` is a new file
- yellow `~` is a modified file (text files show a line-by-line diff)
- red `-` is a file that will be removed

### 4. Deploy

```bash
# Simulate first with --dry-run; no files are changed
deployctl deploy run MyApp production web1 1.0.0 --dry-run

# Real deployment (without --yes you get a warning and a confirmation prompt)
deployctl deploy run MyApp production web1 1.0.0

# Skip the interactive confirmation in automation scripts
deployctl deploy run MyApp production web1 1.0.0 --yes
```

A deployment runs these steps in order: validate → diff → **create and verify a backup** (if it fails, the deployment stops) → deploy files → verify file hashes → write the deployment record. If any step fails, the system tries to roll back automatically so the target is never left half-deployed.

### 5. Found a problem? Roll back

```bash
deployctl deploy rollback MyApp production web1 1.0.0 --yes
```

The current state is backed up before rolling back too, so the rollback itself cannot cause data loss.

### 6. Backup management

A backup skips the target's "exclude directories" (see section 1 above; `log` and `logs` by default). Skipped files are not copied, are not counted in the file count or size, and take no part in integrity verification. The rules:

- A bare directory name (for example `logs`) skips any directory with that name at any depth (`logs/`, `sub/logs/`), case-insensitively.
- A relative path (for example `data/cache`) skips only that path, counted from the deployment directory.
- Changing the exclude list only affects backups created afterwards; existing backups are not modified.
- Interactive menu: "Manage Applications" → "Edit Backup Excludes"; you are also asked when adding a deployment target.

```bash
# Create a backup manually (outside a deployment)
deployctl backup create MyApp production web1

# List all backups
deployctl backup list MyApp production web1

# Mark an important backup as "protected"; protected backups are never removed by the retention policy
deployctl backup protect BKP-20260101-120000 --protect

# Remove protection
deployctl backup protect BKP-20260101-120000 --protect false

# Clean up expired backups per the retention policy (protected backups, rollback candidates and backups needed for the minimum count are kept)
deployctl backup cleanup MyApp production web1
```

### 7. History and audit trail

```bash
# Deployment history of a target (successes, failures and rollbacks)
deployctl history deployments MyApp production web1

# Audit events (full trail of every operation: who, when, result)
deployctl history audit MyApp --environment production --limit 100

# Without an application name, show all audit events
deployctl history audit
```

### 8. Recovering from an interrupted deployment

If the computer crashed or the program was killed during a deployment, check for unfinished deployments after restarting:

```bash
deployctl recovery status
```

The system **does not guess** how to handle an unfinished deployment. It lists them and leaves the decision to you, for example marking one as failed by hand:

```bash
deployctl recovery mark-failed DEP-20260101-120000
```

## FAQ

**Q: My antivirus blocks the `.exe` / executable. What should I do?**
A self-built single-file executable has no digital signature, so some antivirus products and Windows SmartScreen may block executables from unknown sources. This is normal: once you trust the source, choose "Run anyway". If you plan to distribute it to many machines, sign it with a code-signing certificate.

**Q: Do I have to use `--yes` to automate in a script?**
Yes. Production deployments and rollbacks always require manual confirmation (`Continue? [y/N]`) by default and are skipped only with an explicit `--yes`, so an automation script cannot hit production by accident.

**Q: I accidentally deleted the database or the release files. What now?**
The deployment history (database) and the backup retention policy are designed separately: deleting old backups does not affect the deployment history. But if the database itself (the `DEPLOYCTL_DATA` directory) is lost, all application registrations and history are lost, so back up that directory regularly.

**Q: Where is the language setting stored? How do I reset it?**
The language preference is stored in `cli-settings.json` under the `DEPLOYCTL_DATA` directory (on Windows by default `%APPDATA%\deployctl\cli-settings.json`). Change it any time from "Language Settings" in the interactive menu, or delete the file and you will be asked again on the next start.

**Q: Double-clicking the executable does nothing, or the window flashes and closes.**
The interactive menu needs a real terminal window (it reads the arrow keys). If the tool is started in a way that redirects output (for example some shortcut settings or schedulers), it prints the command help and exits; this is intentional. Double-click the executable, or run it in a command prompt / terminal, to see the menu.

## Project layout (for developers)

```
Deployment.Domain          — core entities (Release, Deployment, Backup, Target, RetentionPolicy)
Deployment.Application     — business services and their interfaces
Deployment.Infrastructure  — platform implementations (file system, SQLite database, locking, etc.)
Deployment.Shared          — localization (Localization/) and settings storage (Settings/) shared by the CLI and GUI
Deployment.CLI             — the deployctl command line + interactive text menu (Interactive/)
Deployment.Desktop         — the deployctl-gui desktop GUI (Avalonia, cross-platform Windows/Linux)
Deployment.Tests           — unit and integration tests (including tests that script the interactive menu with Spectre.Console.Testing)
```

Operating-system-specific logic is always isolated behind interfaces (`IFileSystem`, `IChecksumService`, `ILockService`, and so on), so the core engine contains no Windows- or Linux-specific code.
