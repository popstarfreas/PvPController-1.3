using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using Mono.Data.Sqlite;
using MySql.Data.MySqlClient;
using TShockAPI;
using TShockAPI.DB;
using Terraria;

namespace PvPController
{
    public class Database
    {
        private readonly IDbConnection _db;

        internal QueryResult QueryReader(string query, params object[] args)
        {
            return _db.QueryReader(query, args);
        }

        internal int Query(string query, params object[] args)
        {
            return _db.Query(query, args);
        }

        internal void CheckTablesExists()
        {
            if (TShock.Config.StorageType.ToLower() == "sqlite")
            {
                //this._db.Query("CREATE TABLE IF NOT EXISTS Loadouts (ID INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT, Creator INT NOT NULL DEFAULT '0', Name VARCHAR(255) NOT NULL UNIQUE DEFAULT '', Inventory TEXT NOT NULL, Private TINYINT NOT NULL DEFAULT '0')");
                //this._db.Query("CREATE TABLE IF NOT EXISTS Chest_Loadouts (ID INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT, Creator INT NOT NULL DEFAULT '0', LoadoutID INT NOT NULL, TileX INT NOT NULL, TileY INT NOT NULL, WorldID INT NOT NULL, UNIQUE(TileX, TileY, WorldID))");
            }
            else
            {
                this._db.Query("CREATE TABLE IF NOT EXISTS WeaponControllerSetup (Setup TINYINT NOT NULL DEFAULT '0') ENGINE=InnoDB DEFAULT CHARSET=utf8");
                this._db.Query("CREATE TABLE IF NOT EXISTS WeaponControllers (ID INT NOT NULL AUTO_INCREMENT, Name VARCHAR(255) UNIQUE NOT NULL, PRIMARY KEY(ID)) ENGINE=InnoDB DEFAULT CHARSET=utf8");
                this._db.Query("CREATE TABLE IF NOT EXISTS WeaponControllerEntry (ID INT NOT NULL AUTO_INCREMENT, ControllerID INT NOT NULL, PRIMARY KEY(ID)) ENGINE=InnoDB DEFAULT CHARSET=utf8");
                this._db.Query("CREATE TABLE IF NOT EXISTS WeaponControllerEntryTypes (ID INT NOT NULL AUTO_INCREMENT, Name VARCHAR(255) UNIQUE NOT NULL, PRIMARY KEY(ID)) ENGINE=InnoDB DEFAULT CHARSET=utf8");
                this._db.Query("CREATE TABLE IF NOT EXISTS WeaponControllerEntryValue (EntryID INT NOT NULL, Value FLOAT NOT NULL, EntryType INT NOT NULL, PRIMARY KEY(EntryID, EntryType)) ENGINE=InnoDB DEFAULT CHARSET=utf8");
            }
        }

        /* Inserts the default values for the WeaponControllers */
        internal bool SetupControllers()
        {
            bool passed = true;
            int count = PvPController.ControlTypes.Length;
            for (int i = 0; i < count; i++)
            {
                if (this._db.Query("INSERT IGNORE INTO WeaponControllers (Name) VALUES ({0})", PvPController.ControlTypes[i]) == 0)
                {
                    passed = false;
                    break;
                }
            }

            return passed;
        }

