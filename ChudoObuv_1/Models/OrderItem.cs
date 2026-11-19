using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ChudoObuv_1.Models;

[Table("Order_Items")]
public partial class OrderItem
{
    [Key]
    [Column("ID_OrderItems")]
    public int IdOrderItems { get; set; }

    [Column("FK_ID_Orders")]
    public int? FkIdOrders { get; set; }

    [Column("FK_ID_Products")]
    public int? FkIdProducts { get; set; }

    [Column("FK_ID_Sizes")]
    public int? FkIdSizes { get; set; }

    public int? Quantity { get; set; }

    public int? UnitPrice { get; set; }

    [ForeignKey("FkIdOrders")]
    [InverseProperty("OrderItems")]
    public virtual Order? FkIdOrdersNavigation { get; set; }

    [ForeignKey("FkIdProducts")]
    [InverseProperty("OrderItems")]
    public virtual Product? FkIdProductsNavigation { get; set; }

    [ForeignKey("FkIdSizes")]
    [InverseProperty("OrderItems")]
    public virtual Size? FkIdSizesNavigation { get; set; }
}
