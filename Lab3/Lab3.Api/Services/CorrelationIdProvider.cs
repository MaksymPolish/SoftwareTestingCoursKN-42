namespace Lab3.Api.Services;

/// <summary>
/// Implementation of correlation ID provider using HttpContext
/// </summary>
public class CorrelationIdProvider : ICorrelationIdProvider
{
    private readonly IHttpContextAccessor _contextAccessor;
    private const string CorrelationIdKey = "CorrelationId";

    public CorrelationIdProvider(IHttpContextAccessor contextAccessor)
    {
        _contextAccessor = contextAccessor;
    }

    public string GenerateCorrelationId()
    {
        var correlationId = Guid.NewGuid().ToString();
        SetCorrelationId(correlationId);
        return correlationId;
    }

    public string GetCorrelationId()
    {
        if (_contextAccessor.HttpContext?.Items.TryGetValue(CorrelationIdKey, out var correlationId) == true)
        {
            return correlationId?.ToString() ?? string.Empty;
        }
        return string.Empty;
    }

    public void SetCorrelationId(string correlationId)
    {
        if (_contextAccessor.HttpContext != null)
        {
            _contextAccessor.HttpContext.Items[CorrelationIdKey] = correlationId;
        }
    }
}
