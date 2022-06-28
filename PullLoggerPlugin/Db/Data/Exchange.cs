using System.IO;
using System.Linq;

namespace PullLogger.Db.Data;

public class Exchange
{
    private readonly Database _db;
    private readonly CsvExchange _csv;

    public Exchange(Container container)
    {
        _db = container.Resolve<Database>();
        _csv = new CsvExchange(container, this);
    }

    public void ImportFile(string file)
    {
        var fileInfo = new FileInfo(file);

        if (!fileInfo.Exists) return;
        if (fileInfo.FullName.EndsWith(".csv"))
        {
            _csv.Import(file);
        }
    }

    public void Export()
    {
    }

    // todo: to actual query for performance?
    public bool IsNotDuplicated(Pull pull)
    {
        if (pull.Time == null) return true;
        var minTime = pull.Time.Value.AddSeconds(-5);
        var maxTime = pull.Time.Value.AddSeconds(5);

        var a = !_db.Pulls.Any(x => x.EventName == pull.EventName && x.Time > minTime && x.Time < maxTime);
        return a;
    }

    public void ComputePullNumbers()
    {
        var a = from pulls in _db.Pulls where pulls.EventName == PullEvent.Pull select pulls;
    }
}