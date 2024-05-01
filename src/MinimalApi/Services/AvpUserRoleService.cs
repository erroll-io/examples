using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using Amazon.VerifiedPermissions;
using Amazon.VerifiedPermissions.Model;
using Microsoft.Extensions.Options;

namespace MinimalApi.Services;

public class AvpUserRoleService : IUserRoleService
{
    private readonly IAmazonVerifiedPermissions _avpClient;
    private readonly AvpConfig _avpConfig;

    private static Lazy<Dictionary<string, string>> _roleIdsByPolicyTemplateIdLazy;
    private static Lazy<Dictionary<string, string>> _policyTemplateIdsByRoleIdLazy;
    private static Lazy<Dictionary<string, List<string>>> _policyTemplateIdsByActionLazy;
    private static Dictionary<string, string> _roleIdsByPolicyTemplateId => _roleIdsByPolicyTemplateIdLazy.Value;
    private static Dictionary<string, string> _policyTemplateIdsByRoleId => _policyTemplateIdsByRoleIdLazy.Value;
    private static Dictionary<string, List<string>> _policyTemplateIdsByAction => _policyTemplateIdsByActionLazy.Value;

    public AvpUserRoleService(
        IAmazonVerifiedPermissions avpClient,
        IOptions<AvpConfig> avpConfigOptions)
    {
        _avpClient = avpClient;
        _avpConfig = avpConfigOptions.Value;

        _roleIdsByPolicyTemplateIdLazy = new Lazy<Dictionary<string, string>>(
            () => _avpConfig.RoleTemplates.ToDictionary(kv => kv.Value, kv => $"MinimalApi::Role::{kv.Key}"),
            true);
        _policyTemplateIdsByRoleIdLazy = new Lazy<Dictionary<string, string>>(
            () => _avpConfig.RoleTemplates,
            true);
        _policyTemplateIdsByActionLazy = new Lazy<Dictionary<string, List<string>>>(
            () => ParsePolicyTemplateActions().Result,
            true);
    }

