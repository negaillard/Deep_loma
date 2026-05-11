using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Storage;

public sealed class StorageContextFactory : IDesignTimeDbContextFactory<StorageContext>
{
	public StorageContext CreateDbContext(string[] args)
	{
		var cs =
			Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
			?? Environment.GetEnvironmentVariable("ConnectionStrings__Storage");

		if (string.IsNullOrWhiteSpace(cs))
		{
			cs =
				"Data Source=localhost\\SQLEXPRESS;Initial Catalog=DCP;Integrated Security=True;MultipleActiveResultSets=True;TrustServerCertificate=True";
		}

		var optionsBuilder = new DbContextOptionsBuilder<StorageContext>();
		optionsBuilder.UseSqlServer(cs);
		return new StorageContext(optionsBuilder.Options);
	}
}
