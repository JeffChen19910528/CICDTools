namespace Deployment.CLI.Localization;

/// <summary>
/// Minimal in-code localization for the interactive menu (English / Traditional Chinese).
/// Kept as plain dictionaries (rather than .resx satellite assemblies) so everything stays
/// inside the single self-contained published executable.
/// </summary>
public static class L
{
    public static Language Current { get; set; } = Language.English;

    public static string T(string key, params object[] args)
    {
        if (!Map.TryGetValue(key, out var entry))
            return key;

        var template = Current == Language.TraditionalChinese ? entry.Zh : entry.En;
        return args.Length == 0 ? template : string.Format(template, args);
    }

    private static readonly Dictionary<string, (string En, string Zh)> Map = new()
    {
        ["app.title"] = ("deployctl — Deployment Management Tool", "deployctl — 部署管理工具"),

        // Main menu
        ["menu.prompt"] = ("What would you like to do?", "請選擇您要執行的操作："),
        ["menu.apps"] = ("Manage Applications", "應用程式管理"),
        ["menu.releases"] = ("Release Management", "版本管理"),
        ["menu.deploy"] = ("Deploy & Rollback", "部署與回滾"),
        ["menu.backups"] = ("Backup Management", "備份管理"),
        ["menu.history"] = ("History & Audit", "歷史與稽核紀錄"),
        ["menu.recovery"] = ("Recovery", "異常復原"),
        ["menu.language"] = ("Language Settings", "語言設定"),
        ["menu.exit"] = ("Exit", "離開"),
        ["menu.goodbye"] = ("Goodbye!", "再見！"),

        // Common
        ["common.back"] = ("Back", "返回上一層"),
        ["common.pressEnter"] = ("Press Enter to continue...", "按 Enter 鍵繼續..."),
        ["common.yes"] = ("Yes", "是"),
        ["common.no"] = ("No", "否"),
        ["common.ok"] = ("OK", "確定"),
        ["common.cancel"] = ("Cancel", "取消"),
        ["common.browse"] = ("Browse…", "瀏覽…"),
        ["common.refresh"] = ("Refresh", "重新整理"),
        ["common.all"] = ("(All)", "（全部）"),
        ["common.error"] = ("Error: {0}", "發生錯誤：{0}"),
        ["common.noAppsYet"] = ("No applications registered yet. Please create one first.", "尚未建立任何應用程式，請先建立一個。"),
        ["common.noEnvsYet"] = ("This application has no environments yet. Please add one first.", "此應用程式尚未有任何環境，請先新增一個。"),
        ["common.noTargetsYet"] = ("This environment has no deployment targets yet. Please add one first.", "此環境尚未有任何部署目標，請先新增一個。"),
        ["common.noReleasesYet"] = ("No releases found for this application.", "此應用程式尚無任何版本。"),
        ["common.noBackupsYet"] = ("No backups found for this target.", "此目標尚無任何備份。"),
        ["common.operationCancelled"] = ("Operation cancelled.", "操作已取消。"),
        ["common.confirmContinue"] = ("Continue?", "確定要繼續嗎？"),

        ["select.app"] = ("Select an application:", "請選擇應用程式："),
        ["select.env"] = ("Select an environment:", "請選擇環境："),
        ["select.target"] = ("Select a deployment target:", "請選擇部署目標："),
        ["select.release"] = ("Select a release version:", "請選擇版本："),
        ["select.backup"] = ("Select a backup:", "請選擇備份："),

        // App submenu
        ["app.menu.title"] = ("Manage Applications", "應用程式管理"),
        ["app.menu.create"] = ("Create Application", "新增應用程式"),
        ["app.menu.addEnv"] = ("Add Environment", "新增環境"),
        ["app.menu.addTarget"] = ("Add Deployment Target", "新增部署目標"),
        ["app.menu.list"] = ("List Applications", "列出所有應用程式"),

        ["app.create.namePrompt"] = ("Application name:", "應用程式名稱："),
        ["app.create.descPrompt"] = ("Description (optional):", "描述（可省略）："),
        ["app.create.success"] = ("Application '{0}' created.", "應用程式「{0}」已建立。"),
        ["app.create.exists"] = ("Application '{0}' already exists.", "應用程式「{0}」已存在。"),

        ["app.env.namePrompt"] = ("Environment name (e.g. production):", "環境名稱（例如：production）："),
        ["app.env.requireApproval"] = ("Require manual approval for deployments to this environment?", "此環境的部署是否需要人工核准？"),
        ["app.env.success"] = ("Environment '{0}' added to '{1}'.", "環境「{0}」已新增至「{1}」。"),
        ["app.env.exists"] = ("Environment '{0}' already exists.", "環境「{0}」已存在。"),

        ["app.target.namePrompt"] = ("Target name (e.g. web1):", "目標名稱（例如：web1）："),
        ["app.target.osPrompt"] = ("Target operating system:", "目標作業系統："),
        ["app.target.hostPrompt"] = ("Host name or address (default: localhost):", "主機名稱或位址（預設：localhost）："),
        ["app.target.pathPrompt"] = ("Deployment path on the target (e.g. C:\\apps\\myapp or /var/www/myapp):", "目標上的部署路徑（例如：C:\\apps\\myapp 或 /var/www/myapp）："),
        ["app.target.success"] = ("Target '{0}' added.", "目標「{0}」已新增。"),

        ["app.list.title"] = ("Registered Applications", "已登記的應用程式"),
        ["app.list.colApp"] = ("Application", "應用程式"),
        ["app.list.colEnvs"] = ("Environments", "環境"),
        ["app.list.colTargets"] = ("Targets", "部署目標"),

        // Release submenu
        ["release.menu.title"] = ("Release Management", "版本管理"),
        ["release.menu.create"] = ("Create Release", "建立新版本"),
        ["release.menu.list"] = ("List Releases", "列出版本"),
        ["release.menu.show"] = ("Show Release Detail", "查看版本詳情"),

        ["release.create.versionPrompt"] = ("Version number (e.g. 1.0.0):", "版本號（例如：1.0.0）："),
        ["release.create.sourcePrompt"] = ("Source folder path (files to package):", "來源資料夾路徑（要封裝的檔案）："),
        ["release.create.notesPrompt"] = ("Release notes (optional):", "版本備註（可省略）："),
        ["release.create.sourceNotFound"] = ("Source folder not found: {0}", "找不到來源資料夾：{0}"),
        ["release.create.success"] = ("Release '{0}' created with {1} file(s).", "版本「{0}」已建立，共 {1} 個檔案。"),

        ["release.list.title"] = ("Releases for {0}", "{0} 的版本列表"),
        ["release.list.colId"] = ("Release ID", "版本編號"),
        ["release.list.colVersion"] = ("Version", "版本"),
        ["release.list.colCreated"] = ("Created", "建立時間"),
        ["release.list.colBy"] = ("By", "建立者"),

        ["release.show.files"] = ("Files", "檔案"),
        ["release.show.colPath"] = ("Path", "路徑"),
        ["release.show.colSize"] = ("Size", "大小"),
        ["release.show.colSha"] = ("SHA-256", "SHA-256"),

        // Deploy submenu
        ["deploy.menu.title"] = ("Deploy & Rollback", "部署與回滾"),
        ["deploy.menu.diff"] = ("View Diff (Preview Changes)", "查看差異（預覽變更）"),
        ["deploy.menu.dryrun"] = ("Deploy (Dry Run — No Changes Made)", "部署（模擬執行，不會變更任何檔案）"),
        ["deploy.menu.run"] = ("Deploy (Apply Changes)", "部署（正式執行）"),
        ["deploy.menu.rollback"] = ("Rollback to a Previous Version", "回滾到先前的版本"),

        ["deploy.confirm.warning"] = ("You are about to deploy version '{0}' to {1}/{2}/{3}.", "您即將部署版本「{0}」到 {1}/{2}/{3}。"),
        ["deploy.confirm.backupNote"] = ("A backup will be created automatically before deployment. Continue?", "系統會在部署前自動建立備份，確定要繼續嗎？"),
        ["deploy.result.dryrunDone"] = ("Dry run completed. No files were changed.", "模擬執行完成，未變更任何檔案。"),
        ["deploy.result.success"] = ("Deployment '{0}' completed successfully.", "部署「{0}」已成功完成。"),
        ["deploy.result.failed"] = ("Deployment failed: {0}", "部署失敗：{0}"),

        ["deploy.diff.summary"] = ("Source: {0}   Target: {1}", "來源版本：{0}　　目前版本：{1}"),
        ["deploy.diff.counts"] = ("Added: {0}   Modified: {1}   Deleted: {2}   Unchanged: {3}", "新增：{0}　修改：{1}　刪除：{2}　未變更：{3}"),

        ["rollback.versionPrompt"] = ("Version to roll back to:", "要回滾到的版本："),
        ["rollback.confirm.warning"] = ("You are about to roll back {0}/{1}/{2} to version '{3}'. A backup of the current state will be created first. Continue?", "您即將把 {0}/{1}/{2} 回滾到版本「{3}」，系統會先備份目前狀態，確定要繼續嗎？"),
        ["rollback.result.success"] = ("Rollback '{0}' completed. Now at version '{1}'.", "回滾「{0}」已完成，目前版本為「{1}」。"),
        ["rollback.result.failed"] = ("Rollback failed: {0}", "回滾失敗：{0}"),

        // Backup submenu
        ["backup.menu.title"] = ("Backup Management", "備份管理"),
        ["backup.menu.create"] = ("Create Backup Now", "立即建立備份"),
        ["backup.menu.list"] = ("List Backups", "列出備份"),
        ["backup.menu.protect"] = ("Protect / Unprotect a Backup", "保護／取消保護備份"),
        ["backup.menu.cleanup"] = ("Cleanup Old Backups (Apply Retention Policy)", "清理舊備份（套用保留政策）"),

        ["backup.create.success"] = ("Backup '{0}' created and verified ({1} file(s)).", "備份「{0}」已建立並驗證完成（{1} 個檔案）。"),
        ["backup.list.title"] = ("Backups for {0}/{1}/{2}", "{0}/{1}/{2} 的備份列表"),
        ["backup.list.colId"] = ("Backup ID", "備份編號"),
        ["backup.list.colRelease"] = ("Release", "版本"),
        ["backup.list.colCreated"] = ("Created", "建立時間"),
        ["backup.list.colFiles"] = ("Files", "檔案數"),
        ["backup.list.colSize"] = ("Size", "大小"),
        ["backup.list.colStatus"] = ("Status", "狀態"),
        ["backup.list.colProtected"] = ("Protected", "已保護"),

        ["backup.protect.prompt"] = ("Protect this backup? (Protected backups are never auto-deleted)", "是否保護此備份？（受保護的備份不會被自動清理）"),
        ["backup.protect.protectedMsg"] = ("Backup '{0}' is now protected.", "備份「{0}」已設為保護。"),
        ["backup.protect.unprotectedMsg"] = ("Protection removed from backup '{0}'.", "已取消保護備份「{0}」。"),

        ["backup.cleanup.confirm"] = ("This will delete backups outside the retention policy for {0}/{1}/{2}. Continue?", "此操作將刪除 {0}/{1}/{2} 超出保留政策範圍的備份，確定要繼續嗎？"),
        ["backup.cleanup.success"] = ("Retention cleanup complete.", "保留政策清理完成。"),

        // History submenu
        ["history.menu.title"] = ("History & Audit", "歷史與稽核紀錄"),
        ["history.menu.deployments"] = ("View Deployment History", "查看部署歷史"),
        ["history.menu.audit"] = ("View Audit Log", "查看稽核紀錄"),

        ["history.deploy.title"] = ("Deployment History for {0}/{1}/{2}", "{0}/{1}/{2} 的部署歷史"),
        ["history.deploy.colId"] = ("Deployment ID", "部署編號"),
        ["history.deploy.colRelease"] = ("Release", "版本"),
        ["history.deploy.colStatus"] = ("Status", "狀態"),
        ["history.deploy.colOperator"] = ("Operator", "操作者"),
        ["history.deploy.colStarted"] = ("Started", "開始時間"),
        ["history.deploy.colCompleted"] = ("Completed", "完成時間"),

        ["history.audit.title"] = ("Audit Log", "稽核紀錄"),
        ["history.audit.colTime"] = ("Timestamp", "時間"),
        ["history.audit.colEvent"] = ("Event", "事件"),
        ["history.audit.colTarget"] = ("App/Env/Target", "應用程式/環境/目標"),
        ["history.audit.colOperator"] = ("Operator", "操作者"),
        ["history.audit.colResult"] = ("Result", "結果"),
        ["history.audit.colDetails"] = ("Details", "詳情"),
        ["history.audit.filterPrompt"] = ("Filter by application? (leave empty for all)", "依應用程式篩選？（留空表示全部）"),
        ["history.audit.none"] = ("No audit events found.", "查無稽核紀錄。"),

        // Recovery submenu
        ["recovery.menu.title"] = ("Recovery", "異常復原"),
        ["recovery.menu.status"] = ("View Incomplete Deployments", "查看未完成的部署"),
        ["recovery.menu.markFailed"] = ("Mark a Deployment as Failed", "將部署標記為失敗"),

        ["recovery.status.none"] = ("No incomplete deployments found.", "沒有發現未完成的部署。"),
        ["recovery.status.found"] = ("Found {0} incomplete deployment(s):", "發現 {0} 個未完成的部署："),
        ["recovery.markFailed.idPrompt"] = ("Deployment ID to mark as failed:", "要標記為失敗的部署編號："),
        ["recovery.markFailed.success"] = ("Deployment '{0}' marked as FAILED.", "部署「{0}」已標記為失敗。"),
        ["recovery.markFailed.notFound"] = ("Deployment '{0}' not found.", "找不到部署「{0}」。"),

        // Language submenu
        ["lang.menu.title"] = ("Language Settings", "語言設定"),
        ["lang.menu.current"] = ("Current language: {0}", "目前語言：{0}"),
        ["lang.menu.changed"] = ("Language switched to {0}.", "語言已切換為 {0}。"),
    };
}
