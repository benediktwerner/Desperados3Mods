# Desperados III Mods

Collection of my mods for the game Desperados III.

You can find me (1vader#0203) on the official [Desperados III](https://discord.gg/gDFNGzx) and [Mimimi](https://discord.gg/69ZxNTu) Discord servers for bug reports or ideas for improvements or new mods. Or you can just create an issue here.

## Mods Overview

- **Convenience**: Gives the options to auto-mute the game when in the background and starting levels on full zoom level and/or with highlights enabled.
- **D1CooperGun**: Makes Cooper's gun work more like in Desperados 1. The two guns now work like one, together having six quick shots after which a long reload is required. You have infinite reloads and can still use double shot. All the parameters (reload ammo and reload time) are tuneable.
- **DevKillsList**: Shows you which devs you still need to kill for the Veteran Bounty Hunter achievement. (Requires `BepInEx.ConfigurationManager`)
- **ExtendedCheats**: Collection of random cheats like infinite ammo or modifying ability ranges.
- **KingsmanEasterEgg**: Allows you to modify the chance that a snipe with Doc will trigger the [exploding heads easter egg](https://desperados.fandom.com/wiki/Desperados_III_Easter_Eggs#Exploding_Heads). Usually the chance for this is 0.1% so most people will probably never see it without this.
- **ShowdownModePauseOnDesperadoDiff**: Allows you to enable pausing Showdown mode on Desperado difficulty.

## Installation

Download BepInEx version 5.4 from its [releases page](https://github.com/BepInEx/BepInEx/releases) and extract it in your games installation directory. You should have a directory called `BepInEx` right next to the `Desperados III.exe`. See also [Installing BepInEx](https://bepinex.github.io/bepinex_docs/master/articles/user_guide/installation/index.html).

Next, download any mods you want to use and place them in the `BepInEx/plugins` directory.

You can download the mods from the [releases page](https://github.com/benediktwerner/Desperados3Mods/releases) or using the links below.

To configure the mods you need to launch the game at least once after installing them. Then you can edit the configuration files in `BepInEx/config`.

Alternatively, you can install the [BepInEx.ConfigurationManager](https://github.com/BepInEx/BepInEx.ConfigurationManager) (again by downloading the files and placing them inside the `BepInEx/plugins` directory). This allows you to bring up an in-game mod settings menu by pressing `F1`. This is also required for the `DevKillsList` mod since it displays the list inside this UI. You probably want to install [RewiredBlocker](https://github.com/benediktwerner/RewiredBlocker) together with the ConfigurationManager to block the game from picking up on your input in the background when the settings menu is open.

### Direct Download Links

- **Convenience**: [Convenience.dll v1.0.1](https://github.com/benediktwerner/Desperados3Mods/releases/download/cheats-v1.1.0/Convenience.dll)
- **D1CooperGun**: [D1CooperGun.dll v1.0](https://github.com/benediktwerner/Desperados3Mods/releases/download/v1.0.0/D1CooperGun.dll)
- **DevKillsList**: [DevKillsList.dll v1.0.1](https://github.com/benediktwerner/Desperados3Mods/releases/download/cheats-v1.1.0/DevKillsList.dll)
- **ExtendedCheats**: [ExtendedCheats.dll v1.1](https://github.com/benediktwerner/Desperados3Mods/releases/download/cheats-v1.1.0/ExtendedCheats.dll)
- **KingsmanEasterEgg**: [KingsmanEasterEgg.dll v1.0](https://github.com/benediktwerner/Desperados3Mods/releases/download/v1.0.0/KingsmanEasterEgg.dll)
- **ShowdownModePauseOnDesperadoDiff**: [ShowdownModePauseOnDesperadoDiff.dll v1.0](https://github.com/benediktwerner/Desperados3Mods/releases/download/v1.0.0/ShowdownModePauseOnDesperadoDiff.dll)
