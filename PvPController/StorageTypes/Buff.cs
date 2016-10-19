using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PvPController.StorageTypes
{
    public class Buff
    {
        public int netID;
        public int milliseconds;

        public Buff(int netID, int milliseconds)
        {
            this.netID = netID;
            this.milliseconds = milliseconds;
        }
    }
}
