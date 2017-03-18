using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data
{
    public class Reminder
    {
        public DateTime datetime { get; set; }
        public string reminder { get; set; }
        public int ID { get; set; }
        public bool Notified { get; set; }
    }
}
