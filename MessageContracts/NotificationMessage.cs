using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MessageContracts
{
	public record NotificationMessage(
		string RecipientEmail,
		string RecipientName,
		string DocumentTitle,
		string RequestedByName,
		DateTime RequestedAt);

}
