using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PvPController
{
    public struct Controller
    {
        public List<ControllerItemBan> BannedItemIDs;
        public List<ControllerProjectileBan> BannedProjectileIDs;
        public List<ControllerItemBan> BannedArmorPieces;
        public List<ControllerItemBan> BannedAccessories;
        public List<ControllerWeaponBuff> WeaponBuff;
        public List<ControllerProjectileDamage> ProjectileDamageModification;
        public List<ControllerProjectileSpeed> ProjectileSpeedModification;
        public List<ControllerWeaponDamage> WeaponDamageModification;
    }
}
