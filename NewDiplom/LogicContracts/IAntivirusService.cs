using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.LogicContracts
{
	public interface IAntivirusService
	{
		Task<bool> IsFileCleanAsync(Stream stream);
	}
}
