using Contracts.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Models;

namespace API.Authorization
{
	[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
	public class AuthorizeSignerAttribute : Attribute, IAuthorizationFilter
	{
		public void OnAuthorization(AuthorizationFilterContext context)
		{
			var user = context.HttpContext.Items["User"] as UserViewModel;

			if (user == null ||
				(user.SystemRole != SystemRole.SystemAdmin &&
				 user.SystemRole != SystemRole.DocumentManager &&
				 user.SystemRole != SystemRole.Signer))
			{
				context.Result = new ForbidResult(); // 403
			}
		}
	}
}


