using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace SumarAuto.Client.Models
{
    public class LoginVM
    {
        [Required]
        public string UserName { get; set; }
        public int userId { get; set; }

        [Required]
        public string Password { get; set; }
    }
}