using fuquizlearn_api.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace fuquizlearn_api.Authorization
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class AuthorizeAttribute : Attribute, IAuthorizationFilter
    {
        private readonly IList<Role> _roles;

        public AuthorizeAttribute(params Role[] roles)
        {
            _roles = roles ?? new Role[] { };
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            // skip authorization if action is decorated with [AllowAnonymous] attribute
            var allowAnonymous = context.ActionDescriptor.EndpointMetadata.OfType<AllowAnonymousAttribute>().Any();
            if (allowAnonymous)
                return;

            // authorization
            var account = (Account)context.HttpContext.Items["Account"];

            if (account != null)

            {
                if (!account.IsVerified)
                {

                    context.Result = new JsonResult(new { message = "Account is not verified" }) { StatusCode = StatusCodes.Status401Unauthorized };
                }
                else if ((_roles.Any() && !_roles.Contains(account.Role)))
                {
                    context.Result = new JsonResult(new { message = "Insufficient permission" }) { StatusCode = StatusCodes.Status401Unauthorized };
                }
                // not logged in or role not authorized
            }
            else
            {

                context.Result = new JsonResult(new { message = "Unauthorized" }) { StatusCode = StatusCodes.Status401Unauthorized };
            }
        }
    }
}
