//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Security.Claims;
//using System.Threading.Tasks;
//
//namespace MinimalApi.Services;
//
//public interface IAuthClaimsService
//{
//    Task<IEnumerable<Claim>> GetUserClaims(ClaimsPrincipal principal);
//    Task<UserRole> CreateUserRole(string userId, string roleId, string condition);
//    Task<IEnumerable<UserRole>> GetUserRolesByCondition(
//        string roleId,
//        string condition,
//        string roleComparisonOperator = null);
//}
//
//public class AuthClaimsService : IAuthClaimsService
//{
//    private readonly IUserService _userService;
//    private readonly IRoleService _roleService;
//    private readonly IUserRoleService _userRoleService;
//
//    public AuthClaimsService(
//        IUserService userService,
//        IRoleService roleService,
//        IUserRoleService userRoleService)
//    {
//        _userService = userService;
//        _roleService = roleService;
//        _userRoleService = userRoleService;
//    }
//
//
//    public Task<UserRole> CreateUserRole(string userId, string roleId, string condition)
//    {
//        return _userRoleService.CreateUserRole(roleId, userId, condition);
//
//        //return _userRoleService.SaveUserRole(new UserRole()
//        //{
//        //    Id = Guid.NewGuid().ToString(),
//        //    UserId = userId,
//        //    RoleId = roleId,
//        //    Condition = condition,
//        //    CreatedAt = DateTime.UtcNow
//        //});
//    }
//
//    public Task<IEnumerable<UserRole>> GetUserRolesByCondition(
//        string roleId,
//        string condition,
//        string roleComparisonOperator = null)
//    {
//        return _userRoleService.GetUserRolesByRoleCondition(roleId, condition, roleComparisonOperator);
//    }
//}
//