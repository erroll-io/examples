using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MinimalApi.Services;

namespace MinimalApi;

public static class TestEndpoints
{
    [Authorize]
    public static async Task<IResult> CompareAuthorizationStrategies(
        [FromServices] IHttpContextAccessor httpContextAccessor,
        [FromServices] IAuthorizationService authorizationService,
        [FromServices] CedarComparisonService comparisonService,
        [FromQuery] int? executionCount)
    {
        var authorizationResult = await authorizationService.AuthorizeAsync(
            httpContextAccessor.HttpContext.User,
            new OperationRequirement("MinimalApi::Action::\"ExecuteTests\""));

        if (!authorizationResult.Succeeded)
            return Results.Forbid();

        var results = await comparisonService.Compare(executionCount);

        return Results.Ok(
            new AuthorizationComparisonResponse
            {
                Results = results.ToArray()
            });
    }
}

public class AuthorizationComparisonResponse
{
    public AuthorizationComparisonResult[] Results { get; set; }
}
