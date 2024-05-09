using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace MinimalApi.Services;

public interface IUserRoleService
{
    Task<UserRole> CreateUserRole(string principalId, string roleId, string condition);
    Task<UserRole> GetUserRole(string principalId, string roleId, string condition);
    Task<IEnumerable<UserRole>> GetUserRolesByUserId(string userId);
    Task<IEnumerable<UserRole>> GetUserRolesByRoleCondition(
        string roleId,
        string condition,
        string roleComparisonOperator = null);
    Task<IEnumerable<string>> GetUserRoleConditionValues(ClaimsPrincipal principal, string action, string conditionType);
    Task DeleteUserRole(string principalId, string roleId, string condition);
}
