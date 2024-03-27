using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using MinimalApi.Services;

namespace MinimalApi;

public static class DataEndpoints
{
    [Authorize]
    // POST /data
    public static async Task<IResult> CreateUserData(
        [FromServices] IHttpContextAccessor httpContextAccessor,
        [FromServices] IUserService userService,
        [FromServices] IDataService dataService,
        [FromBody] DataRecordRequest request)
    {
        if (request == default)
            return Results.BadRequest();
        if (string.IsNullOrEmpty(request.DataTypeId))
            return Results.BadRequest(nameof(request.DataTypeId));
        if (string.IsNullOrEmpty(request.FileName))
            return Results.BadRequest(nameof(request.FileName));
        if (!request.Size.HasValue)
            return Results.BadRequest(nameof(request.Size));
            
        var userDataResult = await dataService.CreateDataRecord(
            httpContextAccessor.HttpContext.User,
            new DataRecordParams()
            {
                DataTypeId = request.DataTypeId,
                FileName = request.FileName,
                Size = request.Size.Value,
                Metadata = request.Metadata
            });

        return userDataResult.FromServiceResult(userData =>
            new DataRecordResponse()
            {
                Id = userDataResult.Result.Id
            });
    }

    [Authorize]
    // POST /projects/{projectId}/data
    public static async Task<IResult> CreateProjectData(
        [FromServices] IHttpContextAccessor httpContextAccessor,
        [FromServices] IDataService dataService,
        string projectId,
        [FromBody] DataRecordRequest request)
    {
        if (string.IsNullOrEmpty(projectId))
            return Results.BadRequest();
        if (request == default)
            return Results.BadRequest();
        if (string.IsNullOrEmpty(request.DataTypeId))
            return Results.BadRequest(nameof(request.DataTypeId));
        if (string.IsNullOrEmpty(request.FileName))
            return Results.BadRequest(nameof(request.FileName));
        if (!request.Size.HasValue)
            return Results.BadRequest(nameof(request.Size));

        var projectDataResult = await dataService.CreateProjectData(
            httpContextAccessor.HttpContext.User,
            projectId,
            new DataRecordParams()
            {
                DataTypeId = request.DataTypeId,
                FileName = request.FileName,
                Size = request.Size.Value,
                Metadata = request.Metadata
            });

        return projectDataResult.FromServiceResult(projectData =>
            new DataRecordResponse()
            {
                Id = projectData.Id
            });
    }

    [Authorize]
    // PUT /data/{dataRecordId}
    public static async Task<IResult> FinalizeUpload(
        [FromServices] IHttpContextAccessor httpContextAccessor,
        [FromServices] IDataService dataService,
        string dataRecordId,
        [FromBody] DataFinalizeUploadRequest request)
    {
        if (string.IsNullOrEmpty(dataRecordId))
            return Results.BadRequest(nameof(dataRecordId));
        if (request == default)
            return Results.BadRequest();
        if (string.IsNullOrEmpty(request.UploadId))
            return Results.BadRequest(nameof(request.UploadId));
        if (request.Parts == default || !request.Parts.Any())
            return Results.BadRequest(nameof(request.Parts));

        if (!httpContextAccessor.HttpContext.User.HasPermission(
            "MinimalApi::Action::UpdateData",
            $"DataRecord::{dataRecordId}"))
        {
            return Results.Forbid();
        }

        var dataRecordResult = await dataService.FinalizeDataUpload(
            httpContextAccessor.HttpContext.User,
            dataRecordId,
            request.Parts);

        return dataRecordResult.FromServiceResult(dataRecord =>
            Results.NoContent());
    }

    [Authorize]
    // GET /data
    public static async Task<IResult> GetUserData(
        [FromServices] IHttpContextAccessor httpContextAccessor,
        [FromServices] IUserService userService,
        [FromServices] IDataService dataService)
    {
        var dataResult = await dataService.GetUserData(httpContextAccessor.HttpContext.User);

        return dataResult.FromServiceResult(data =>
            new DataResponse()
            {
                Data = data.Select(ToResponse)
            });
    }

    [Authorize]
    // GET /projects/{projectId}}/data
    public static async Task<IResult> GetProjectData(
        [FromServices] IHttpContextAccessor httpContextAccessor,
        [FromServices] IUserService userService,
        [FromServices] IDataService dataService,
        string projectId)
    {
        if (string.IsNullOrEmpty(projectId))
            return Results.BadRequest(nameof(projectId));

        var dataResult = await dataService.GetProjectData(httpContextAccessor.HttpContext.User, projectId);

        return dataResult.FromServiceResult(data =>
            new DataResponse()
            {
                Data = data.Select(ToResponse)
            });
    }

    public static DataRecordResponse ToResponse(this DataRecord dataRecord)
    {
        return new DataRecordResponse()
        {
            Id = dataRecord.Id,
            DataTypeId = dataRecord.DataTypeId,
            FileName = dataRecord.FileName,
            Location = dataRecord.Location,
            Size = dataRecord.Size,
            Metadata = dataRecord.Metadata,
            CreatedAt = dataRecord.CreatedAt,
            ModifiedAt = dataRecord.ModifiedAt
        };
    }
}
