using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

using Amazon.VerifiedPermissions;
using Amazon.VerifiedPermissions.Model;
using Microsoft.Extensions.Options;

namespace MinimalApi.Services;

public class AvpUserRoleService : IUserRoleService
{
    private readonly IAmazonVerifiedPermissions _avpClient;
    private readonly Func<Task<AvpLookupService>> AvpLookupFactory;
    private readonly AvpConfig _avpConfig;

    private static Lazy<AvpLookupService> _avpLookupLazy;
    public static AvpLookupService AvpLookup => _avpLookupLazy.Value;

    public AvpUserRoleService(
        IAmazonVerifiedPermissions avpClient,
        Func<Task<AvpLookupService>> avpLookupFactory,
        IOptions<AvpConfig> avpConfigOptions)
    {
        _avpClient = avpClient;
        AvpLookupFactory = avpLookupFactory;
        _avpConfig = avpConfigOptions.Value;

        if (_avpLookupLazy == default)
            _avpLookupLazy = new Lazy<AvpLookupService>(() => avpLookupFactory().Result);
    }

    public async Task<UserRole> CreateUserRole(
        string principalId,
        string roleId,
        string condition)
    {
        var policyTemplateId = GetPolicyTemplateIdForRole(roleId);

        var response = await _avpClient.CreatePolicyAsync(
            new CreatePolicyRequest()
            {
                PolicyStoreId = _avpConfig.PolicyStoreId,
                Definition = new PolicyDefinition()
                {
                    TemplateLinked = new TemplateLinkedPolicyDefinition()
                    {
                        PolicyTemplateId = policyTemplateId,
                        Principal = AvpLogic.ToPrincipalEntity(principalId),
                        Resource = AvpLogic.ToResourceEntity(condition)
                    }
                },
                ClientToken = Guid.NewGuid().ToString()
            });

        return new UserRole()
        {
            Id = response.PolicyId,
            UserId = principalId,
            RoleId = roleId,
            CreatedAt = response.CreatedDate,
            ModifiedAt = response.LastUpdatedDate,
            Condition = $"{response.Resource.EntityType}:{response.Resource.EntityId}"
        };
    }

    public async Task<UserRole> GetUserRole(string principalId, string roleId, string condition)
    {
        var policyTemplateId = GetPolicyTemplateIdForRole(roleId);

        var response = await _avpClient.ListPoliciesAsync(
            new ListPoliciesRequest()
            {
                PolicyStoreId = _avpConfig.PolicyStoreId,
                Filter = new PolicyFilter()
                {
                    PolicyTemplateId = policyTemplateId,
                    PolicyType = PolicyType.TEMPLATE_LINKED,
                    Principal = AvpLogic.ToPrincipalReference(principalId),
                    Resource = AvpLogic.ToResourceReference(condition)
                }
            });

        var policy = response.Policies.FirstOrDefault();

        if (policy == default)
            return null;

        return new UserRole()
        {
            Id = policy.PolicyId,
            UserId = policy.Principal.EntityId,
            RoleId = AvpLookup.RoleIdsByPolicyTemplateId[policy.Definition.TemplateLinked.PolicyTemplateId],
            CreatedAt = policy.CreatedDate,
            ModifiedAt = policy.LastUpdatedDate
        };
    }

    public async Task<IEnumerable<UserRole>> GetUserRolesByUserId(string principalId)
    {
        var policies = await AvpLookup.GetPoliciesForPrincipal(principalId);

        return policies.Select(policyItem =>
            new UserRole()
            {
                Id = policyItem.PolicyId,
                UserId = principalId,
                RoleId = AvpLookup.RoleIdsByPolicyTemplateId[policyItem.PolicyTemplateId],
                Condition = policyItem.Condition,
                //Metadata = AvpLogic.InterpolateStatement(
                //    AvpLookup.PolicyTemplateStatementsByPolicyTemplateId[policyItem.PolicyTemplateId],
                //    $"MinimalApi::User::\"{principalId}\"",
                //    policyItem.Condition),
                CreatedAt = policyItem.CreatedAt,
                ModifiedAt = policyItem.ModifiedAt 
            });
    }

