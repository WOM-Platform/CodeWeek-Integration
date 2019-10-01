using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.Extensions.Logging;
using MySql;
using WomPlatform.Web.Api.DatabaseModels;

namespace WomPlatform.Web.Api {

    public class DataContext : DbContext {

        protected ILogger<DataContext> Logger { get; }

        public DataContext(
            DbContextOptions options,
            ILogger<DataContext> logger
        ) : base(options) {
            Logger = logger;

            Logger.LogTrace(LoggingEvents.DatabaseConnection, "Creating DataContext");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder) {
            modelBuilder.Entity<Certificate>().HasKey(nameof(Certificate.CertificateId));
            modelBuilder.Entity<Certificate>().Property(nameof(Certificate.CertificateId))
                .ValueGeneratedNever().IsRequired();
            modelBuilder.Entity<Certificate>().Property(nameof(Certificate.CertificateUrl))
                .HasMaxLength(2048).IsRequired();
            modelBuilder.Entity<Certificate>().Property(nameof(Certificate.EventPageUrl))
                .HasMaxLength(2048).IsRequired();
            modelBuilder.Entity<Certificate>().Property(nameof(Certificate.RegistrationDate))
                .IsRequired();
        }

        public override void Dispose() {
            Logger.LogTrace(LoggingEvents.DatabaseConnection, "Disposing DataContext");
            base.Dispose();
        }

        public DbSet<Certificate> Certificates { get; set; }

    }

}
