//using Microsoft.AspNetCore.Authorization;
//
//namespace MinimalApi;
//
//internal class ProjectAuthorizeAttribute : AuthorizeAttribute
//{
//    const string POLICY_PREFIX = "MinimalApi.Project";
//
//    public ProjectAuthorizeAttribute(int age) => Age = age;
//
//    // Get or set the Age property by manipulating the underlying Policy property
//    public int Age
//    {
//        get
//        {
//            if (int.TryParse(Policy.Substring(POLICY_PREFIX.Length), out var age))
//            {
//                return age;
//            }
//            return default(int);
//        }
//        set
//        {
//            Policy = $"{POLICY_PREFIX}{value.ToString()}";
//        }
//    }
//}
//