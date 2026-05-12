using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using AgiVysSystem.Api.Models.People; 
using AgiVysSystem.Api.Models.Configuration;
using AgiVysSystem.Api.Models.Company;
using AgiVysSystem.Api.Models.Companies;
using AgiVysSystem.Api.Models.Financial;

namespace AgiVysSystem.Api.Data;

public class AppDbContext : IdentityDbContext<AgiVysSystem.Api.Models.User.User, IdentityRole<int>, int>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Person> People { get; set; }
    public DbSet<AppSystem> AppSystems { get; set; }
    public DbSet<Plan> Plans { get; set; }
    public DbSet<Menu> Menus { get; set; }
    public DbSet<Submenu> Submenus { get; set; }
    public DbSet<Company> Companies { get; set; }
    public DbSet<CompanyAddress> CompanyAddresses { get; set; }
    public DbSet<AddressPerson> AddressPeople { get; set; }
    public DbSet<UserSystem> UserSystems { get; set; }

    public DbSet<AgiVysSystem.Api.Models.Order.Order> Orders { get; set; }
    public DbSet<AgiVysSystem.Api.Models.Order.OrderItem> OrderItems { get; set; }
    public DbSet<Payment> Payments { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {   
        base.OnModelCreating(builder);
        
        builder.Entity<AgiVysSystem.Api.Models.Order.Order>().Property(p => p.TotalValue).HasPrecision(18, 2);
        builder.Entity<AgiVysSystem.Api.Models.Order.OrderItem>().Property(p => p.Value).HasPrecision(18, 2);
        builder.Entity<Payment>().Property(p => p.Value).HasPrecision(18, 2);
        builder.Entity<Payment>().Property(p => p.NetValue).HasPrecision(18, 2);

        builder.Entity<Person>()
            .HasIndex(p => p.Document)
            .IsUnique();

        builder.Entity<Plan>()
            .HasMany(p => p.AllowedMenus)
            .WithMany()
            .UsingEntity(j => j.ToTable("PlanMenus"));

        builder.Entity<Plan>()
            .HasMany(p => p.AllowedSubmenus)
            .WithMany()
            .UsingEntity(j => j.ToTable("PlanSubmenus"));

        builder.Entity<Submenu>()
            .HasOne(s => s.Menu)
            .WithMany(m => m.Submenus)
            .HasForeignKey(s => s.MenuId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Company>()
            .HasIndex(c => c.Cnpj)
            .IsUnique();

        builder.Entity<UserSystem>()
            .HasOne(us => us.User)
            .WithMany()
            .HasForeignKey(us => us.UserId);

        builder.Entity<UserSystem>()
            .HasOne(us => us.System)
            .WithMany()
            .HasForeignKey(us => us.SystemId);
    }
}