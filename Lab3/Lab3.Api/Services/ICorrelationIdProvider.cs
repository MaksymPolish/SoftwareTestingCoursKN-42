namespace Lab3.Api.Services;

/// Service for managing correlation IDs across request lifecycle
public interface ICorrelationIdProvider
{
    /// Generates a new correlation ID for the current request
    string GenerateCorrelationId();

    /// Gets the correlation ID for the current request
    string GetCorrelationId();

    /// Sets the correlation ID for the current request
    void SetCorrelationId(string correlationId);
}
