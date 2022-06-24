namespace PullLogger.Log;

public interface ILogBackend
{
    public void Log(PullRecord record);

    /**
     * Removes the last pull event.
     */
    public void RetCon();
    
    /**
     * If you goofed up.
     */
    public void UnRetCon();
}