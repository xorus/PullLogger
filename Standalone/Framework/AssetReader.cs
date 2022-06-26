using System;
using System.IO;
using PullLogger;

namespace Standalone.Framework;

public class AssetReader
{
    private readonly DirectoryInfo _baseDir;

    public AssetReader(Container container)
    {
        var custom = container.Resolve<AppConfig>().DalamudBaseDir;
        if (custom != null) _baseDir = new DirectoryInfo(custom);
        else
        {
            _baseDir = new DirectoryInfo(Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "XIVLauncher"
            ));
        }
    }

    public DirectoryInfo GetConfigDir() => new DirectoryInfo(Path.Combine(_baseDir.FullName, "pluginConfigs"));

    public DirectoryInfo GetUIResDir()
    {
        var dir = new DirectoryInfo(Path.Combine(_baseDir.FullName, "dalamudAssets"));
        if (!dir.Exists) throw new Exception("Could not find dalamudAssets directory" + dir.FullName);

        var ver = File.ReadAllText(Path.Combine(dir.FullName, "asset.ver"));
        var verDir = new DirectoryInfo(Path.Combine(dir.FullName, ver, "UIRes"));
        if (!verDir.Exists) throw new Exception("Could not find asset version directory " + verDir.FullName);

        return verDir;
    }
}