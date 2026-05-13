using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SQLite;

namespace CalorieLens.Models
{
    public class User
{
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

    public string Username { get; set; }
    public string Password { get; set; }

    public double Weight { get; set; }
    public double Height { get; set; }

    public double TargetWeight { get; set; }
}
}
