using HRMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace HRMS.Infrastructure.Persistence
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<Contractor> Contractors { get; set; }
        public DbSet<Company> Companies { get; set; }
        public DbSet<Employee> Employees { get; set; }
        public DbSet<PayrollMaster> Payrolls { get; set; }
        public DbSet<PFContribution> PFContributions { get; set; }
        public DbSet<ESIContribution> ESIContributions { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<PayslipImportMaster> PayslipImportMasters { get; set; }
        public DbSet<PayslipImportDetails> PayslipImportDetails { get; set; }

        public DbSet<PayslipImport> PayslipImports { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Company>()
                .HasOne(c => c.Contractor)
                .WithMany(c => c.Companies)
                .HasForeignKey(c => c.ContractorId);

            modelBuilder.Entity<Employee>()
                .HasOne(e => e.Company)
                .WithMany(c => c.Employees)
                .HasForeignKey(e => e.CompanyId);
            modelBuilder.Entity<PFContribution>()
                .HasOne(p => p.Payroll)
                .WithOne(p => p.PFContribution)
                .HasForeignKey<PFContribution>(p => p.PayrollId);

            modelBuilder.Entity<ESIContribution>()
                .HasOne(e => e.Payroll)
                .WithOne(p => p.ESIContribution)
                .HasForeignKey<ESIContribution>(e => e.PayrollId);

            modelBuilder.Entity<PayslipImportMaster>(entity =>
            {
                entity.HasKey(e => e.ImportId);
                entity.Property(e => e.FileName).HasMaxLength(200);
                entity.Property(e => e.MonthYear).HasMaxLength(50);
                entity.Property(e => e.Status).HasMaxLength(50);
            });
        }
    }
}
