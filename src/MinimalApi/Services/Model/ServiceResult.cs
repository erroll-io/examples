using System;

using Microsoft.AspNetCore.Authorization;

namespace MinimalApi.Services;

public class ServiceResult
{
    public AuthorizationResult AuthorizationResult { get; set; }
    public bool IsSuccess { get; set; }
    public Exception Exception { get; set; }
    public string ErrorMessage { get; set; }

    public static ServiceResult Success(AuthorizationResult authorizationResult = default)
    {
        return new ServiceResult()
        {
            IsSuccess = true,
            AuthorizationResult = authorizationResult ?? AuthorizationResult.Success(),
        };
    }

    public static ServiceResult Forbidden(
        AuthorizationResult authorizationResult = default,
        string errorMessage = default)
    {
        return new ServiceResult()
        {
            AuthorizationResult = authorizationResult ?? AuthorizationResult.Failed(),
            ErrorMessage = errorMessage
        };
    }

    public static ServiceResult Failure(string errorMessage = default)
    {
        return new ServiceResult()
        {
            ErrorMessage = errorMessage
        };
    }
}

public class ServiceResult<TResource> : ServiceResult
{
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
