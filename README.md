| What        | Location                                         |
| ----------- | ------------------------------------------------ |
| Local mods  | %appdata%\SpaceEngineers\Mods\{mod name}         |
| World saves | %appdata%\SpaceEngineers\Saves\{Your Steam ID}\  |

Files of note:

- Sandbox.sbc – contains world settings and libraries of player data, like GPS points and faction information.
- SANDBOX_0_0_0\_.sbs – detail definition of a world, positions and states of objects (we don’t have more details at this moment, but it should be pretty self-explanatory – run some experiments)
- .vx2 – voxel data for an asteroid or planet
- .xmlcache – a cache file which is regenerated on each save/reload. You do not need to modify this file to change a world, however it does need to be deleted if you make changes to SANDBOX_0_0_0\_.sbs.
- Textures can be found in: steamapps\common\SpaceEngineers\Content\Textures\

# Features

| Command | Arguments                                                | Description                                                                                |
| ------- | -------------------------------------------------------- | ------------------------------------------------------------------------------------------ |
| Search  | {near/far/all} {csv query}                               | Show the nearest, furthest, or all of the gps markers that match the comma separated query |
| Show    | {on/off}                                                 | Toggle off the current set of gps markers or on the last known set of active gps markers   |
| Color   | {red/orange/yellow/green/blue/indigo/violet} {csv query} | Color all the gps markers that match the csv query                                         |
