using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.ViewModels
{
	public class PagedResult<T>
	{
		public List<T> Items { get; set; } = [];
		public int TotalCount { get; set; }
		public int PageNumber { get; set; }
		public int PageSize { get; set; }
		public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
		public bool HasPrevious => PageNumber > 1;
		public bool HasNext => PageNumber < TotalPages;
	}
}
