# CS2-Parachute

Parachute function when you keep pressed E on the air. 

### Requirements
* [CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp/) (version 179 or higher)

### Installation

Drag and drop from [releases](https://github.com/Franc1sco/CS2-Parachute/releases) to game/csgo/addons/counterstrikesharp/plugins

### Configuration

Configure the file parachute.json generated on addons/counterstrikesharp/configs/plugins/Parachute
```json
{
  "Enabled": true,
  "DecreaseVec": 50,
  "Linear": true,
  "FallSpeed": 100,
  "AccessFlag": "@css/vip",
  "TeleportTicks": 300,
  "ParachuteModelEnabled": true,
  "ConfigVersion": 1
}
```
* Enable - Enable or disable the plugin.
* DecreaseVec - 0: dont use Realistic velocity-decrease - x: sets the velocity-decrease.
* Linear - 0: disables linear fallspeed - 1: enables it
* FallSpeed - speed of the fall when you use the parachute
* TeleportTicks - 300: ticks until perform a teleport (for prevent console spam).
* ParachuteModelEnabled - true: Add a parachute model while using it.
* AccessFlag - access required for can use parachuse, leave blank "" for public access.
