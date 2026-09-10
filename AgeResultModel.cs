namespace MyApi;

public sealed class AgeResultModel
{
    public string UserName { get; init; } = string.Empty;
    public int Age { get; init; }
    public bool IsAdult => Age >= 18;
}
