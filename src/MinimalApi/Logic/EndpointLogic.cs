using System;

using Microsoft.AspNetCore.Http;

using MinimalApi.Services;

namespace MinimalApi;

public static class EndpointResults
{
    public static IResult FromServiceResult<TResource, TResponse>(
        this ServiceResult<TResource> result,
        Func<TResource, TResponse> handler)
    {
        // TODO: consider result.Exception

        return result.AuthorizationResult.Succeeded
            ? result.IsSuccess 
                ? Results.Ok(handler(result.Result))
                : Error(result.ErrorMessage ?? "API failure.")
            : Results.Forbid();
    }

    public static IResult Error(string errorMessage)
    {
        return Results.Json(
            new ErrorResponse()
            {
                Error = errorMessage
            },
            statusCode: 500);
    }
}
