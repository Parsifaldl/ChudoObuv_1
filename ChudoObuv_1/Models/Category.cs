using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ChudoObuv_1.Models;

public partial class Category
{
    [Key]
    [Column("ID_Categories")]
    public int IdCategories { get; set; }

    [Column("Name_Category")]
    [StringLength(255)]
    public string? NameCategory { get; set; }

    [InverseProperty("FkIdCategoriesNavigation")]
    public virtual ICollection<Product> Products { get; set; } = new List<Product>();
}
