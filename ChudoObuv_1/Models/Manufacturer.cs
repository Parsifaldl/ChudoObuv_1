using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ChudoObuv_1.Models;

public partial class Manufacturer
{
    [Key]
    [Column("ID_Manufacturers")]
    public int IdManufacturers { get; set; }

    [Column("Name_Manufacturer")]
    [StringLength(255)]
    public string? NameManufacturer { get; set; }

    [StringLength(255)]
    public string? City { get; set; }

    [InverseProperty("FkIdManufacturersNavigation")]
    public virtual ICollection<Product> Products { get; set; } = new List<Product>();
}
