using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

using MinimalApi.Services;

namespace MinimalApi;

public static class UsersEndpoints
{
    public static async Task<IResult> CreateUser(
        [FromServices] IUserService usersService,
        [FromBody] UserCreateRequest request)
    {
        if (request == default)
            return Results.BadRequest();
        if (string.IsNullOrEmpty(request.Email))
            return Results.BadRequest(nameof(request.Email));

        var user = await usersService.CreateUser(
            new UserCreateParams()
            {
                Email = request.Email,
                Language = request.Language,
                Timezone = request.Timezone,
                Metadata = request.Metadata
            });

        return Results.Ok(user.ToResponse());
    }

    public static async Task<IResult> GetUser(
        [FromServices] IUserService usersService,
        string id)
    {
        if (string.IsNullOrEmpty(id))
            return Results.BadRequest(nameof(id));

        var user = await usersService.GetUser(id);

        return Results.Ok(user.ToResponse());
    }

    [Authorize]
    public static async Task<IResult> GetCurrentUser(
        [FromServices] IUserService usersService,
        [FromServices] IOptionsSnapshot<AuthConfig> authConfigSnapshot,
        [FromServices] IHttpContextAccessor httpContextAccessor)
    {
        var doUseAvp = authConfigSnapshot.Value.DoUseAvp;

        var userResult = await usersService.GetCurrentUser(httpContextAccessor.HttpContext.User);

        if (!userResult.AuthorizationResult.Succeeded)
            return Results.Forbid();

        return Results.Ok(userResult.Result.ToResponse(
            httpContextAccessor.HttpContext.User == default
                ? default
                : httpContextAccessor.HttpContext.User.Claims
                    .Where(claim => claim.Type == "permission")
                    .Select(claim => KeyValuePair.Create(claim.Type, claim.Value))));
    }

    public static async Task<IResult> UpdateUser(
        [FromServices] IUserService usersService,
        string id,
        [FromBody] UserCreateRequest request)
    {
        if (string.IsNullOrEmpty(id))
            return Results.BadRequest();
        if (request == default)
            return Results.BadRequest();

        await usersService.UpdateUser(
            id,
            new UserCreateParams()
            {
                Language = request.Language,
                Timezone = request.Timezone
            });
        
        return Results.NoContent();
    }

    public static async Task<IResult> UpdateCurrentUser(
        [FromServices] IHttpContextAccessor httpContextAccessor,
        [FromServices] IUserService usersService,
        [FromBody] UserCreateRequest request)
    {
        await usersService.UpdateCurrentUser(
            httpContextAccessor.HttpContext.User, 
            new UserCreateParams()
            {
                Language = request.Language,
                Timezone = request.Timezone
            });

        return Results.NoContent();
    }

    private static UserResponse ToResponse(this User user, IEnumerable<KeyValuePair<string, string>> claims = default)
    {
        return new UserResponse()
        {
            Id = user.Id,
            PrincipalId = user.PrincipalId,
            Language = user.Language,
            Timezone = user.Timezone,
            Metadata = user.Metadata,
            CreatedAt = user.CreatedAt,
            ModifiedAt = user.ModifiedAt,
            Claims = claims
        };
    }
}
