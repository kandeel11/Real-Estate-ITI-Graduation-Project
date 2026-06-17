namespace RealEstate.Host.Infrastructure;

public sealed class HangfireOptions
{
    public bool Enabled { get; set; } = true;
    public bool DashboardEnabled { get; set; } = false;
    public int WorkerCount { get; set; } = 2;
    public int QueuePollIntervalSeconds { get; set; } = 30;
}
