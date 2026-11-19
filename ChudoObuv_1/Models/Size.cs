using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ChudoObuv_1.Models;

public partial class Size
{
    [Key]
    [Column("ID_Sizes")]
    public int IdSizes { get; set; }

    [Column("Name_Size")]
    public int? NameSize { get; set; }

    [InverseProperty("FkIdSizesNavigation")]
    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    [InverseProperty("FkIdSizesNavigation")]
    public virtual ICollection<StockItem> StockItems { get; set; } = new List<StockItem>();
}
