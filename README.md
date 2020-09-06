# PvP Controller
This project aims to provide finer control over Terraria's PvP for a server running TSAPI and TShock.

## Terraria 1.3
The state of this repo is built for Terraria v1.3.5.3. As the interest in this plugin by 3rd parties remains low and it isn't written in a way that makes it easy for other servers to use, Dark Gaming has decided to continue future development in a private repo so we can focus on the features and setup that works best for us. This repo with code is left up for other developers who want a reference point for certain features or any developers who want to alter it for their own use.

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
 * The damage controller (which facilitates modified weapon damage) is not included
 * This plugin by default uses MongoDB and has no option for SQLite/MySQL

## Development
You will need to build/grab a release for [https://github.com/popstarfreas/PacketFactory]. You can use NuGet to get the mongodb dependencies.
