using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.LogicContracts.Authentication
{
	public interface ICodeVerificationLogic
	{
		string GenerateCode();
		Task<(bool success, string message)> SendCodeAsync(string email);
		Task<(bool success, string message)> VerifyCodeAsync(string email, string code);
	}
}
