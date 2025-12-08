namespace JumpTaskAutomation.Logging;

public interface IAutomationLogger
{
    void Info(string message);
    void Warn(string message);
    void Error(string message);
    void Error(string message, Exception exception);
}

public sealed class ConsoleAutomationLogger : IAutomationLogger
{
    private readonly object sync = new();

    public void Info(string message) => Write("INFO", message, ConsoleColor.Cyan);

    public void Warn(string message) => Write("WARN", message, ConsoleColor.Yellow);

    public void Error(string message) => Write("ERROR", message, ConsoleColor.Red);

    public void Error(string message, Exception exception) =>
        Write("ERROR", $"{message} :: {exception.Message}", ConsoleColor.Red);

    private void Write(string level, string message, ConsoleColor color)
    {
        lock (sync)
        {
            var previous = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.WriteLine($"[{DateTimeOffset.Now:HH:mm:ss}] {level}: {message}");
            Console.ForegroundColor = previous;
        }
    }
}
