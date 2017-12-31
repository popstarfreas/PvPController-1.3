using System.Collections.Generic;

namespace PvPController.StorageTypes
{
    public class Weapon
    {
        public int netID;
        public float damageRatio;
        public float velocityRatio;
        public int minDamage = -1;
        public int maxDamage = -1;
        public bool banned;
        public List<Buff> buffs = new List<Buff>();

        public Weapon(int netID, float damageRatio, float velocityRatio, bool banned, int minDamage, int maxDamage)
        {
            this.netID = netID;
            this.damageRatio = damageRatio;
            this.velocityRatio = velocityRatio;
            this.banned = banned;
            this.minDamage = minDamage;
            this.maxDamage = maxDamage;
        }

        public void setBuffs(List<Buff> buffs)
        {
            this.buffs = buffs;
        }   
    }
}
