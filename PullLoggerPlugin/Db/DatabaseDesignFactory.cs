using System;
using System.IO;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Design;

namespace PullLogger.Db;

[UsedImplicitly]
public class DatabaseDesignFactory : IDesignTimeDbContextFactory<Database>
{
    /**
     * hardcoded path to the database file, for dev
     */
    public Database CreateDbContext(string[] args)
    {
        return new Database(
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "XIVLauncher",
                "pluginConfigs",
                "PullLogger"
            )
        );
    }
}