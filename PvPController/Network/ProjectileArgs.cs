using Microsoft.Xna.Framework;

namespace PvPController.Network
{
    internal struct ProjectileArgs
    {
        public int Ident;
        public int Owner;
        public int Type;
        public int Damage;
        public Vector2 Velocity;
        public Vector2 Position;
        public float Ai0;
        public float Ai1;

        public ProjectileArgs(int ident, int owner, int type, int damage, Vector2 velocity, Vector2 position, float ai0, float ai1)
        {
            Ident = ident;
            Owner = owner;
            Type = type;
            Damage = damage;
            Velocity = velocity;
            Position = position;
            Ai0 = ai0;
            Ai1 = ai1;
        }
    }
}
