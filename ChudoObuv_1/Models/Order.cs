using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ChudoObuv_1.Models;

public partial class Order
{
    [Key]
    [Column("ID_Orders")]
    public int IdOrders { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? OrderDate { get; set; }

    [Column("FK_ID_Users")]
    public int? FkIdUsers { get; set; }

    [ForeignKey("FkIdUsers")]
    [InverseProperty("Orders")]
    public virtual UsersImport? FkIdUsersNavigation { get; set; }

    [InverseProperty("FkIdOrdersNavigation")]
    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}
