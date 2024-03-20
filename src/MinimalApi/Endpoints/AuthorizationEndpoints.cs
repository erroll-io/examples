using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace MinimalApi;

public static class AuthorizationEndpoints
{
    public static async Task<IResult> Authorize(
        [FromServices] IHttpContextAccessor contextAccessor,
        [FromServices] IAuthorizer authorizer,
        [FromBody] AuthorizationRequest request)
    {
        var result = authorizer.Authorize(
            request.Policy,
            request.Principal,
            request.Action,
            request.Resource);

        return Results.Ok(new AuthorizationResponse()
        {
            Result = result.ToString()
        });
    }

}