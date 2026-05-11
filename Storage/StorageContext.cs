using Microsoft.EntityFrameworkCore;
using Storage.Models;

namespace Storage
{
	public class StorageContext : DbContext
	{
		public StorageContext()
		{
		}

		public StorageContext(DbContextOptions<StorageContext> options) : base(options)
		{
		}

		public virtual DbSet<User> Users { set; get; }
		public virtual DbSet<Role> Roles { set; get; }
		public virtual DbSet<Document> Documents { set; get; }
		public virtual DbSet<DocumentUser> DocumentUsers { set; get; }
		public virtual DbSet<Signature> Signatures { set; get; }
		public virtual DbSet<Certificate> Certificates { set; get; }
	}
}
