using System;
using System.Linq;
using Newtonsoft.Json.Linq;
using StackExchange.Redis;

namespace PvPController
{
    internal class Synchroniser
    {
        private PvPController Controller;
        private ConnectionMultiplexer Redis;

        internal Synchroniser(PvPController controller)
        {
            Controller = controller;
            this.SetupRedis();
        }


        /// <summary>
        /// Connects to the redis server for modification updates
        /// </summary>
        private void SetupRedis()
        {
            Redis = ConnectionMultiplexer.Connect(Controller.Config.RedisHost);
            ISubscriber sub = Redis.GetSubscriber();
            sub.SubscribeAsync("pvpcontroller-updates", (channel, message) =>
            {
                Console.WriteLine(message);
                parseUpdate(message);
            });
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

            switch (objectType)
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
            var weapon = Controller.Weapons.FirstOrDefault(p => p.netID == netID);

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
                case "maxDamage":
                    weapon.maxDamage = Convert.ToInt32(update.value);
                    break;
                case "minDamage":
                    weapon.minDamage = Convert.ToInt32(update.value);
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
            var projectile = Controller.Projectiles.FirstOrDefault(p => p.netID == netID);

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
    }
}
