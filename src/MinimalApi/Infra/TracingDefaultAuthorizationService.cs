using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Authorization;

public class TracingDefaultAuthorizationService : DefaultAuthorizationService
{
    private readonly ILogger _logger;
    private readonly MinimalApi.AuthConfig _authConfig;

    public TracingDefaultAuthorizationService(
        IAuthorizationPolicyProvider policyProvider,
        IAuthorizationHandlerProvider handlers,
        ILogger<TracingDefaultAuthorizationService> logger,
        IAuthorizationHandlerContextFactory contextFactory,
        IAuthorizationEvaluator evaluator,
        IOptions<AuthorizationOptions> options,
        IOptions<MinimalApi.AuthConfig> authConfigOptions)
            : base(policyProvider, handlers, logger, contextFactory, evaluator, options)
    {
        _logger = logger;
        _authConfig = authConfigOptions.Value;
    }

    public override async Task<AuthorizationResult> AuthorizeAsync(ClaimsPrincipal user, object? resource, IEnumerable<IAuthorizationRequirement> requirements)
    {
        var sw = new Stopwatch();

        sw.Start();
        var result = await base.AuthorizeAsync(user, resource, requirements);
        sw.Stop();

        _logger.LogTrace($"MinimalApi::Metric::{(_authConfig.DoUseAvp ? "AVP" : "")}{requirements.First().GetType().Name}EvalTimeMs: {sw.ElapsedMilliseconds}");

        return result;
    }
}
