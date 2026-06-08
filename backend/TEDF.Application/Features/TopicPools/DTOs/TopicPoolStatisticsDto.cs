namespace TEDF.Application.Features.TopicPools.DTOs;

public class TopicPoolStatisticsDto
{
    public Guid PoolId { get; init; }
    public string PoolCode { get; init; } = string.Empty;
    public string PoolName { get; init; } = string.Empty;
    public int TotalMentors { get; init; }
    public int TotalTopicsCount { get; init; }
    public int ActiveTopicsCount { get; init; }
    public int RegisteredTopicsCount { get; init; }
    public int ExpiredTopicsCount { get; init; }
}
