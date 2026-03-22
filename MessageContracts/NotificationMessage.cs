using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MessageContracts
{
	public record NotificationMessage(
		int UserId,
		string Title,
		DateTime RequestedAt);
}
