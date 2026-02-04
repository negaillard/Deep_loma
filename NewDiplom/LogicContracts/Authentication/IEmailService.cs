using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.LogicContracts.Authentication
{
	public interface IEmailService
	{
		Task<bool> SendVerificationCodeAsync(string email, string code);
	}
}