        /* Obtains the entries from the database, and populates the controller lists with the entries
         * information. Each property and value is a seperate row, so checks are made as to whether an
         * element in the list exists with the same entry id or not, and appropriate action is taken.
         */ 
        public void ObtainControllerEntries()
        {
            int entryID;
            string controller;
            string property;
            float value;
            using (var reader = this._db.QueryReader("SELECT WCE.ID, WC.Name as Controller, WCET.Name as Property, Value FROM WeaponControllers WC INNER JOIN WeaponControllerEntry WCE ON WCE.ControllerID = WC.ID INNER JOIN WeaponControllerEntryValue WCEV ON WCEV.EntryID = WCE.ID INNER JOIN WeaponControllerEntryTypes WCET ON WCET.ID = WCEV.EntryType"))
            {
                while (reader.Read())
                {
                    entryID = reader.Get<int>("ID");
                    controller = reader.Get<string>("Controller");
                    property = reader.Get<string>("Property");
                    value = reader.Get<float>("Value");

                    switch (controller) {
                        case "WeaponBan":
                            if (property == "ItemID" && PvPController.Controller.BannedItemIDs.Count(p => p.entryID == entryID) == 0)
                            {
                                var itemBan = new ControllerItemBan();
                                itemBan.entryID = entryID;
                                itemBan.itemID = Convert.ToInt16(value);
                                PvPController.Controller.BannedItemIDs.Add(itemBan);
                            }
                            break;
                        case "ProjectileBan":
                            if (property == "ProjectileID" && PvPController.Controller.BannedProjectileIDs.Count(p => p.entryID == entryID) == 0)
                            {
                                var projectileBan = new ControllerProjectileBan();
                                projectileBan.entryID = entryID;
                                projectileBan.projectileID = Convert.ToInt16(value);
                                PvPController.Controller.BannedProjectileIDs.Add(projectileBan);
                            }
                            break;
                        case "ArmorBan":
                            if (property == "ItemID" && PvPController.Controller.BannedArmorPieces.Count(p => p.entryID == entryID) == 0)
                            {
                                var itemBan = new ControllerItemBan();
                                itemBan.entryID = entryID;
                                itemBan.itemID = Convert.ToInt16(value);
                                PvPController.Controller.BannedArmorPieces.Add(itemBan);
                            }
                            break;
                        case "AccessoriesBan":
                            if (property == "ItemID" && PvPController.Controller.BannedAccessories.Count(p => p.entryID == entryID) == 0)
                            {
                                var itemBan = new ControllerItemBan();
                                itemBan.entryID = entryID;
                                itemBan.itemID = Convert.ToInt16(value);
                                PvPController.Controller.BannedAccessories.Add(itemBan);
                            }
                            break;
                        case "ProjectileDamage":
                            if (PvPController.Controller.ProjectileDamageModification.Count(p => p.entryID == entryID) == 0)
                            {
                                var projectileDamage = new ControllerProjectileDamage();
                                projectileDamage.entryID = entryID;
                                switch(property)
                                {
                                    case "ProjectileID":
                                        projectileDamage.projectileID = Convert.ToInt16(value);
                                        break;
                                    case "DamageRatio":
                                        projectileDamage.damageRatio = value;
                                        break; 
                                }

                                PvPController.Controller.ProjectileDamageModification.Add(projectileDamage);
                            } else
                            {
                                var entry = PvPController.Controller.ProjectileDamageModification.FirstOrDefault(p => p.entryID == entryID);
                                switch (property)
                                {
                                    case "ProjectileID":
                                        entry.projectileID = Convert.ToInt16(value);
                                        break;
                                    case "DamageRatio":
                                        entry.damageRatio = value;
                                        break;
                                }
                            }
                            break;
                        case "ProjectileSpeed":
                            if (PvPController.Controller.ProjectileSpeedModification.Count(p => p.entryID == entryID) == 0)
                            {
                                var projectileSpeed = new ControllerProjectileSpeed();
                                projectileSpeed.entryID = entryID;
                                switch (property)
                                {
                                    case "ProjectileID":
                                        projectileSpeed.projectileID = Convert.ToInt16(value);
                                        break;
                                    case "SpeedRatio":
                                        projectileSpeed.speedRatio = value;
                                        break;
                                }

                                PvPController.Controller.ProjectileSpeedModification.Add(projectileSpeed);
                            }
                            else
                            {
                                var entry = PvPController.Controller.ProjectileSpeedModification.FirstOrDefault(p => p.entryID == entryID);
                                switch (property)
                                {
                                    case "ItemID":
                                        entry.projectileID = Convert.ToInt16(value);
                                        break;
                                    case "SpeedRatio":
                                        entry.speedRatio = value;
                                        break;
                                }
                            }
                            break;
                        case "WeaponBuff":
                            if (PvPController.Controller.WeaponBuff.Count(p => p.entryID == entryID) == 0)
                            {
                                var weaponBuff = new ControllerWeaponBuff();
                                weaponBuff.entryID = entryID;
                                switch (property)
                                {
                                    case "ItemID":
                                        weaponBuff.weaponID = Convert.ToInt16(value);
                                        break;
                                    case "Milliseconds":
                                        weaponBuff.buffMilliseconds = Convert.ToInt16(value);
                                        break;
                                    case "BuffID":
                                        weaponBuff.buffID = Convert.ToInt16(value);
                                        break;
                                }

                                PvPController.Controller.WeaponBuff.Add(weaponBuff);
                            }
                            else
                            {
                                var entry = PvPController.Controller.WeaponBuff.FirstOrDefault(p => p.entryID == entryID);
                                switch (property)
                                {
                                    case "ItemID":
                                        entry.weaponID = Convert.ToInt16(value);
                                        break;
                                    case "Milliseconds":
                                        entry.buffMilliseconds = Convert.ToInt16(value);
                                        break;
                                    case "BuffID":
                                        entry.buffID = Convert.ToInt16(value);
                                        break;
                                }
                            }
                            break;
                        case "WeaponDamage":
                            if (PvPController.Controller.WeaponDamageModification.Count(p => p.entryID == entryID) == 0)
                            {
                                var weaponDamage = new ControllerWeaponDamage();
                                weaponDamage.entryID = entryID;
                                switch (property)
                                {
                                    case "ItemID":
                                        weaponDamage.weaponID = Convert.ToInt16(value);
                                        break;
                                    case "DamageRatio":
                                        weaponDamage.damageRatio = value;
                                        break;
                                }

                                PvPController.Controller.WeaponDamageModification.Add(weaponDamage);
                            }
                            else
                            {
                                var entry = PvPController.Controller.WeaponDamageModification.FirstOrDefault(p => p.entryID == entryID);
                                switch (property)
                                {
                                    case "ItemID":
                                        entry.weaponID = Convert.ToInt16(value);
                                        break;
                                    case "DamageRatio":
                                        entry.damageRatio = value;
                                        break;
                                }
                            }
                            break;
                    }
                }
            }
        }

        private Database(IDbConnection db)
        {
            _db = db;
        }

        public static Database InitDb(string name)
        {
            IDbConnection idb;
            if (TShock.Config.StorageType.ToLower() == "sqlite")
                idb =
                    new SqliteConnection(string.Format("uri=file://{0},Version=3",
                                                       Path.Combine(TShock.SavePath, name + ".sqlite")));

            else if (TShock.Config.StorageType.ToLower() == "mysql")
            {
                try
                {
                    string[] host = TShock.Config.MySqlHost.Split(':');
                    idb = new MySqlConnection
                    {
                        ConnectionString = String.Format("Server={0}; Port={1}; Database={2}; Uid={3}; Pwd={4}",
                                                         host != null && host.Length > 0 ? host[0] : "localhost",
                                                         host != null && host.Length > 1 ? host[1] : "3306",
                                                         TShock.Config.MySqlDbName,
                                                         TShock.Config.MySqlUsername,
                                                         TShock.Config.MySqlPassword
                                                         )
                    };
                    Console.WriteLine("A");
                }
                catch (MySqlException x)
                {
                    TShock.Log.Error(x.ToString());
                    throw new Exception("MySQL not setup correctly.");
                }
            }
            else
                throw new Exception("Invalid storage type.");


            var db = new Database(idb);
            return db;
        }
    }
}