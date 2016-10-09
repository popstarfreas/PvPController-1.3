using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PvPController
{
    public class Controller
    {
        public List<ControllerItemBan> BannedItemIDs;
        public List<ControllerProjectileBan> BannedProjectileIDs;
        public List<ControllerItemBan> BannedArmorPieces;
        public List<ControllerItemBan> BannedAccessories;
        public List<ControllerWeaponBuff> WeaponBuff;
        public List<ControllerProjectileDamage> ProjectileDamageModification;
        public List<ControllerProjectileSpeed> ProjectileSpeedModification;
        public List<ControllerWeaponDamage> WeaponDamageModification;

        public Controller()
        {
            BannedItemIDs = new List<ControllerItemBan>();
            BannedProjectileIDs = new List<ControllerProjectileBan>();
            BannedArmorPieces = new List<ControllerItemBan>();
            BannedAccessories = new List<ControllerItemBan>();
            WeaponBuff = new List<ControllerWeaponBuff>();
            ProjectileDamageModification = new List<ControllerProjectileDamage>();
            ProjectileSpeedModification = new List<ControllerProjectileSpeed>();
            WeaponDamageModification = new List<ControllerWeaponDamage>();
        }
    }
}
