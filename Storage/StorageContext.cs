using Microsoft.EntityFrameworkCore;
using Storage.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Storage
{
	public class StorageContext : DbContext
	{
		protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
		{
			if (optionsBuilder.IsConfigured == false)
			{
				optionsBuilder.UseSqlServer(@"  Data Source=localhost\SQLEXPRESS;
												Initial Catalog=DCP;
												Integrated Security=True;
												MultipleActiveResultSets=True;;
												TrustServerCertificate=True");

			}
			base.OnConfiguring(optionsBuilder);
		}

		public virtual DbSet<User> Users { set; get; }
		public virtual DbSet<Role> Roles { set; get; }
		public virtual DbSet<Document> Documents { set; get; }
		public virtual DbSet<DocumentUser> DocumentUsers { set; get; }
		public virtual DbSet<Signature> Signatures { set; get; }
		public virtual DbSet<Certificate> Certificates { set; get; }
	}
}
