using PullLogger.Db.Data;

namespace PullLogger.Db;

public class DbManager
{
    public DbManager(Container container, string configDirectory)
    {
        Profiler.Start("db init");
        var db = new Database(configDirectory);
        db.Init();
        container.Register(db);
        Profiler.Stop("db init");

        container.Register<Exchange>();
    }
}