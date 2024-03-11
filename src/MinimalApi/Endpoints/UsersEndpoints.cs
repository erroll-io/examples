using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using MinimalApi.Services;

namespace MinimalApi;

public static class UsersEndpoints
{
    public static async Task<IResult> CreateUser(
        [FromServices] IUsersService usersService,
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
        [FromServices] IUsersService usersService,
        string id)
    {
        if (string.IsNullOrEmpty(id))
            return Results.BadRequest(nameof(id));

        var user = await usersService.GetUser(id);

        return Results.Ok(user.ToResponse());
    }

    public static async Task<IResult> GetCurrentUser(
        [FromServices] IUsersService usersService,
        [FromServices] IHttpContextAccessor httpContextAccessor,
        // TODO: temporary until we wire up authn
        [FromQuery] string sub = default,
        [FromQuery] string username = default)
    {
        var user = await usersService.GetCurrentUser(
            ClaimsPrincipalLogic.GetFakeUser(username, sub));

        return Results.Ok(user.ToResponse());
    }

    public static async Task<IResult> UpdateUser(
        [FromServices] IUsersService usersService,
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
        [FromServices] IUsersService usersService,
        [FromBody] UserCreateRequest request,
        // TODO: temporary until we wire up authn
        [FromQuery] string sub = default,
        [FromQuery] string username = default)
    {
        var user = ClaimsPrincipalLogic.GetFakeUser(username, sub);

        await usersService.UpdateCurrentUser(
            user, 
            new UserCreateParams()
            {
                Language = request.Language,
                Timezone = request.Timezone
            });

        return Results.NoContent();
    }

    private static UserResponse ToResponse(this User user)
    {
        return new UserResponse()
        {
            Id = user.Id,
            Language = user.Language,
            Timezone = user.Timezone,
            Metadata = user.Metadata,
            CreatedAt = user.CreatedAt,
            ModifiedAt = user.ModifiedAt
        };
    }
}
