using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ChudoObuv_1.Models;

public partial class Role
{
    [Key]
    [Column("ID_Roles")]
    public int IdRoles { get; set; }

    [Column("Name_Role")]
    [StringLength(255)]
    public string? NameRole { get; set; }

    [InverseProperty("FkIdRolesNavigation")]
    public virtual ICollection<UsersImport> UsersImports { get; set; } = new List<UsersImport>();
}
