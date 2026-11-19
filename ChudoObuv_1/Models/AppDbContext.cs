using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace ChudoObuv_1.Models;

public partial class AppDbContext : DbContext
{
    public AppDbContext()
    {
    }

    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Category> Categories { get; set; }

    public virtual DbSet<Manufacturer> Manufacturers { get; set; }

    public virtual DbSet<Order> Orders { get; set; }

    public virtual DbSet<OrderItem> OrderItems { get; set; }

    public virtual DbSet<Product> Products { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<Size> Sizes { get; set; }

    public virtual DbSet<StockItem> StockItems { get; set; }

    public virtual DbSet<Subcategory> Subcategories { get; set; }

    public virtual DbSet<UsersImport> UsersImports { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Server=.\\SQLEXPRESS;Database=ChudoObuv_1;Trusted_Connection=True;TrustServerCertificate=True;");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.IdCategories).HasName("PK__Categori__A48D362892BD70F4");
        });

        modelBuilder.Entity<Manufacturer>(entity =>
        {
            entity.HasKey(e => e.IdManufacturers).HasName("PK__Manufact__16B39E1D05D2CF24");
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.IdOrders).HasName("PK__Orders$__20F81C1D898CF3A6");

            entity.HasOne(d => d.FkIdUsersNavigation).WithMany(p => p.Orders).HasConstraintName("FK_Orders_Users_import");
        });

        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.HasKey(e => e.IdOrderItems).HasName("PK__Order_It__834649EE9E6DC858");

            entity.HasOne(d => d.FkIdOrdersNavigation).WithMany(p => p.OrderItems).HasConstraintName("FK_Order_Items_Orders");

            entity.HasOne(d => d.FkIdProductsNavigation).WithMany(p => p.OrderItems).HasConstraintName("FK_Order_Items_Products");

            entity.HasOne(d => d.FkIdSizesNavigation).WithMany(p => p.OrderItems).HasConstraintName("FK_Order_Items_Sizes");
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.IdProducts).HasName("PK__Products__9B4120EE0E7D5ACC");

            entity.HasOne(d => d.FkIdCategoriesNavigation).WithMany(p => p.Products).HasConstraintName("FK_Products_Categories");

            entity.HasOne(d => d.FkIdManufacturersNavigation).WithMany(p => p.Products).HasConstraintName("FK_Products_Manufacturers");

            entity.HasOne(d => d.FkIdSubcategoriesNavigation).WithMany(p => p.Products).HasConstraintName("FK_Products_Subcategories");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.IdRoles).HasName("PK__Roles$__30F629937F5B3A7A");
        });

        modelBuilder.Entity<Size>(entity =>
        {
            entity.HasKey(e => e.IdSizes).HasName("PK__Sizes$__83C707BF5D297CD5");
        });

        modelBuilder.Entity<StockItem>(entity =>
        {
            entity.HasKey(e => e.IdStockItems).HasName("PK__Stock_It__B68E6DCAC2A1BBB0");

            entity.HasOne(d => d.FkIdProductsNavigation).WithMany(p => p.StockItems).HasConstraintName("FK_Stock_Items_Products");

            entity.HasOne(d => d.FkIdSizesNavigation).WithMany(p => p.StockItems).HasConstraintName("FK_Stock_Items_Sizes");
        });

        modelBuilder.Entity<Subcategory>(entity =>
        {
            entity.HasKey(e => e.IdSubcategories).HasName("PK__Subcateg__A4010AB6E6EBD035");
        });

        modelBuilder.Entity<UsersImport>(entity =>
        {
            entity.HasKey(e => e.IdUsers).HasName("PK__Users_im__B97FFDA1F53B8A92");

            entity.HasOne(d => d.FkIdRolesNavigation).WithMany(p => p.UsersImports).HasConstraintName("FK_Users_import_Roles");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
