using System;
using System.IO;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;
using System.Timers;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using StackExchange.Redis;
using PvPController.StorageTypes;
using Newtonsoft.Json.Linq;

namespace PvPController
{
    [ApiVersion(1, 25)]
    public class PvPController : TerrariaPlugin
    {
        public Timer OnSecondUpdate;
        public static Config Config = new Config();
        public static Database database;
        public static ConnectionMultiplexer redis;
        public static string[] ControlTypes = {
            "WeaponBan",
            "ProjectileBan",
            "ArmorBan",
            "AccessoriesBan",
            "ProjectileDamage",
            "ProjectileSpeed",
            "WeaponBuff",
            "WeaponDamage"
        };
        public static string[] ControlEntryTypes = {
            "ItemID",
            "ProjectileID",
            "BuffID",
            "DamageRatio",
            "SpeedRatio",
            "Milliseconds"
        };

        public static List<Weapon> weapons = new List<Weapon>();
        public static List<StorageTypes.Projectile> projectiles = new List<StorageTypes.Projectile>();
        public static List<EquipItem> equipItems = new List<EquipItem>();

        // Tracks what weapon created what projectile for the specified projectile index
        public static Item[,] ProjectileWeapon = new Item[255, Main.maxProjectileTypes];

        // Tracks The last active bow weapon for the specified player index
        public static Item[] LastActiveBow = new Item[255];

        public override string Author
        {
            get
            {
                return "popstarfreas";
            }
        }

        public override string Description
        {
            get
            {
                return "The mightiest power in PvP Control ever created.";
            }
        }

        public override string Name
        {
            get
            {
                return "PvP Controller";
            }
        }

        public override Version Version
        {
            get
            {
                return new Version(2, 0, 0);
            }
        }

        public PvPController(Main game)
            : base(game)
        {
            Order = 6;
        }

        /* Registers the appropriate hooks, loads the existing config or write one if not,
         * and then starts the one second update timer.
         */
        public override void Initialize()
        {
            ServerApi.Hooks.NetGetData.Register(this, GetData);
            Commands.ChatCommands.Add(new Command("pvpcontroller.reload", Reload, "pvpcreload"));

            /* Config Setup */
            string path = Path.Combine(TShock.SavePath, "PvPController.json");
            if (!File.Exists(path))
                Config.WriteTemplates(path);
            Config = Config.Read(path);
            foreach (var netID in Config.BannedArmorPieces)
            {
                equipItems.Add(new EquipItem(netID, true));
            }

            /* Redis Setup */
            redis = ConnectionMultiplexer.Connect(Config.RedisHost);
            ISubscriber sub = redis.GetSubscriber();
            sub.SubscribeAsync("pvpcontroller-updates", (channel, message) =>
            {
                Console.WriteLine(message);
                parseUpdate(message);
            });

            GetDataHandlers.InitGetDataHandler();

            /* Database setup */
            database = new Database();
            weapons = database.GetWeapons();
            projectiles = database.GetProjectiles();
            database.addWeaponBuffs(weapons);

            /* Update Timer running every second */
            OnSecondUpdate = new Timer(1000);
            OnSecondUpdate.Enabled = true;
            OnSecondUpdate.Elapsed += SecondUpdate;
        }

        /* Parses updates from the redis channel. These changes will be made to the existing
         * local lists of Weapons and Projectiles, the internal state of the DB has already been
         * updated by the time this comes through (or is in the process of).
         * 
         * @param message
         *      The raw message from the subscribed channel.
         */
        void parseUpdate(string message)
        {
            dynamic update = JObject.Parse(message);
            string objectType = update.objectType;
            Console.WriteLine(objectType);
            float value = update.value;

            switch(objectType)
            {
                case "weapon":
                    handleWeaponUpdate(update);
                    break;
                case "projectile":
                    handleProjectileUpdate(update);
                    break;
            }
        }

        /* Handles an update for an existing weapon
         * 
         * @param update
         *      The update object contained the netID, changeType and value
         */
        void handleWeaponUpdate(dynamic update)
        {
            string changeType = update.changeType;
            int netID = update.netID;
            var weapon = weapons.FirstOrDefault(p => p.netID == netID);

            /* The weapon was not found in the list, therefore it cannot be used
                * since we do not have the other values of the object.*/
            if (weapon == null)
            {
                Console.WriteLine($"{netID} unusable");
                return;
            }

            switch (changeType)
            {
                case "damageRatio":
                    weapon.damageRatio = Convert.ToSingle(update.value);
                    break;
                case "velocityRatio":
                    weapon.velocityRatio = Convert.ToSingle(update.value);
                    break;
                case "banned":
                    weapon.banned = Convert.ToBoolean(update.value);
                    break;
            }
        }

