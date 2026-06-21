using AssetKeeper.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AssetKeeper.Context;

public class MyDbContext : IdentityDbContext<IdentityUser, IdentityRole, string>
{
    public MyDbContext(DbContextOptions<MyDbContext> options) : base(options) { }

    public DbSet<Asset> Assets { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<Brand> Brands { get; set; }
    public DbSet<Employee> Employees { get; set; }
    public DbSet<AssetAssignment> AssetAssignments { get; set; }
    public DbSet<AssetHistory> AssetHistory { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // پشتیبانی از فارسی برای تمام فیلدهای رشته‌ای
        foreach (var property in modelBuilder.Model.GetEntityTypes()
            .SelectMany(e => e.GetProperties())
            .Where(p => p.ClrType == typeof(string)))
        {
            property.SetCollation("Persian_100_CI_AI");
        }

        // تنظیمات روابط
        modelBuilder.Entity<AssetAssignment>()
            .HasOne(a => a.Asset)
            .WithMany(a => a.Assignments)
            .HasForeignKey(a => a.AssetId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<AssetAssignment>()
            .HasOne(a => a.Employee)
            .WithMany(e => e.Assignments)
            .HasForeignKey(a => a.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        // رابطه Asset ↔ Category
        modelBuilder.Entity<Asset>()
            .HasOne(a => a.Category)
            .WithMany(c => c.Assets)
            .HasForeignKey(a => a.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        // رابطه Asset ↔ Brand
        modelBuilder.Entity<Asset>()
            .HasOne(a => a.Brand)
            .WithMany(b => b.Assets)
            .HasForeignKey(a => a.BrandId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Asset>()
            .HasIndex(a => a.AssetCode)
            .IsUnique();

        modelBuilder.Entity<Employee>()
            .HasIndex(e => e.PersonnelCode)
            .IsUnique();

        modelBuilder.Entity<Employee>()
            .HasIndex(e => e.NationalCode)
            .IsUnique();

        modelBuilder.Entity<AssetHistory>()
            .HasOne(h => h.ChangedByEmployee)
            .WithMany()
            .HasForeignKey(h => h.ChangedByEmployeeId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}