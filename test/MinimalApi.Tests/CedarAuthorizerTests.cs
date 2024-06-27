using System.Collections.Generic;

namespace MinimalApi.Tests;

public class CedarAuthorizerTests
{
    [Fact]
    public void CanAllow()
    {
        var policy = "permit(principal == User::\"alice\", action == Action::\"view\", resource == File::\"93\");";
        var principal = "User::\"alice\"";
        var action = "Action::\"view\"";
        var resource = "File::\"93\"";

        var result = new CedarAuthorizer().Authorize(policy, principal, action, resource, "", "");

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void CanDeny()
    {
        var policy = "permit(principal == User::\"alice\", action == Action::\"view\", resource == File::\"93\");";
        var principal = "User::\"bob\"";
        var action = "Action::\"view\"";
        var resource = "File::\"93\"";

        var result = new CedarAuthorizer().Authorize(policy, principal, action, resource, "", "");

        Assert.False(result.Succeeded);
    }

    [Fact]
    public void CanAllowWithMultiple()
    {
        var policyOne = "permit(principal in User::\"alice\", action in [Action::\"update\", Action::\"delete\"], resource == Photo::\"peppers.jpg\") when { context.mfa_authenticated == true && context.request_client_ip == \"42.42.42.42\" };";
        var policyTwo = "permit(principal == User::\"alice\", action == Action::\"view\", resource == File::\"93\");";
        var policyThree = "permit(principal == User::\"alice\", action == Action::\"view\", resource == File::\"95\");";
        var principal = "User::\"alice\"";
        var action = "Action::\"view\"";
        var resource = "File::\"93\"";

        var result = new CedarAuthorizer().Authorize(
            new List<CedarPolicy>()
            {
                new CedarPolicy("23", policyOne),
                new CedarPolicy("42", policyTwo),
                new CedarPolicy("86", policyThree),
            },
            principal,
            action,
            resource,
            "",
            "");

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void CanAllowWithContext()
    {
        var policy = "permit(principal in User::\"Bob\", action in [Action::\"update\", Action::\"delete\"], resource == Photo::\"peppers.jpg\") when { context.mfa_authenticated == true && context.request_client_ip == \"42.42.42.42\" };";
        var principal = "User::\"Bob\"";
        var action = "Action::\"update\"";
        var resource = "Photo::\"peppers.jpg\"";
        var context = "{\"mfa_authenticated\": true, \"request_client_ip\": \"42.42.42.42\", \"oidc_scope\": \"profile\" }";

        var result = new CedarAuthorizer().Authorize(policy, principal, action, resource, context, "");

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void CanDenyWithContext()
    {
        var policy = "permit( principal in User::\"Bob\", action in [Action::\"update\", Action::\"delete\"], resource == Photo::\"peppers.jpg\") when { context.mfa_authenticated == true && context.request_client_ip == \"42.42.42.42\" };";
        var principal = "User::\"Bob\"";
        var action = "Action::\"update\"";
        var resource = "Photo::\"peppers.jpg\"";
        var context = "{\"mfa_authenticated\": true, \"request_client_ip\": \"23.23.23.23\", \"oidc_scope\": \"profile\" }";

        var result = new CedarAuthorizer().Authorize(policy, principal, action, resource, context, "");

        Assert.False(result.Succeeded);
    }

    [Fact]
    public void CanAllowRoleWithEntities()
    {
        var policy = "permit(principal in Role::\"photoJudges\", action == Action::\"view\", resource == Photo::\"peppers.jpg\");";
        var principal = "User::\"Bob\"";
        var action = "Action::\"view\"";
        var resource = "Photo::\"peppers.jpg\"";
        var entities = "[ { \"uid\": { \"type\": \"User\", \"id\": \"Bob\" }, \"attrs\": {}, \"parents\": [ { \"type\": \"Role\", \"id\": \"photoJudges\" }, { \"type\": \"Role\", \"id\": \"juniorPhotoJudges\" } ] }, { \"uid\": { \"type\": \"Role\", \"id\": \"photoJudges\" }, \"attrs\": {}, \"parents\": [] }, { \"uid\": { \"type\": \"Role\", \"id\": \"juniorPhotoJudges\" }, \"attrs\": {}, \"parents\": [] } ]";

        var result = new CedarAuthorizer().Authorize(policy, principal, action, resource, "", entities);

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void CanDenyRoleWithEntities()
    {
        var policy = "permit(principal in Role::\"photoJudges\", action == Action::\"view\", resource == Photo::\"peppers.jpg\");";
        var principal = "User::\"Bob\"";
        var action = "Action::\"view\"";
        var resource = "Photo::\"peppers.jpg\"";
        var entities = "[ { \"uid\": { \"type\": \"User\", \"id\": \"Bob\" }, \"attrs\": {}, \"parents\": [ { \"type\": \"Role\", \"id\": \"photoSubmitters\" }, { \"type\": \"Role\", \"id\": \"juniorPhotoSubmitters\" } ] }, { \"uid\": { \"type\": \"Role\", \"id\": \"photoJudges\" }, \"attrs\": {}, \"parents\": [] }, { \"uid\": { \"type\": \"Role\", \"id\": \"juniorPhotoJudges\" }, \"attrs\": {}, \"parents\": [] } ]";

        var result = new CedarAuthorizer().Authorize(policy, principal, action, resource, "", entities);

        Assert.False(result.Succeeded);
    }
}
