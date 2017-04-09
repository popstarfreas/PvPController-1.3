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
        // Events for other plugins
        public delegate void PlayerKillHandler(object sender, PlayerKillEventArgs e);
        public event PlayerKillHandler OnPlayerKill;

        public delegate void PlayerDamageHandler(object sender, PlayerDamageEventArgs e);
        public event PlayerDamageHandler OnPlayerDamage;
        public DateTime[] LastMessage = new DateTime[256];

        public Timer OnSecondUpdate;
        private GetDataHandlers GetDataHandler;
        private string ConfigPath;
        public Config Config;
        public Database Database;
        public ConnectionMultiplexer Redis;

        public List<Weapon> Weapons = new List<Weapon>();
        public List<StorageTypes.Projectile> Projectiles = new List<StorageTypes.Projectile>();
        public List<EquipItem> EquipItems = new List<EquipItem>();
        public Player[] Players = new Player[256];

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
        
        /// <summary>
        /// Registers the appropriate hooks, loads the existing config or write one if not, and then starts the one second update timer.
        /// </summary>
        public override void Initialize()
        {
            ServerApi.Hooks.NetGetData.Register(this, GetData);
            Commands.ChatCommands.Add(new Command("pvpcontroller.reload", Reload, "pvpcreload"));

            /* Config Setup */
            ConfigPath = Path.Combine(TShock.SavePath, "PvPController.json");
            Config = new Config(ConfigPath);

            foreach (var netID in Config.BannedArmorPieces)
            {
                EquipItems.Add(new EquipItem(netID, true));
            }

            /* Redis Setup */
            Redis = ConnectionMultiplexer.Connect(Config.redisHost);
            ISubscriber sub = Redis.GetSubscriber();
            sub.SubscribeAsync("pvpcontroller-updates", (channel, message) =>
            {
                Console.WriteLine(message);
                parseUpdate(message);
            });

            GetDataHandler = new GetDataHandlers(this);

            /* Database setup */
            Database = new Database(Config);
            Weapons = Database.GetWeapons();
            Projectiles = Database.GetProjectiles();
            Database.addWeaponBuffs(Weapons);

            /* Update Timer running every second */
            OnSecondUpdate = new Timer(1000);
            OnSecondUpdate.Enabled = true;
            OnSecondUpdate.Elapsed += SecondUpdate;
        }

        /// <summary>
        /// Raises a player kill event if there are any listeners
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        internal void RaisePlayerKillEvent(object sender, PlayerKillEventArgs args)
        {
            OnPlayerKill?.Invoke(sender, args);
        }
        
        /// <summary>
        /// Riases a player death event if there are any listeners
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        internal void RaisePlayerDamageEvent(object sender, PlayerDamageEventArgs args)
        {
            OnPlayerDamage?.Invoke(sender, args);
        }

        /// <summary>
        /// Parses updates from the redis channel. These changes will be made to the existing
        /// local lists of Weapons and Projectiles, the internal state of the DB should have
        /// already been updated by the time this comes through (or is in the process of).
        /// </summary>
        /// <param name="message">The raw message from the subscribed channel</param>
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

        /// <summary>
        /// Handles an update for an existing weapon
        /// </summary>
        /// <param name="update">The update object containing the netID, changeType and value</param>
        void handleWeaponUpdate(dynamic update)
        {
            string changeType = update.changeType;
            int netID = update.netID;
            var weapon = Weapons.FirstOrDefault(p => p.netID == netID);

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

        /// <summary>
        /// Handles an update for an existing weapon
        /// </summary>
        /// <param name="update">The update object containing the netID, changeType and value</param>
        void handleProjectileUpdate(dynamic update)
        {
            string changeType = update.changeType;
            int netID = update.netID;
            var projectile = Projectiles.FirstOrDefault(p => p.netID == netID);
            
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

        /// <summary>
        /// Checks if any of the players are using a banned accessory or armor item
        /// and will prevent any damage for a specified amount of time and teleport
        /// them to spawn if so.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
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
                            if (player.TPlayer.blackBelt)
                            {
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
                                    bool bannedArmor = EquipItems.Count(p => p.netID == player.TPlayer.armor[slot].netID && p.banned) > 0;
                                    bool bannedAccessory = EquipItems.Count(p => p.netID == player.TPlayer.armor[slot].netID && p.banned) > 0;

                                    if (bannedArmor || bannedAccessory)
                                    {
                                        string type = bannedArmor ? "Armor" : "Accessory";
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

        /// <summary>
        /// Passes on packets received to the DataHandlers to be processed if necessary
        /// </summary>
        /// <param name="args"></param>
        private void GetData(GetDataEventArgs args)
        {
            var type = args.MsgID;
            var player = Players[args.Msg.whoAmI];

            if (player == null)
            {
                args.Handled = true;
                return;
            }

            if (!player.TshockPlayer.ConnectionAlive)
            {
                args.Handled = true;
                return;
            }

            using (var data = new MemoryStream(args.Msg.readBuffer, args.Index, args.Length))
            {
                try
                {
                    if (GetDataHandler.HandlerGetData(type, player, data))
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
        /// <summary>
        /// Updates the config objet with the existing config file, or creates a new
        /// one if it doesn't exist
        /// </summary>
        /// <param name="e">The command args object from tshock containing information such as
        /// what player used the command, and the command parameters</param>
        void Reload(CommandArgs e)
        {
            Config.Reload(ConfigPath);

            foreach (var netID in Config.BannedArmorPieces)
            {
                EquipItems.Add(new EquipItem(netID, true));
            }

            e.Player.SendSuccessMessage("Reloaded PvPController config.");

            Weapons = Database.GetWeapons();
            Projectiles = Database.GetProjectiles();
        }

        /// <summary>
        /// Disposes of any registered hooks
        /// </summary>
        /// <param name="disposing"></param>
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

