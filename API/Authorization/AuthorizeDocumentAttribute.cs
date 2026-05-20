using Contracts.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Models.Enums;

namespace API.Authorization
{
	[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
	public class AuthorizeDocumentAttribute : Attribute, IAuthorizationFilter
	{
		public void OnAuthorization(AuthorizationFilterContext context)
		{
			var user = context.HttpContext.Items["User"] as UserViewModel;

			if (user == null ||
				(user.SystemRole != SystemRole.SystemAdmin && user.SystemRole != SystemRole.DocumentManager))
			{
				context.Result = new StatusCodeResult(403);
			}
		}
	}
}
