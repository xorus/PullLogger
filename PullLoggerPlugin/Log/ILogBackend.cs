namespace PullLogger.Log;

public interface ILogBackend
{
    public void Log(PullRecord record);
}