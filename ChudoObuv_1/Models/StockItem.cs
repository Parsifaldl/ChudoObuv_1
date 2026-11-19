using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ChudoObuv_1.Models;

[Table("Stock_Items")]
public partial class StockItem
{
    [Key]
    [Column("ID_StockItems")]
    public int IdStockItems { get; set; }

    [Column("FK_ID_Products")]
    public int? FkIdProducts { get; set; }

    [Column("FK_ID_Sizes")]
    public int? FkIdSizes { get; set; }

    public int? Quantity { get; set; }

    [ForeignKey("FkIdProducts")]
    [InverseProperty("StockItems")]
    public virtual Product? FkIdProductsNavigation { get; set; }

    [ForeignKey("FkIdSizes")]
    [InverseProperty("StockItems")]
    public virtual Size? FkIdSizesNavigation { get; set; }
}
