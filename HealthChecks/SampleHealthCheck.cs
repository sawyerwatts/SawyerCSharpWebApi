using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace SawyerWebApiCtlrs.HealthChecks;

public class SampleHealthCheck : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(HealthCheckResult.Healthy());
    }
}
