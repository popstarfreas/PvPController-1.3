using System;
using System.IO;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;
using System.Timers;
using System.Diagnostics;
using System.Linq;

namespace PvPController
{
    [ApiVersion(1, 25)]
    public class PvPController : TerrariaPlugin
    {
        public Timer OnSecondUpdate;
        public static Config Config = new Config();
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
                return "Takes the punch out of weapons and blesses others.";
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
                return new Version(1, 2, 0);
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
            string path = Path.Combine(TShock.SavePath, "PvPController.json");
            Commands.ChatCommands.Add(new Command("pvpcontroller.reload", Reload, "pvpcreload"));
            if (!File.Exists(path))
                Config.WriteTemplates(path);
            Config = Config.Read(path);
            GetDataHandlers.InitGetDataHandler();

            OnSecondUpdate = new Timer(1000);
            OnSecondUpdate.Enabled = true;
            OnSecondUpdate.Elapsed += SecondUpdate;
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
                                    bool bannedArmor = Config.BannedArmorPieces.Contains(player.TPlayer.armor[slot].netID);
                                    bool bannedAccessory = Config.BannedAccessories.Contains(player.TPlayer.armor[slot].netID);

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

                            if (violation)
                            {
                                if (GetDataHandlers.LastBannedUsage[player.Index] != null)
                                {
                                    GetDataHandlers.LastBannedUsage[player.Index].Stop();
                                    GetDataHandlers.LastBannedUsage[player.Index].Reset();
                                }
                                else
                                {
                                    GetDataHandlers.LastBannedUsage[player.Index] = new Stopwatch();
                                }

                                GetDataHandlers.LastBannedUsage[player.Index].Start();
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
            e.Player.SendSuccessMessage("Reloaded PvPController config.");
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

