using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models
{
	public interface IUserModel : IId
	{
		string Fullname {  get; }
		string Login { get; }
		string Email { get; }
		int CertificateId { get; }
		int RoleId { get; }
		SystemRole SystemRole { get; }
		DateTime Created { get; }
		bool IsActive { get; set; }
	}
}
