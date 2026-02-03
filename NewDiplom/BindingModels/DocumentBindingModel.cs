using Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.BindingModels
{
	public class DocumentBindingModel : IDocumentModel
	{
		public string Title { get; set; } = string.Empty;

		public string Description { get; set; } = string.Empty;

		public DateTime CreatedAt {  get; set; }

		public int CreatedByUserId { get; set; }

		public int Id {  get; set; }
	}
}
