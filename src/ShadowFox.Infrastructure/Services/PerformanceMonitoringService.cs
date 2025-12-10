using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace ShadowFox.Infrastructure.Services;

/// <summary>
/// Service for monitoring performance metrics and logging performance data
/// </summary>
public interface IPerformanceMonitoringService
{
    /// <summary>
    /// Starts monitoring an operation
    /// </summary>
    IDisposable StartOperation(string operationName);
    
    /// <summary>
    /// Records a performance metric
    /// </summary>
    void RecordMetric(string metricName, double value, string unit = "ms");
    
    /// <summary>
    /// Gets performance statistics for an operation
    /// </summary>
    PerformanceStatistics GetStatistics(string operationName);
}

public class PerformanceStatistics
{
    public string OperationName { get; set; } = string.Empty;
    public int CallCount { get; set; }
    public double AverageTime { get; set; }
    public double MinTime { get; set; }
    public double MaxTime { get; set; }
    public double TotalTime { get; set; }
}

public class PerformanceMonitoringService : IPerformanceMonitoringService
{
    private readonly ILogger<PerformanceMonitoringService> _logger;
    private readonly Dictionary<string, List<double>> _operationTimes = new();
    private readonly object _lock = new();

    public PerformanceMonitoringService(ILogger<PerformanceMonitoringService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public IDisposable StartOperation(string operationName)
    {
        return new OperationTimer(operationName, this, _logger);
    }

    public void RecordMetric(string metricName, double value, string unit = "ms")
    {
        lock (_lock)
        {
            if (!_operationTimes.ContainsKey(metricName))
            {
                _operationTimes[metricName] = new List<double>();
            }
            
            _operationTimes[metricName].Add(value);
        }

        _logger.LogDebug("Performance metric recorded: {MetricName} = {Value} {Unit}", 
            metricName, value, unit);

        // Log warning for slow operations
        if (unit == "ms" && value > 1000)
        {
            _logger.LogWarning("Slow operation detected: {MetricName} took {Value}ms", 
                metricName, value);
        }
    }

    public PerformanceStatistics GetStatistics(string operationName)
    {
        lock (_lock)
        {
            if (!_operationTimes.ContainsKey(operationName) || _operationTimes[operationName].Count == 0)
            {
                return new PerformanceStatistics
                {
                    OperationName = operationName,
                    CallCount = 0,
                    AverageTime = 0,
                    MinTime = 0,
                    MaxTime = 0,
                    TotalTime = 0
                };
            }

            var times = _operationTimes[operationName];
            return new PerformanceStatistics
            {
                OperationName = operationName,
                CallCount = times.Count,
                AverageTime = times.Average(),
                MinTime = times.Min(),
                MaxTime = times.Max(),
                TotalTime = times.Sum()
            };
        }
    }

    private class OperationTimer : IDisposable
    {
        private readonly string _operationName;
        private readonly PerformanceMonitoringService _service;
        private readonly ILogger _logger;
        private readonly Stopwatch _stopwatch;

        public OperationTimer(string operationName, PerformanceMonitoringService service, ILogger logger)
        {
            _operationName = operationName;
            _service = service;
            _logger = logger;
            _stopwatch = Stopwatch.StartNew();
            
            _logger.LogDebug("Started operation: {OperationName}", operationName);
        }

        public void Dispose()
        {
            _stopwatch.Stop();
            var elapsedMs = _stopwatch.Elapsed.TotalMilliseconds;
            
            _service.RecordMetric(_operationName, elapsedMs);
            
            _logger.LogDebug("Completed operation: {OperationName} in {ElapsedMs}ms", 
                _operationName, elapsedMs);
        }
    }
}