using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ChudoObuv_1.Models;

[Table("Users_import")]
public partial class UsersImport
{
    [Key]
    [Column("ID_Users")]
    public int IdUsers { get; set; }

    [StringLength(255)]
    public string? FirstName { get; set; }

    [StringLength(255)]
    public string? Name { get; set; }

    [StringLength(255)]
    public string? LastName { get; set; }

    [StringLength(255)]
    public string? Login { get; set; }

    [Column("FK_ID_Roles")]
    public int? FkIdRoles { get; set; }

    [ForeignKey("FkIdRoles")]
    [InverseProperty("UsersImports")]
    public virtual Role? FkIdRolesNavigation { get; set; }

    [InverseProperty("FkIdUsersNavigation")]
    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
}
