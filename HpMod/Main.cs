using System;
using System.IO;
using System.Timers;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;
using System.Diagnostics;

namespace AntiPvPWeapons
{
    [ApiVersion(1, 21)]
    public class AntiPvPWeapons : TerrariaPlugin
    {
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
                return "Takes the punch out of weapons.";
            }
        }

        public override string Name
        {
            get
            {
                return "Anti-PvP Weapons";
            }
        }

        public override Version Version
        {
            get
            {
                return new Version(1, 0, 0);
            }
        }

        public AntiPvPWeapons(Main game)
            : base(game)
        {
            Order = 3;
        }

        public override void Initialize()
        {
            ServerApi.Hooks.NetGetData.Register(this, GetData);
            string path = Path.Combine(TShock.SavePath, "AntiPvPWeapons.json");
            Commands.ChatCommands.Add(new Command("antipvpweapons.reload", Reload, "apwreload"));
            if (!File.Exists(path))
                Config.WriteTemplates(path);
            Config = Config.Read(path);
            GetDataHandlers.InitGetDataHandler();
        }
        
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
        void Reload(CommandArgs e)
        {
            string path = Path.Combine(TShock.SavePath, "AntiPvPWeapons.json");
            if (!File.Exists(path))
                Config.WriteTemplates(path);
            Config = Config.Read(path);
            e.Player.SendSuccessMessage("Reloaded AntiPvPWeapons config.");
        }

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

