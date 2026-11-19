using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ChudoObuv_1.Models;

public partial class Subcategory
{
    [Key]
    [Column("ID_Subcategories")]
    public int IdSubcategories { get; set; }

    [Column("Name_Subcategory")]
    [StringLength(255)]
    public string? NameSubcategory { get; set; }

    [InverseProperty("FkIdSubcategoriesNavigation")]
    public virtual ICollection<Product> Products { get; set; } = new List<Product>();
}
