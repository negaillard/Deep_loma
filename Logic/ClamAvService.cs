using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Contracts.LogicContracts;
using nClam;
using Microsoft.Extensions.Options;

namespace Logic
{
	public class ClamAvService : IAntivirusService
	{
		private readonly ClamClient _clam;

		public ClamAvService(IOptions<AntivirusOptions> options)
		{
			var cfg = options.Value;
			_clam = new ClamClient(cfg.Host, cfg.Port);
		}

		public async Task<bool> IsFileCleanAsync(Stream stream)
		{
			var result = await _clam.SendAndScanFileAsync(stream);

			return result.Result == ClamScanResults.Clean;
		}
	}

	public class AntivirusOptions
	{
		public string Host { get; set; } = "localhost";
		public int Port { get; set; } = 3310;
	}
}
