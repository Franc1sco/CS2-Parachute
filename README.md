# CS2-Parachute

Parachute function when you keep pressed E on the air. 

### Requirements
[CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp/)

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
  "ConfigVersion": 1
}
```
* Enable - Enable or disable the plugin.
* DecreaseVec - 0: dont use Realistic velocity-decrease - x: sets the velocity-decrease.
* Linear - 0: disables linear fallspeed - 1: enables it
* FallSpeed - speed of the fall when you use the parachute
* AccessFlag - access required for can use parachuse, leave blank "" for public access.

### TODO

Take ideas from https://forums.alliedmods.net/showthread.php?p=580269 like add option for buy a parachute.
