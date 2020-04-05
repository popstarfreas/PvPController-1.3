using System;
using System.IO;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;
using System.Timers;
using System.Collections.Generic;
using System.Linq;
using PvPController.StorageTypes;
using PvPController.Controllers;

namespace PvPController
{
    [ApiVersion(2, 0)]
    public class PvPController : TerrariaPlugin
    {
        // Events for other plugins
        public delegate void PlayerKillHandler(object sender, PlayerKillEventArgs e);
        public static event PlayerKillHandler OnPlayerKill;

        public delegate void PlayerDamageHandler(object sender, PlayerDamageEventArgs e);
        public static event PlayerDamageHandler OnPlayerDamage;
        public DateTime[] LastMessage = new DateTime[256];

        public Timer OnSecondUpdate;
        private GetDataHandlers GetDataHandler;
        private string ConfigPath;
        public Config Config;
        public Database Database;
        private Synchroniser Synchroniser;

        public List<Weapon> Weapons = new List<Weapon>();
        public List<StorageTypes.Projectile> Projectiles = new List<StorageTypes.Projectile>();
        public List<EquipItem> EquipItems = new List<EquipItem>();
        public Player[] Players = new Player[256];

        public static PvPController ActiveInstance;

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
            ActiveInstance = this;
        }
        
        /// <summary>
        /// Registers the appropriate hooks, loads the existing config or write one if not, and then starts the one second update timer.
        /// </summary>
        public override void Initialize()
        {
            ServerApi.Hooks.NetGetData.Register(this, GetData);
            ServerApi.Hooks.ServerJoin.Register(this, ServerJoin);
            ServerApi.Hooks.ServerLeave.Register(this, ServerLeave);
            ServerApi.Hooks.ServerChat.Register(this, ServerChat);
            ServerApi.Hooks.NetSendData.Register(this, SendData);
            Commands.ChatCommands.Add(new Command("pvpcontroller.reload", Reload, "pvpcreload"));
            Commands.ChatCommands.Add(new Command("pvpcontroller.spectate", ToggleSpectate, "spectate"));
            SetupConfig();

            foreach (var netID in Config.BannedArmorPieces)
            {
                EquipItems.Add(new EquipItem(netID, true));
            }

            if (Config.UseDatabase)
            {
                SetupDatabase();
            }

            if (Config.UseRedis)
            {
                Synchroniser = new Synchroniser(this);
            }

            GetDataHandler = new GetDataHandlers(this);
            SetupUpdateTimer();
            SetupDuplicateEquipPrevention();
        }

        /// <summary>
        /// Sets up the Config class using the file if it exists
        /// </summary>
        private void SetupConfig()
        {
            ConfigPath = Path.Combine(TShock.SavePath, "PvPController.json");
            Config = new Config(ConfigPath);
        }


        /// <summary>
        /// Connects to the database and loads all the required information
        /// </summary>
        private void SetupDatabase()
        {
            Database = new Database(Config);
            Weapons = Database.GetWeapons();
            Projectiles = Database.GetProjectiles();
            Database.addWeaponBuffs(Weapons);
        }

        /// <summary>
        /// Starts the update timer
        /// </summary>
        private void SetupUpdateTimer()
        {
            OnSecondUpdate = new Timer(1000);
            OnSecondUpdate.Enabled = true;
            OnSecondUpdate.Elapsed += SecondUpdate;
        }

        /// <summary>
        /// Initializes a new player object when someone joins
        /// </summary>
        /// <param name="args"></param>
        private void ServerJoin(JoinEventArgs args)
        {
            Players[args.Who] = new Player(TShock.Players[args.Who], this);
            Players[args.Who].IsDead = true;

            for (int i = 0; i < (int)(NetItem.ArmorSlots/2); i++)
            {
                if (Players[args.Who].TPlayer.armor[i].netID != 0
                    && EquipController.ShouldPreventEquip(Players[args.Who], Players[args.Who].TPlayer.armor[i], 59 + i))
                {
                    Players[args.Who].TPlayer.armor[i].SetDefaults(0);
                }
            }
        }

        /// <summary>
        /// Nulls the existing player object for the player index when someone leaves
        /// </summary>
        /// <param name="args"></param>
        private void ServerLeave(LeaveEventArgs args)
        {
            Players[args.Who] = null;
        }

