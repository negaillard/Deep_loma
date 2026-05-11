using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models
{
	public enum AppType
	{
		SIGNER_APP,
		DOCUMENT_APP,
		ADMIN_APP,
		/// <summary>Объединённое демо-приложение (вход по правам пользователя).</summary>
		DEMO_APP
	}
}
