using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Amazon.VerifiedPermissions;
using Amazon.VerifiedPermissions.Model;
using Microsoft.Extensions.Options;

namespace MinimalApi.Services;

public class AvpValueCache
{
    public Dictionary<string, string> RoleIdsByPolicyTemplateId { get; private set; }
    public Dictionary<string, string> PolicyTemplateIdsByRoleId { get; private set; }
    public Dictionary<string, List<string>> PolicyTemplateIdsByAction { get; private set; }
    public Dictionary<string, string> PolicyTemplateStatementsByPolicyTemplateId { get; private set; }
    public Dictionary<string, List<PolicyBrief>> PoliciesByPrincipalId { get; private set; }

    private readonly IAmazonVerifiedPermissions _avpClient;
    private readonly AvpConfig _avpConfig;

    private AvpValueCache(IAmazonVerifiedPermissions avpClient, AvpConfig avpConfig)
    {
        _avpClient = avpClient;
        _avpConfig = avpConfig;
    }

    public static async Task<AvpValueCache> Initialize(
        IAmazonVerifiedPermissions avpClient,
        IOptions<AvpConfig> avpConfigOptions)
    {
        var avpCache = new AvpValueCache(avpClient, avpConfigOptions.Value);

        // parse SSM params to build role/template lookup dicts
        avpCache.ParseConfigValues();

        // parse all policy templates to build action lookup dict
        await avpCache.ParsePolicyTemplateActions();

        return avpCache;
    }

    public async Task<IEnumerable<PolicyBrief>> GetPoliciesForPrincipal(string principalId)
    {
        if (PoliciesByPrincipalId == default)
            PoliciesByPrincipalId = new Dictionary<string, List<PolicyBrief>>();

        // TODO: implement proper caching behavior...as is, this will only run once
        if (!PoliciesByPrincipalId.ContainsKey(principalId))
        {
            PoliciesByPrincipalId[principalId] = new List<PolicyBrief>();

            // TODO: pagination

            var policiesResponse = await _avpClient.ListPoliciesAsync(
                new ListPoliciesRequest()
                {
                    PolicyStoreId = _avpConfig.PolicyStoreId,
                    Filter = new PolicyFilter()
                    {
                        PolicyType = PolicyType.TEMPLATE_LINKED,
                        Principal = AvpLogic.ToPrincipalReference(principalId)
                    }
                });

            foreach (var policyItem in policiesResponse.Policies)
            {
                PoliciesByPrincipalId[principalId].Add(
                    new PolicyBrief()
                    {
                        PolicyId = policyItem.PolicyId,
                        PolicyTemplateId = policyItem.Definition.TemplateLinked.PolicyTemplateId,
                        Condition = policyItem.Resource == default
                            ? string.Empty
                            : $"{policyItem.Resource.EntityType}::\"{policyItem.Resource.EntityId}\"",
                        CreatedAt = policyItem.CreatedDate,
                        ModifiedAt = policyItem.LastUpdatedDate,
                    });
            }
        }

        return PoliciesByPrincipalId[principalId];
    }

    private void ParseConfigValues()
    {
        RoleIdsByPolicyTemplateId = new Dictionary<string, string>();
        PolicyTemplateIdsByRoleId = new Dictionary<string, string>();
        PolicyTemplateStatementsByPolicyTemplateId = new Dictionary<string, string>();

        foreach (var policyTemplate in _avpConfig.PolicyTemplates)
        {
            RoleIdsByPolicyTemplateId[policyTemplate.TemplateId] = $"MinimalApi::Role::{policyTemplate.RoleName}";
            PolicyTemplateIdsByRoleId[$"MinimalApi::Role::{policyTemplate.RoleName}"] = policyTemplate.TemplateId;
            PolicyTemplateStatementsByPolicyTemplateId[policyTemplate.TemplateId] = policyTemplate.Statement;
        }
    }

    private async Task ParsePolicyTemplateActions()
    {
        PolicyTemplateIdsByAction = new Dictionary<string, List<string>>();

        // TODO: pagination; this logic breaks when >50 policies exist

        var policiesResponse = await _avpClient.ListPoliciesAsync(
            new ListPoliciesRequest()
            {
                PolicyStoreId = _avpConfig.PolicyStoreId,
                Filter = new PolicyFilter()
                {
                    PolicyType = PolicyType.TEMPLATE_LINKED
                }
            });

        if (policiesResponse.Policies.Any(policy => policy.Effect == PolicyEffect.Forbid))
        {
            throw new Exception("Unsupported 'Forbid' policy encountered.");
        }

        foreach (var policy in policiesResponse.Policies
            .Where(policy => policy.PolicyType == PolicyType.TEMPLATE_LINKED)
            .Where(policy => policy.Effect == PolicyEffect.Permit))
        {
            foreach (var action in policy.Actions)
            {
                var actionFqn = $"{action.ActionType}::{action.ActionId}";

                if (!PolicyTemplateIdsByAction.ContainsKey(actionFqn))
                    PolicyTemplateIdsByAction[actionFqn] = new List<string>();

                if (PolicyTemplateIdsByAction[actionFqn].Contains(policy.Definition.TemplateLinked.PolicyTemplateId))
                    continue;

                PolicyTemplateIdsByAction[actionFqn].Add(policy.Definition.TemplateLinked.PolicyTemplateId);
            }
        }
    }
}

public class PolicyBrief
{
    public string PolicyId { get; set; }
    public string PolicyTemplateId { get; set; }
    public string Condition { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ModifiedAt { get; set; }
}
