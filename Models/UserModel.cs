using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Deestone.Models
{
  public class UserModel
  {
    public int Id { get; set; }

    [Required]
    [StringLength(20)]
    public string UserId { get; set; }

    [Required]
    [StringLength(20)]
    public string EmpId { get; set; }

    [Required]
    [StringLength(50)]
    public string Firstname { get; set; }
    public string Lastname { get; set; }
    public string Email { get; set; }
    public string Password { get; set; }
    public int Active { get; set; }
    public int Role { get; set; }
  }

  public class UserLogin
  {
    public string username { get; set; }
    public string password { get; set; }
  }

}