﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PvPController.StorageTypes
{
    public class Projectile
    {
        public int netID;
        public int damageRatio;
        public float velocityRatio;
        public bool banned;

        public Projectile(int netID, int damageRatio, float velocityRatio, bool banned)
        {
            this.netID = netID;
            this.damageRatio = damageRatio;
            this.velocityRatio = velocityRatio;
            this.banned = banned;
        }
    }
}
