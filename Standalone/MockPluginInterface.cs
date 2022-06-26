using System.IO;
using Dalamud.Configuration;
using Newtonsoft.Json;

namespace Standalone;

/**
 * most of this is lifted from dalamud PluginInterface code
 */
public class MockPluginInterface
{
    private readonly string _pluginName;
    private readonly DirectoryInfo _configDirectory;

    public MockPluginInterface(string pluginName, string configDirectory)
    {
        _pluginName = pluginName;
        _configDirectory = new DirectoryInfo(configDirectory);
    }

    private FileInfo GetConfigFile(string pluginName) =>
        new FileInfo(Path.Combine(_configDirectory.FullName, pluginName + ".json"));

    private DirectoryInfo GetDirectoryPath(string pluginName) =>
        new DirectoryInfo(Path.Combine(_configDirectory.FullName, pluginName));

    public IPluginConfiguration? GetPluginConfig()
    {
        var file = GetConfigFile(_pluginName);
        if (!file.Exists) return null;
        return JsonConvert.DeserializeObject<IPluginConfiguration>(File.ReadAllText(file.FullName),
            new JsonSerializerSettings
            {
                TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple,
                TypeNameHandling = TypeNameHandling.Objects
            });
    }

    public string GetDirectory(string pluginName)
    {
        try
        {
            DirectoryInfo directoryPath = this.GetDirectoryPath(pluginName);
            if (!directoryPath.Exists)
                directoryPath.Create();
            return directoryPath.FullName;
        }
        catch
        {
            return string.Empty;
        }
    }

    private void Save(IPluginConfiguration config, string pluginName) => File.WriteAllText(
        GetConfigFile(pluginName).FullName, JsonConvert.SerializeObject((object)config, Formatting.Indented,
            new JsonSerializerSettings()
            {
                TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple,
                TypeNameHandling = TypeNameHandling.Objects
            }));

    public void SavePluginConfig(IPluginConfiguration? currentConfig)
    {
        if (currentConfig == null) return;
        Save(currentConfig, _pluginName);
    }
}