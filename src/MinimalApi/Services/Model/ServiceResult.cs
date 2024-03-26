using System;

using Microsoft.AspNetCore.Authorization;

namespace MinimalApi.Services;

public class ServiceResult<TResource>
{
    public AuthorizationResult AuthorizationResult { get; set; }
    public bool IsSuccess { get; set; }
    public Exception Exception { get; set; }
    public string ErrorMessage { get; set; }
    public TResource Result { get; set; }

    private ServiceResult()
    {
    }

    public static ServiceResult<TResource> Success(TResource result, AuthorizationResult authorizationResult = default)
    {
        return new ServiceResult<TResource>()
        {
            IsSuccess = true,
            AuthorizationResult = authorizationResult ?? AuthorizationResult.Success(),
            Result = result
        };
    }

    public static ServiceResult<TResource> Forbidden(
        AuthorizationResult authorizationResult = default,
        string errorMessage = default)
    {
        return new ServiceResult<TResource>()
        {
            AuthorizationResult = authorizationResult ?? AuthorizationResult.Failed(),
            ErrorMessage = errorMessage
        };
    }

    public static ServiceResult<TResource> Failure(string errorMessage = default)
    {
        return new ServiceResult<TResource>()
        {
            ErrorMessage = errorMessage
        };
    }
}
