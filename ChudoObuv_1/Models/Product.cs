using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ChudoObuv_1.Models;

public partial class Product
{
    [Key]
    [Column("ID_Products")]
    public int IdProducts { get; set; }

    [Column("FK_ID_Categories")]
    public int? FkIdCategories { get; set; }

    [Column("FK_ID_Subcategories")]
    public int? FkIdSubcategories { get; set; }

    [StringLength(255)]
    public string? Image { get; set; }

    [Column("Name_Product")]
    [StringLength(255)]
    public string? NameProduct { get; set; }

    [Column("FK_ID_Manufacturers")]
    public int? FkIdManufacturers { get; set; }

    [StringLength(255)]
    public string? Description { get; set; }

    [StringLength(255)]
    public string? Composition { get; set; }

    public int? Price { get; set; }

    [ForeignKey("FkIdCategories")]
    [InverseProperty("Products")]
    public virtual Category? FkIdCategoriesNavigation { get; set; }

    [ForeignKey("FkIdManufacturers")]
    [InverseProperty("Products")]
    public virtual Manufacturer? FkIdManufacturersNavigation { get; set; }

    [ForeignKey("FkIdSubcategories")]
    [InverseProperty("Products")]
    public virtual Subcategory? FkIdSubcategoriesNavigation { get; set; }

    [InverseProperty("FkIdProductsNavigation")]
    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    [InverseProperty("FkIdProductsNavigation")]
    public virtual ICollection<StockItem> StockItems { get; set; } = new List<StockItem>();
}