        /* Handles an update for an existing weapon
         * 
         * @param update
         *      The update object contained the netID, changeType and value
         */
        void handleProjectileUpdate(dynamic update)
        {
            string changeType = update.changeType;
            int netID = update.netID;
            var projectile = projectiles.FirstOrDefault(p => p.netID == netID);
            
            /* The weapon was not found in the list, therefore it cannot be used
             * since we do not have the other values of the object.*/
            if (projectile == null)
            {
                return;
            }

            switch (changeType)
            {
                case "damageRatio":
                    projectile.damageRatio = Convert.ToSingle(update.value);
                    break;
                case "velocityRatio":
                    projectile.velocityRatio = Convert.ToSingle(update.value);
                    break;
                case "banned":
                    projectile.banned = Convert.ToBoolean(update.value);
                    break;
            }
        }

        /* Checks if any of the players are using a banned accessory or armor item
         * and will prevent any damage for a specified amount of time and teleport
         * them to spawn if so.
         */
        private void SecondUpdate(object sender, ElapsedEventArgs args)
        {
            var players = TShock.Players.ToList();
            try
            {
                foreach (var player in players)
                {
                    if (player != null)
                    {
                        if (player.TPlayer.hostile)
                        {
                            bool violation = false;
                            if (player.TPlayer.blackBelt)
                            {
                                violation = true;
                                player.TPlayer.hostile = false;
                                player.SendData(PacketTypes.TogglePvp, "", player.Index, 0f, 0f, 0f, 0);
                                player.Teleport(Main.spawnTileX * 16, (Main.spawnTileY - 3) * 16);
                                player.SendMessage("TELEPORT WARNING: Master Ninja Gear & Blackbelt are not allowed for PvP!", 217, 255, 0);
                            }
                            else
                            {
                                // Armor and Accessories (active) are 0-9
                                for (int slot = 0; slot <= 9; slot++)
                                {
                                    bool bannedArmor = equipItems.Count(p => p.netID == player.TPlayer.armor[slot].netID && p.banned) > 0;
                                    bool bannedAccessory = equipItems.Count(p => p.netID == player.TPlayer.armor[slot].netID && p.banned) > 0;

                                    if (bannedArmor || bannedAccessory)
                                    {
                                        string type = bannedArmor ? "Armor" : "Accessory";
                                        violation = true;
                                        player.TPlayer.hostile = false;
                                        player.SendData(PacketTypes.TogglePvp, "", player.Index, 0f, 0f, 0f, 0);
                                        player.Teleport(Main.spawnTileX * 16, (Main.spawnTileY - 3) * 16);
                                        player.SendMessage($"TELEPORT WARNING:{type} {player.TPlayer.armor[slot].name} is not allowed for PvP!", 217, 255, 0);
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
            } catch(Exception e)
            {
                TShock.Log.ConsoleError(e.Message);
            }
        }

        /* Passes on packets received to the DataHandlers to be processed if necessary */
        private void GetData(GetDataEventArgs args)
        {
            var type = args.MsgID;
            var player = TShock.Players[args.Msg.whoAmI];

            if (player == null)
            {
                args.Handled = true;
                return;
            }

            if (!player.ConnectionAlive)
            {
                args.Handled = true;
                return;
            }

            using (var data = new MemoryStream(args.Msg.readBuffer, args.Index, args.Length))
            {
                try
                {
                    if (GetDataHandlers.HandlerGetData(type, player, data))
                    {
                        args.Handled = true;
                    }
                }
                catch (Exception ex)
                {
                    TShock.Log.ConsoleError(ex.ToString());
                }
            }
        }

        /* Updates the config object with the existing config file, or creates a new
         * one if it doesn't exist.
         * 
         * @param e
         *          The command args object from tshock containing information such as
         *          what player used the command, and the command parameters
         */
        void Reload(CommandArgs e)
        {
            string path = Path.Combine(TShock.SavePath, "PvPController.json");
            if (!File.Exists(path))
                Config.WriteTemplates(path);
            Config = Config.Read(path);
            foreach (var netID in Config.BannedArmorPieces)
            {
                equipItems.Add(new EquipItem(netID, true));
            }
            e.Player.SendSuccessMessage("Reloaded PvPController config.");
            weapons = database.GetWeapons();
            projectiles = database.GetProjectiles();
        }

        /* Disposes of any registered hooks*/
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ServerApi.Hooks.NetGetData.Deregister(this, GetData);
                base.Dispose(disposing);
            }
        }
    }
}

