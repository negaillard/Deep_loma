using Contracts.BindingModels.Authentication;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.LogicContracts.Authentication
{
	public interface ISessionService
	{
		Task<string> CreateSessionAsync(int userId, string username);
		Task<UserSession> GetSessionAsync(string sessionId);
		Task<(bool, string)> ValidateSessionAsync(string sessionId);
		Task<bool> DeleteSessionAsync(string sessionId);
	}
}
