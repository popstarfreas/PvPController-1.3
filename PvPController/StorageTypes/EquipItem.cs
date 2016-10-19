using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PvPController.StorageTypes
{
    public class EquipItem
    {
        public int netID;
        public bool banned;

        public EquipItem(int netID, bool banned)
        {
            this.netID = netID;
            this.banned = banned;
        }
    }
}
