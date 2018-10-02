# PvP Controller
This project aims to provide finer control over Terraria's PvP for a server running TSAPI and TShock.

## Features
 * Damage modifications for weapons
 * Projectile velocity modifications for weapons
 * Server-sided health
 * Teleportation disability
 * Spectator mode
 * Prefix trimming on armor
 * Armor/Accessory banning
 * Server-sided damage (Damage is calculated by the server)
 * Trimming of duplicate accessories (works to provide the above point with correct damage)
 * Potion heal control amount and cooldown (Does not currently alter debuff time)
 
## Please Note
 * This plugin does not provide any commands for altering settings outside of the Config. No repo currently exists for the Web Controller, which provides a web interface for modifying weapons.
 * The damage controller (which facilitates modified weapon damage) is a separate module

## Development
You will need to build/grab a release for [https://github.com/popstarfreas/PacketFactory]. You can use NuGet to get the mongodb dependencies.
