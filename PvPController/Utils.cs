using Terraria;

namespace PvPController
{
    public static class Utils
    {
        public static bool IsBow(Item item)
        {
            bool bow = false;
            switch(item.name)
            {
                case "Wooden Bow":
                case "Boreal Wood Bow":
                case "Copper Bow":
                case "Palm Wood Bow":
                case "Rich Mahogany Bow":
                case "Tin Bow":
                case "Ebonwood Bow":
                case "Iron Bow":
                case "Shadewood Bow":
                case "Lead Bow":
                case "Pearlwood Bow":
                case "Silver Bow":
                case "Tungsten Bow":
                case "Gold Bow":
                case "Platinum Bow":
                case "Demon Bow":
                case "Tendon Bow":
                case "Hellwing Bow":
                case "The Bee's Knees":
                case "Molten Fury":
                case "Marrow":
                case "Daedalus Stormbow":
                case "Ice Bow":
                case "Shadowflame Bow":
                case "Phantasm":
                case "Tsunami":
                case "Pulse Bow":
                    bow = true;
                    break;
            }

            return bow;
        }
    }
}