        /// <summary>
        /// Prevents people using teleport commands in pvp
        /// </summary>
        /// <param name="args"></param>
        private void ServerChat(ServerChatEventArgs args)
        {
            var player = Players[args.Who];
            if (player != null && Config.BanTeleportItems && player.TPlayer.hostile)
            {
                if (args.Text.StartsWith(TShock.Config.CommandSpecifier) || args.Text.StartsWith(TShock.Config.CommandSilentSpecifier))
                {
                    var command = args.Text.Substring(1).ToLower();
                    if (command.StartsWith("spawn") || command.StartsWith("tp") || command.StartsWith("warp") || command.StartsWith("home")
                      || command.StartsWith("left") || command.StartsWith("right") || command.StartsWith("up") || command.StartsWith("down"))
                    {
                        if (!player.TshockPlayer.HasPermission("pvpcontroller.teleportimmune"))
                        {
                            player.TshockPlayer.SendData(PacketTypes.Teleport, "", 0, player.Index, player.TshockPlayer.LastNetPosition.X, player.TshockPlayer.LastNetPosition.Y);
                            player.TshockPlayer.SetBuff(149, 60);
                            args.Handled = true;
                        }
                    }
                }
            }
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
        /// Toggles whether the player is spectating
        /// </summary>
        private void ToggleSpectate(CommandArgs args)
        {
            if (!Players[args.Player.Index].Spectating)
            {
                var secondsSinceSpectator = (DateTime.Now - Players[args.Player.Index].LastSpectating).TotalSeconds;
                if (secondsSinceSpectator < 30)
                {
                    args.Player.SendErrorMessage($"You cannot enable Spectator Mode for another {Math.Truncate(30 - secondsSinceSpectator)} seconds.");
                    return;
                }
            }

            Players[args.Player.Index].Spectating = !Players[args.Player.Index].Spectating;

            if (Players[args.Player.Index].Spectating)
            {
                Players[args.Player.Index].IsDead = true;
                args.Player.TPlayer.dead = true;
                args.Player.TPlayer.hostile = false;
                args.Player.TPlayer.position.X = 0;
                args.Player.TPlayer.position.Y = 0;
                FakePlayerDeath(args.Player);
                TSPlayer.All.SendMessage($"{args.Player.Name} has become a Spectator.", new Microsoft.Xna.Framework.Color(187, 144, 212));

                if (args.Parameters.Count > 0)
                {
                    var name = string.Join(" ", args.Parameters);
                    var player = TShock.Players.FirstOrDefault(p => p.Name == name) ?? TShock.Players.FirstOrDefault(p => p.Name.StartsWith(name));

                    if (player != null)
                    {
                        args.Player.Teleport(player.TPlayer.position.X, player.TPlayer.position.Y);
                    }
                }
            } else
            {
                Players[args.Player.Index].LastSpectating = DateTime.Now;
                args.Player.Spawn();
                TSPlayer.All.SendMessage($"{args.Player.Name} is now not a Spectator.", new Microsoft.Xna.Framework.Color(187, 144, 212));
            }
        }

        /// <summary>
        /// Fakes a player dying to everyone but themselves
        /// </summary>
        /// <param name="player">The player to die</param>
        private void FakePlayerDeath(TSPlayer player)
        {
            byte[] playerDeath = new PacketFactory()
                .SetType((short)PacketTypes.PlayerDeathV2)
                .PackByte((byte)player.Index)
                .PackByte(0)
                .PackInt16(0)
                .PackByte(0)
                .PackByte(1)
                .GetByteData();

            foreach (var plr in TShock.Players)
            {
                if (plr != null && plr.Index != player.Index)
                    plr.SendRawData(playerDeath);
            }
        }
        
        /// <summary>
        /// Prevents spectators being revealed to people who just joined
        /// </summary>
        /// <param name="args"></param>
        private void SendData(SendDataEventArgs args)
        {
            if (args.MsgId == PacketTypes.PlayerUpdate && Players[args.number] != null && Players[args.number].Spectating)
            {
                args.Handled = true;
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
            var players = Players.ToList();
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
                                player.TshockPlayer.SetBuff(149, 60);
                                player.TshockPlayer.SendMessage("ACCESSORY VIOLATION: Master Ninja Gear & Blackbelt are not allowed for PvP!", 217, 255, 0);
                            }
                            else
                            {
                                // Armor and Accessories (active) are 0-9
                                var bannedArmorAndAccessories = player.TPlayer.armor.Where(p => p != null && EquipItems.Count(e => e.netID == p.netID) > 0);
                                var item = bannedArmorAndAccessories.FirstOrDefault();

                                if (item != null)
                                {
                                    bool isArmor = item.headSlot > -1 || item.bodySlot > -1 || item.legSlot > -1;
                                    string type = isArmor ? "Armor" : "Accessory";
                                    player.TPlayer.hostile = false;
                                    player.TshockPlayer.SetBuff(149, 60);
                                    player.TshockPlayer.SendMessage($"ACCESSORY VIOLATION: {type} {item.Name} is not allowed for PvP!", 217, 255, 0);
                                }
                            }
                            
                            if (Config.BanPrefixedArmor)
                            {
                                player.CheckArmorAndEnforce(GetDataHandler);
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

        #region temporaryDuplicateEquip
        private void SetupDuplicateEquipPrevention()
        {
            EquipController.Controllers.Add(this.ShouldPreventEquip);
        }

        private bool ShouldPreventEquip(Player player, Item equip, int slotId)
        {
            bool shouldPreventEquip = false;
            for (int i = 0; i < (int)(NetItem.ArmorSlots / 2); i++)
            {
                if (i != slotId - NetItem.InventorySlots && player.TPlayer.armor[i].netID == equip.netID)
                {
                    shouldPreventEquip = true;
                    break;
                }
            }

            if (!shouldPreventEquip)
            {
                if (equip.headSlot > -1)
                {
                    shouldPreventEquip = slotId != 59;
                }
                else if (equip.bodySlot > -1)
                {
                    shouldPreventEquip = slotId != 60;
                }
                else if (equip.legSlot > -1)
                {
                    shouldPreventEquip = slotId != 61;
                }
            }

            return shouldPreventEquip;
        }

        #endregion
        
        /// <summary>
        /// Updates the config object with the existing config file, or creates a new
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

            if (Config.UseDatabase)
            {
                Weapons = Database.GetWeapons();
                Projectiles = Database.GetProjectiles();
            }
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
                ServerApi.Hooks.ServerJoin.Deregister(this, ServerJoin);
                ServerApi.Hooks.ServerChat.Deregister(this, ServerChat);
                base.Dispose(disposing);
            }
        }
    }
}

