namespace Deployment.CLI;

public static class DataPaths
{
    public record Resolved(string DataDir, string BackupStore, string ReleasesStore);

    public static Resolved Resolve()
    {
        var dataDir = Environment.GetEnvironmentVariable("DEPLOYCTL_DATA")
            ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "deployctl");

        var backupStore = Environment.GetEnvironmentVariable("DEPLOYCTL_BACKUPS")
            ?? Path.Combine(dataDir, "backups");

        var releasesStore = Environment.GetEnvironmentVariable("DEPLOYCTL_RELEASES")
            ?? Path.Combine(dataDir, "releases");

        return new Resolved(dataDir, backupStore, releasesStore);
    }
}
