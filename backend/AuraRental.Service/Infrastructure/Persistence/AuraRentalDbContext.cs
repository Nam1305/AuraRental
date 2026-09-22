using System.Globalization;
using System.Text;
using AuraRental.Domain.Entities;
using AuraRental.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace AuraRental.Service.Infrastructure.Persistence;

public sealed class AuraRentalDbContext(DbContextOptions<AuraRentalDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Branch> Branches => Set<Branch>();
    public DbSet<UserBranch> UserBranches => Set<UserBranch>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductVariant> ProductVariants => Set<ProductVariant>();
    public DbSet<InventoryItem> InventoryItems => Set<InventoryItem>();
    public DbSet<BranchRentalPrice> BranchRentalPrices => Set<BranchRentalPrice>();
    public DbSet<Setting> Settings => Set<Setting>();
    public DbSet<Reservation> Reservations => Set<Reservation>();
    public DbSet<ReservationItem> ReservationItems => Set<ReservationItem>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<Refund> Refunds => Set<Refund>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<IdempotencyRecord> IdempotencyRecords => Set<IdempotencyRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("aura");

        ConfigureUsers(modelBuilder);
        ConfigureCatalog(modelBuilder);
        ConfigureRentals(modelBuilder);
        ConfigureMoney(modelBuilder);
        ConfigureInfrastructure(modelBuilder);
        ConfigureColumnConventions(modelBuilder);
    }

    private static void ConfigureUsers(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");
            entity.HasKey(item => item.Id);
            entity.HasIndex(item => item.Username).IsUnique();
            entity.HasIndex(item => item.Email).IsUnique();
            entity.Property(item => item.Role).HasConversion(new EnumValueConverter<UserRole>());
        });

        modelBuilder.Entity<Branch>(entity =>
        {
            entity.ToTable("branches");
            entity.HasKey(item => item.Id);
            entity.HasIndex(item => item.Code).IsUnique();
        });

        modelBuilder.Entity<UserBranch>(entity =>
        {
            entity.ToTable("user_branches");
            entity.HasKey(item => new { item.UserId, item.BranchId });
            entity.HasOne(item => item.User).WithMany(item => item.UserBranches).HasForeignKey(item => item.UserId);
            entity.HasOne(item => item.Branch).WithMany(item => item.UserBranches).HasForeignKey(item => item.BranchId);
        });

        modelBuilder.Entity<Customer>(entity =>
        {
            entity.ToTable("customers");
            entity.HasKey(item => item.Id);
            entity.HasIndex(item => item.Phone).IsUnique();
        });
    }

    private static void ConfigureCatalog(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>(entity =>
        {
            entity.ToTable("products");
            entity.HasKey(item => item.Id);
            entity.HasIndex(item => new { item.BranchId, item.Code }).IsUnique();
            entity.HasOne(item => item.Branch).WithMany(item => item.Products).HasForeignKey(item => item.BranchId);
        });

        modelBuilder.Entity<ProductVariant>(entity =>
        {
            entity.ToTable("product_variants");
            entity.HasKey(item => item.Id);
            entity.HasIndex(item => new { item.ProductId, item.Size }).IsUnique();
            entity.HasOne(item => item.Product).WithMany(item => item.Variants).HasForeignKey(item => item.ProductId);
        });

        modelBuilder.Entity<InventoryItem>(entity =>
        {
            entity.ToTable("inventory_items");
            entity.HasKey(item => item.Id);
            entity.HasIndex(item => item.AssetCode).IsUnique();
            entity.HasIndex(item => new { item.BranchId, item.Status });
            entity.Property(item => item.Status).HasConversion(new EnumValueConverter<InventoryStatus>());
            entity.HasOne(item => item.Variant).WithMany(item => item.InventoryItems).HasForeignKey(item => item.VariantId);
            entity.HasOne(item => item.Branch).WithMany(item => item.InventoryItems).HasForeignKey(item => item.BranchId);
        });

        modelBuilder.Entity<BranchRentalPrice>(entity =>
        {
            entity.ToTable("branch_rental_prices");
            entity.HasKey(item => item.Id);
            entity.HasIndex(item => new { item.BranchId, item.VariantId, item.PackageCode }).IsUnique();
            entity.HasOne(item => item.Branch).WithMany(item => item.RentalPrices).HasForeignKey(item => item.BranchId);
            entity.HasOne(item => item.Variant).WithMany(item => item.RentalPrices).HasForeignKey(item => item.VariantId);
        });

        modelBuilder.Entity<Setting>(entity =>
        {
            entity.ToTable("settings");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Id).ValueGeneratedNever();
        });
    }

    private static void ConfigureRentals(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Reservation>(entity =>
        {
            entity.ToTable("reservations");
            entity.HasKey(item => item.Id);
            entity.HasIndex(item => item.ReservationNo).IsUnique();
            entity.HasIndex(item => item.FormTokenHash).IsUnique();
            entity.HasIndex(item => new { item.BranchId, item.Status, item.RentalStartAt });
            entity.Property(item => item.Status).HasConversion(new EnumValueConverter<ReservationStatus>());
            entity.HasOne(item => item.Customer).WithMany(item => item.Reservations).HasForeignKey(item => item.CustomerId);
            entity.HasOne(item => item.Branch).WithMany().HasForeignKey(item => item.BranchId);
            entity.HasOne(item => item.Creator).WithMany().HasForeignKey(item => item.CreatedBy).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ReservationItem>(entity =>
        {
            entity.ToTable("reservation_items");
            entity.HasKey(item => item.Id);
            entity.HasIndex(item => new { item.ReservationId, item.InventoryItemId }).IsUnique();
            entity.HasOne(item => item.Reservation).WithMany(item => item.Items).HasForeignKey(item => item.ReservationId);
            entity.HasOne(item => item.InventoryItem).WithMany(item => item.ReservationItems).HasForeignKey(item => item.InventoryItemId);
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.ToTable("orders");
            entity.HasKey(item => item.Id);
            entity.HasIndex(item => item.OrderNo).IsUnique();
            entity.HasIndex(item => item.ReservationId).IsUnique();
            entity.HasIndex(item => new { item.CustomerId, item.CreatedAt });
            entity.HasIndex(item => new { item.BranchId, item.CreatedAt });
            entity.Property(item => item.Status).HasConversion(new EnumValueConverter<OrderStatus>());
            entity.HasOne(item => item.Customer).WithMany(item => item.Orders).HasForeignKey(item => item.CustomerId);
            entity.HasOne(item => item.Reservation).WithOne(item => item.Order).HasForeignKey<Order>(item => item.ReservationId);
            entity.HasOne(item => item.Branch).WithMany().HasForeignKey(item => item.BranchId);
            entity.HasOne(item => item.IdentityVerifier).WithMany().HasForeignKey(item => item.IdentityVerifiedBy).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(item => item.Settler).WithMany().HasForeignKey(item => item.SettledBy).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.ToTable("order_items");
            entity.HasKey(item => item.Id);
            entity.HasIndex(item => new { item.OrderId, item.InventoryItemId }).IsUnique();
            entity.Property(item => item.Condition).HasConversion(new EnumValueConverter<ItemCondition>());
            entity.HasOne(item => item.Order).WithMany(item => item.Items).HasForeignKey(item => item.OrderId);
            entity.HasOne(item => item.InventoryItem).WithMany(item => item.OrderItems).HasForeignKey(item => item.InventoryItemId);
        });
    }

    private static void ConfigureMoney(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Refund>(entity =>
        {
            entity.ToTable("refunds");
            entity.HasKey(item => item.Id);
            entity.HasIndex(item => new { item.OrderId, item.Version }).IsUnique();
            entity.Property(item => item.Status).HasConversion(new EnumValueConverter<RefundStatus>());
            entity.Property(item => item.ItemsSnapshot).HasColumnType("jsonb");
            entity.HasOne(item => item.Order).WithMany(item => item.Refunds).HasForeignKey(item => item.OrderId);
            entity.HasOne(item => item.Creator).WithMany().HasForeignKey(item => item.CreatedBy).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(item => item.Submitter).WithMany().HasForeignKey(item => item.SubmittedBy).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(item => item.Approver).WithMany().HasForeignKey(item => item.ApprovedBy).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.ToTable("payments");
            entity.HasKey(item => item.Id);
            entity.HasIndex(item => item.ReservationId);
            entity.Property(item => item.Type).HasConversion(new EnumValueConverter<PaymentType>());
            entity.Property(item => item.Status).HasConversion(new EnumValueConverter<PaymentStatus>());
            entity.HasOne(item => item.Reservation).WithMany(item => item.Payments).HasForeignKey(item => item.ReservationId);
            entity.HasOne(item => item.Refund).WithMany(item => item.Payments).HasForeignKey(item => item.RefundId);
            entity.HasOne(item => item.Recorder).WithMany().HasForeignKey(item => item.RecordedBy).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(item => item.Confirmer).WithMany().HasForeignKey(item => item.ConfirmedBy).OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureInfrastructure(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<IdempotencyRecord>(entity =>
        {
            entity.ToTable("idempotency_records");
            entity.HasKey(item => item.Id);
            entity.HasIndex(item => new { item.Scope, item.Operation, item.IdempotencyKey }).IsUnique();
            entity.HasIndex(item => item.ExpiresAt);
        });
    }

    private static void ConfigureColumnConventions(ModelBuilder modelBuilder)
    {
        foreach (var entity in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entity.GetProperties())
            {
                property.SetColumnName(ToSnakeCase(property.Name));

                if (property.ClrType == typeof(decimal) || property.ClrType == typeof(decimal?))
                {
                    property.SetPrecision(14);
                    property.SetScale(0);
                }
            }
        }

        modelBuilder.Entity<ReservationItem>().Property(item => item.ExtraDayRate).HasPrecision(6, 4);
        modelBuilder.Entity<OrderItem>().Property(item => item.ExtraDayRate).HasPrecision(6, 4);
        modelBuilder.Entity<Setting>().Property(item => item.ExtraDayRate).HasPrecision(6, 4);
    }

    private static string ToSnakeCase(string value)
    {
        var builder = new StringBuilder(value.Length + 8);
        for (var index = 0; index < value.Length; index++)
        {
            var character = value[index];
            if (index > 0 && char.IsUpper(character))
            {
                builder.Append('_');
            }

            builder.Append(char.ToLower(character, CultureInfo.InvariantCulture));
        }

        return builder.ToString();
    }
}