    public async Task<IEnumerable<UserRole>> GetUserRolesByRoleCondition(
        string roleId,
        string condition,
        string roleComparisonOperator = null)
    {
        List<string> roleIds;

        if (roleComparisonOperator == "BEGINS_WITH")
        {
            // hackily recapitulate dynamo BEGINS_WITH
            roleIds = AvpLookup.RoleIdsByPolicyTemplateId.Values.Where(r => r.StartsWith(roleId)).ToList();
        }
        else
        {
            roleIds = new List<string>{ roleId };
        }

        var userRoles = new List<UserRole>();

        foreach (var id in roleIds)
        {
            var policyTemplateId = GetPolicyTemplateIdForRole(id);

            var response = await _avpClient.ListPoliciesAsync(
                new ListPoliciesRequest()
                {
                    PolicyStoreId = _avpConfig.PolicyStoreId,
                    Filter = new PolicyFilter()
                    {
                        PolicyTemplateId = policyTemplateId,
                        Resource = AvpLogic.ToResourceReference(condition)
                    },
                    // TODO: pagination
                    MaxResults = 50
                });

            userRoles.AddRange(response.Policies.Select(policyItem => new UserRole()
            {
                Id = policyItem.PolicyId,
                UserId = policyItem.Principal.EntityId,
                RoleId = AvpLookup.RoleIdsByPolicyTemplateId[policyItem.Definition.TemplateLinked.PolicyTemplateId],
                Condition = $"{policyItem.Resource.EntityType}:{policyItem.Resource.EntityId}",
                CreatedAt = policyItem.CreatedDate,
                ModifiedAt = policyItem.LastUpdatedDate
            }));
        }

        return userRoles;
    }

    public async Task<IEnumerable<string>> GetUserRoleConditionValues(
        ClaimsPrincipal principal,
        string action,
        string conditionType)
    {
        if (!AvpLookup.PolicyTemplateIdsByAction.TryGetValue(action, out var policyTemplateIdsByAction))
            throw new Exception($"Unknown action '{action}'.");

        var policyTemplateIdsGrantingAction = AvpLookup.PolicyTemplateIdsByAction[action];

        // TODO: pagination

        var response = await _avpClient.ListPoliciesAsync(
            new ListPoliciesRequest()
            {
                PolicyStoreId = _avpConfig.PolicyStoreId,
                Filter = new PolicyFilter()
                {
                    Principal = principal.ToPrincipalReference()
                },
                // TODO: pagination
                MaxResults = 50
            });

        return response.Policies
            .Where(policyItem =>
                policyTemplateIdsGrantingAction.Contains(policyItem.Definition.TemplateLinked.PolicyTemplateId)
                && policyItem.Resource.EntityType == conditionType)
            .Select(policyItem => policyItem.Resource.EntityId);
    }

    public async Task DeleteUserRole(string principalId, string roleId, string condition)
    {
        var policyTemplateId = GetPolicyTemplateIdForRole(roleId);

        var response = await _avpClient.ListPoliciesAsync(
            new ListPoliciesRequest()
            {
                PolicyStoreId = _avpConfig.PolicyStoreId,
                Filter = new PolicyFilter()
                {
                    PolicyTemplateId = policyTemplateId,
                    Principal = AvpLogic.ToPrincipalReference(principalId),
                    Resource = AvpLogic.ToResourceReference(condition)
                }
            });

        if (!response.Policies.Any())
        {
            return;
        }

        if (response.Policies.Count > 1)
            throw new Exception("Unexpected roles encountered.");

        await _avpClient.DeletePolicyAsync(
            new DeletePolicyRequest()
            {
                PolicyStoreId = _avpConfig.PolicyStoreId,
                PolicyId = response.Policies[0].PolicyId
            });
    }

    private string GetPolicyTemplateIdForRole(string roleId)
    {
        if (!AvpLookup.PolicyTemplateIdsByRoleId.TryGetValue(
            roleId/*.Substring(roleId.LastIndexOf("::") + 2)*/, out var policyTemplateId))
        {
            throw new Exception("Invalid role.");
        }

        return policyTemplateId;
    }
}