    public async Task<UserRole> CreateUserRole(
        string principalId,
        string roleId,
        string condition)
    {
        var policyTemplateId = GetPolicyTemplateIdForRole(roleId);
        var (resourceType, resourceId) = SplitCondition(condition);

        var response = await _avpClient.CreatePolicyAsync(
            new CreatePolicyRequest()
            {
                PolicyStoreId = _avpConfig.PolicyStoreId,
                Definition = new PolicyDefinition()
                {
                    TemplateLinked = new TemplateLinkedPolicyDefinition()
                    {
                        PolicyTemplateId = policyTemplateId,
                        Principal = new EntityIdentifier()
                        {
                            EntityType = "MinimalApi::User",
                            EntityId = principalId
                        },
                        Resource = new EntityIdentifier()
                        {
                            EntityType = resourceType,
                            //EntityType = $"MinimalApi::{resourceType}",
                            EntityId = resourceId
                        }
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
        var (resourceType, resourceId) = SplitCondition(condition);

        var response = await _avpClient.ListPoliciesAsync(
            new ListPoliciesRequest()
            {
                PolicyStoreId = _avpConfig.PolicyStoreId,
                Filter = new PolicyFilter()
                {
                    PolicyTemplateId = policyTemplateId,
                    Principal = new EntityReference()
                    {
                        Identifier = new EntityIdentifier()
                        {
                            EntityType = "MinimalApi::User",
                            EntityId = principalId
                        }
                    },
                    Resource = new EntityReference()
                    {
                        Identifier = new EntityIdentifier()
                        {
                            EntityType = resourceType,
                            //EntityType = $"MinimalApi::{resourceType}",
                            EntityId = resourceId
                        }
                    }
                }
            });

        var policy = response.Policies.FirstOrDefault();

        if (policy == default)
            return null;

        return new UserRole()
        {
            Id = policy.PolicyId,
            UserId = policy.Principal.EntityId,
            RoleId = _roleIdsByPolicyTemplateId[policy.Definition.TemplateLinked.PolicyTemplateId],
            CreatedAt = policy.CreatedDate,
            ModifiedAt = policy.LastUpdatedDate
        };
    }

    public async Task<IEnumerable<UserRole>> GetUserRolesByUserId(string principalId)
    {
        var response = await _avpClient.ListPoliciesAsync(
            new ListPoliciesRequest()
            {
                PolicyStoreId = _avpConfig.PolicyStoreId,
                Filter = new PolicyFilter()
                {
                    Principal = new EntityReference()
                    {
                        Identifier = new EntityIdentifier()
                        {
                            EntityType = "MinimalApi::User",
                            EntityId = principalId
                        }
                    }
                }
            });

        var foo = _roleIdsByPolicyTemplateId.ToList();

        return response.Policies.Select(policyItem => new UserRole()
        {
            Id = policyItem.PolicyId,
            UserId = policyItem.Principal.EntityId,
            RoleId = _roleIdsByPolicyTemplateId[policyItem.Definition.TemplateLinked.PolicyTemplateId],
            Condition = $"{policyItem.Resource.EntityType}:{policyItem.Resource.EntityId}",
            CreatedAt = policyItem.CreatedDate,
            ModifiedAt = policyItem.LastUpdatedDate
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
            roleIds = _roleIdsByPolicyTemplateId.Values.Where(r => r.StartsWith(roleId)).ToList();
        }
        else
        {
            roleIds = new List<string>{ roleId };
        }

        var userRoles = new List<UserRole>();

        foreach (var id in roleIds)
        {
            var policyTemplateId = GetPolicyTemplateIdForRole(id);
            var (resourceType, resourceId) = SplitCondition(condition);

            var response = await _avpClient.ListPoliciesAsync(
                new ListPoliciesRequest()
                {
                    PolicyStoreId = _avpConfig.PolicyStoreId,
                    Filter = new PolicyFilter()
                    {
                        PolicyTemplateId = policyTemplateId,
                        Resource = new EntityReference()
                        {
                            Identifier = new EntityIdentifier()
                            {
                                EntityType = resourceType,
                                EntityId = resourceId
                            }
                        }
                    }
                });

            userRoles.AddRange(response.Policies.Select(policyItem => new UserRole()
            {
                Id = policyItem.PolicyId,
                UserId = policyItem.Principal.EntityId,
                RoleId = _roleIdsByPolicyTemplateId[policyItem.Definition.TemplateLinked.PolicyTemplateId],
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
        //action = action.TrimStart("MinimalApi::");

        if (!_policyTemplateIdsByAction.TryGetValue(action, out var policyTemplateIdsByAction))
            throw new Exception($"Unknown action '{action}'.");

        var policyTemplateIdsGrantingAction = _policyTemplateIdsByAction[action];

        // TODO: pagination

        var response = await _avpClient.ListPoliciesAsync(
            new ListPoliciesRequest()
            {
                PolicyStoreId = _avpConfig.PolicyStoreId,
                Filter = new PolicyFilter()
                {
                    Principal = new EntityReference()
                    {
                        Identifier = new EntityIdentifier()
                        {
                            EntityType = "MinimalApi::User",
                            EntityId = principal.GetPrincipalIdentity()
                        }
                    }
                }
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
        var (resourceType, resourceId) = SplitCondition(condition);

        var response = await _avpClient.ListPoliciesAsync(
            new ListPoliciesRequest()
            {
                PolicyStoreId = _avpConfig.PolicyStoreId,
                Filter = new PolicyFilter()
                {
                    PolicyTemplateId = policyTemplateId,
                    Principal = new EntityReference()
                    {
                        Identifier = new EntityIdentifier()
                        {
                            EntityType = "User",
                            EntityId = principalId
                        }
                    },
                    Resource = new EntityReference()
                    {
                        Identifier = new EntityIdentifier()
                        {
                            EntityType = resourceType,
                            EntityId = resourceId
                        }
                    }
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
        if (!_policyTemplateIdsByRoleId.TryGetValue(
            roleId.Substring(roleId.LastIndexOf("::") + 2), out var policyTemplateId))
        {
            throw new Exception("Invalid role.");
        }

        return policyTemplateId;
    }

    internal static (string, string) SplitCondition(string condition)
    {
        var match = Regex.Match(condition, @"\w+(:)\w+");

        if (!match.Success)
            throw new Exception("Invalid condition.");

        return (condition.Substring(0, match.Groups[1].Index), condition.Substring(match.Groups[1].Index + 1));
    }

    private async Task<Dictionary<string, List<string>>> ParsePolicyTemplateActions()
    {
        var getPolicyTemplateTasks = _policyTemplateIdsByRoleId.Values.Select(templateId =>
            _avpClient.GetPolicyTemplateAsync(
                new GetPolicyTemplateRequest()
                {
                    PolicyStoreId = _avpConfig.PolicyStoreId,
                    PolicyTemplateId = templateId
                }));

        var policyTemplates = await Task.WhenAll(getPolicyTemplateTasks);

        var permits = new Regex(@"permit\s?\((.*)\)", RegexOptions.Compiled | RegexOptions.Singleline);
        var actions = new Regex(@"action in \[(.*)\]", RegexOptions.Compiled | RegexOptions.Singleline);

        var dict = new Dictionary<string, List<string>>();

        foreach (var template in policyTemplates)
        {
            var permitsMatch = permits.Match(template.Statement);

            if (!permitsMatch.Success)
                continue;
            
            var actionsMatch = actions.Match(permitsMatch.Groups[1].Value);

            if (!actionsMatch.Success)
                continue;
            
            foreach (var action in actionsMatch.Groups[1].Value.Split(',')
                .Select(p => p.Trim().Replace("\"", "")))
            {
                if (!dict.ContainsKey(action))
                    dict[action] = new List<string>();

                dict[action].Add(template.PolicyTemplateId);
            }
        }

        return dict;
    }
}
