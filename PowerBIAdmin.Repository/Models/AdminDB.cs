using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Text;

namespace PowerBIAdmin.Repository.Models
{
    public class PowerBiWorkspace
    {
        [Key]
        public string Name { get; set; }
        public string WorkspaceId { get; set; }
        public string WorkspaceUrl { get; set; }
    }

    public class User
    {
        [Key]
        public string LoginId { get; set; }
        public bool CanEdit { get; set; }
        public bool CanCreate { get; set; }
        public DateTime Created { get; set; }
        public string WorkspaceName { get; set; }
        public string PointApiUrl { get; set; }
    }

    public class AppOwnsDataDB : DbContext
    {
        public AppOwnsDataDB(DbContextOptions<AppOwnsDataDB> options) : base(options) { }

        public DbSet<PowerBiWorkspace> Workspaces { get; set; }

        public DbSet<User> Users { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        }
    }

    public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppOwnsDataDB>
    {
        public AppOwnsDataDB CreateDbContext(string[] args)
        {
            string configFilePath = @Directory.GetCurrentDirectory() + "/../PowerBIAdmin/appsettings.json";
            IConfigurationRoot configuration = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile(configFilePath).Build();
            var builder = new DbContextOptionsBuilder<AppOwnsDataDB>();
            builder.UseSqlServer(configuration["AppOwnsDataDB:ConnectString"]);
            return new AppOwnsDataDB(builder.Options);
        }
    }

}
