using System;
using System.IO;
using System.IO.Streams;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using TShockAPI;
using System.Diagnostics;

namespace AntiPvPWeapons
{
    internal delegate bool GetDataHandlerDelegate(GetDataHandlerArgs args);

    internal class GetDataHandlerArgs : EventArgs
    {
        public TSPlayer Player { get; private set; }
        public MemoryStream Data { get; private set; }

        public GetDataHandlerArgs(TSPlayer player, MemoryStream data)
        {
            Player = player;
            Data = data;
        }
    }

    internal static class GetDataHandlers
    {
        private static Dictionary<PacketTypes, GetDataHandlerDelegate> _getDataHandlerDelegates;

        public static void InitGetDataHandler()
        {
            _getDataHandlerDelegates = new Dictionary<PacketTypes, GetDataHandlerDelegate>
            {
                {PacketTypes.ProjectileNew, HandleProjectile},
                {PacketTypes.PlayerDamage, HandleDamage},
                {PacketTypes.PlayerUpdate, HandlePlayerUpdate}
            };
        }

        public static bool HandlerGetData(PacketTypes type, TSPlayer player, MemoryStream data)
        {
            GetDataHandlerDelegate handler;
            if (_getDataHandlerDelegates.TryGetValue(type, out handler))
            {
                try
                {
                    return handler(new GetDataHandlerArgs(player, data));
                }
                catch (Exception ex)
                {
                    TShock.Log.Error(ex.ToString());
                }
            }
            return false;
        }

        public static Stopwatch[] LastBannedUsage = new Stopwatch[256];
        public static DateTime[] LastMessage = new DateTime[256];

        private static bool HandleProjectile(GetDataHandlerArgs args)
        {
            var ident = args.Data.ReadInt16();
            var pos = new Vector2(args.Data.ReadSingle(), args.Data.ReadSingle());
            var vel = new Vector2(args.Data.ReadSingle(), args.Data.ReadSingle());
            var knockback = args.Data.ReadSingle();
            var dmg = args.Data.ReadInt16();
            var owner = args.Data.ReadInt8();
            var type = args.Data.ReadInt16();
            var bits = (BitsByte)args.Data.ReadInt8();
            owner = (byte)args.Player.Index;
            float[] ai = new float[Projectile.maxAI];

            if (AntiPvPWeapons.Config.BannedProjectileIDs.Contains(type) && args.Player.TPlayer.hostile)
            {
                    var proj = Main.projectile[ident];
                    proj.active = false;
                    proj.type = 0;
                    TSPlayer.All.SendData(PacketTypes.ProjectileDestroy, "", ident);
                    proj.owner = 255;
                    proj.active = false;
                    proj.type = 0;
                    TSPlayer.All.SendData(PacketTypes.ProjectileNew, "", ident);
                    if (LastBannedUsage[args.Player.Index] != null)
                    {
                        LastBannedUsage[args.Player.Index].Stop();
                        LastBannedUsage[args.Player.Index].Reset();
                    }
                    else
                    {
                        LastBannedUsage[args.Player.Index] = new Stopwatch();
                    }
                    LastBannedUsage[args.Player.Index].Start();

                    if ((DateTime.Now - LastMessage[args.Player.Index]).TotalSeconds > 2) {
                        args.Player.SendMessage("++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++", Color.Red);
                        args.Player.SendMessage("That weapon does not work in PVP. Using it will cause you to do no damage!", Color.Red);
                        args.Player.SendMessage("++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++", Color.Red);
                        LastMessage[args.Player.Index] = DateTime.Now;
                    }
                    return true;
            }
            return false;
        }

        private static bool HandlePlayerUpdate(GetDataHandlerArgs args)
        {
            byte plr = args.Data.ReadInt8();
            BitsByte control = args.Data.ReadInt8();
            BitsByte pulley = args.Data.ReadInt8();
            byte item = args.Data.ReadInt8();
            var pos = new Vector2(args.Data.ReadSingle(), args.Data.ReadSingle());
            var vel = Vector2.Zero;
            
            if (control[5] && AntiPvPWeapons.Config.BannedItemIDs.Contains(args.Player.SelectedItem.netID) && args.Player.TPlayer.hostile)
            {
                if (LastBannedUsage[args.Player.Index] != null)
                {
                    LastBannedUsage[args.Player.Index].Stop();
                    LastBannedUsage[args.Player.Index].Reset();
                }
                else
                {
                    LastBannedUsage[args.Player.Index] = new Stopwatch();
                }

                LastBannedUsage[args.Player.Index].Start();

                if ((DateTime.Now - LastMessage[args.Player.Index]).TotalSeconds > 2)
                {
                    args.Player.SendMessage("++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++", Color.Red);
                    args.Player.SendMessage("That weapon does not work in PVP. Using it will cause you to do no damage!", Color.Red);
                    args.Player.SendMessage("++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++", Color.Red);
                    LastMessage[args.Player.Index] = DateTime.Now;
                }
            }

            return false;
        }

        private static bool HandleDamage(GetDataHandlerArgs args)
        {
			if (args.Player == null) return false;
			var index = args.Player.Index;
			var playerId = (byte) args.Data.ReadByte();
			args.Data.ReadByte();
			var damage = args.Data.ReadInt16();
            var text = args.Data.ReadString();
			var crit = args.Data.ReadBoolean();
			args.Data.ReadByte();

            if (LastBannedUsage[args.Player.Index] != null && LastBannedUsage[args.Player.Index].IsRunning)
            {
                if (LastBannedUsage[args.Player.Index].Elapsed.TotalSeconds < AntiPvPWeapons.Config.DamageDisableSeconds)
                {
                    args.Player.SendData(PacketTypes.PlayerHp, "", playerId);
                    return true;
                }
                else
                {
                    LastBannedUsage[args.Player.Index].Stop();
                }
            }
            return false;
        }
    }
}