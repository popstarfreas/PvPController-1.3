using System.Collections.Generic;

namespace PvPController.StorageTypes
{
    public class Weapon
    {
        public int netID;
        public float damageRatio;
        public float velocityRatio;
        public bool banned;
        public List<Buff> buffs = new List<Buff>();

        public Weapon(int netID, float damageRatio, float velocityRatio, bool banned)
        {
            this.netID = netID;
            this.damageRatio = damageRatio;
            this.velocityRatio = velocityRatio;
            this.banned = banned;
        }

        public void setBuffs(List<Buff> buffs)
        {
            this.buffs = buffs;
        }
    }
}
