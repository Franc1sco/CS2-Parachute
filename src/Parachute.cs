using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Modules.Admin;
using System.Text.Json.Serialization;

namespace Parachute;

public class ConfigGen : BasePluginConfig
{
    [JsonPropertyName("Enabled")] public bool Enabled { get; set; } = true;
    [JsonPropertyName("DecreaseVec")] public float DecreaseVec { get; set; } = 50;
    [JsonPropertyName("Linear")] public bool Linear { get; set; } = true;
    [JsonPropertyName("FallSpeed")] public float FallSpeed { get; set; } = 100;
    [JsonPropertyName("AccessFlag")] public string AccessFlag { get; set; } = "";
    [JsonPropertyName("TeleportTicks")] public int TeleportTicks { get; set; } = 300;
}

[MinimumApiVersion(43)]
public class Parachute : BasePlugin, IPluginConfig<ConfigGen>
{
    public override string ModuleName => "CS2 Parachute";
    public override string ModuleAuthor => "Franc1sco Franug";
    public override string ModuleVersion => "1.2";
    public ConfigGen Config { get; set; } = null!;
    public void OnConfigParsed(ConfigGen config) { Config = config; }

    private readonly Dictionary<int?, bool> bUsingPara = new();
    private readonly Dictionary<int?, int> gParaTicks = new();

    public override void Load(bool hotReload)
    {
        if (!Config.Enabled)
        {
            Console.WriteLine("[Parachute] Plugin not enabled!");
            return;
        }

        if (hotReload)
        {
            Utilities.GetPlayers().ForEach(player =>
            {
                bUsingPara.Add(player.UserId, false);
                gParaTicks.Add(player.UserId, 0);
            });
        }

        //AddTimer(0.0f, TimerParachute, TimerFlags.REPEAT);

        RegisterEventHandler<EventPlayerConnectFull>((@event, info) =>
        {
            var player = @event.Userid;

            if (player.IsBot || !player.IsValid)
            {
                return HookResult.Continue;

            } else {
                bUsingPara.Add(player.UserId, false);
                gParaTicks.Add(player.UserId, 0);
                return HookResult.Continue;
            }
        });

        RegisterEventHandler<EventPlayerDisconnect>((@event, info) =>
        {
            var player = @event.Userid;

            if (player == null || player.IsBot || !player.IsValid)
            {
                return HookResult.Continue;

            } else {
                if (bUsingPara.ContainsKey(player.UserId))
                {
                    bUsingPara.Remove(player.UserId);
                }
                if (gParaTicks.ContainsKey(player.UserId))
                {
                    gParaTicks.Remove(player.UserId);
                }
                return HookResult.Continue;
            }
        });

        
        RegisterListener<Listeners.OnTick>(() =>
        {
            var players = Utilities.GetPlayers();

            foreach (var player in players)
            {
                if (player != null 
                && player.IsValid 
                && !player.IsBot 
                && player.PawnIsAlive 
                && (Config.AccessFlag == "" || AdminManager.PlayerHasPermissions(player, Config.AccessFlag)))
                {
                    var buttons = player.Buttons;
                    if ((buttons & PlayerButtons.Use) != 0 && !player.PlayerPawn.Value.OnGroundLastTick)
                    {
                        StartPara(player);

                    } else if (bUsingPara[player.UserId]){
                        bUsingPara[player.UserId] = false;
                        StopPara(player);
                    }
                }
            }
        });
    }

    private void StopPara(CCSPlayerController player)
    {
        player.GravityScale = 1.0f;
        gParaTicks[player.UserId] = 0;
    }

    private void StartPara(CCSPlayerController player)
    {
        if (!bUsingPara[player.UserId])
        {
            bUsingPara[player.UserId] = true;
            player.GravityScale = 0.1f;
        }

        var fallspeed = Config.FallSpeed * (-1.0f);
        var isFallSpeed = false;
        var velocity = player.PlayerPawn.Value.AbsVelocity;
        if (velocity.Z >= fallspeed)
        {
            isFallSpeed = true;
        }

        if (velocity.Z < 0.0f)
        {
            if (isFallSpeed && Config.Linear || Config.DecreaseVec == 0.0) {
                velocity.Z = fallspeed;

            } else {
                velocity.Z = velocity.Z + Config.DecreaseVec;
            }

            var position = player.PlayerPawn.Value.AbsOrigin!;
            var angle = player.PlayerPawn.Value.AbsRotation!;

            if (gParaTicks[player.UserId] > Config.TeleportTicks)
            {
                player.Teleport(position, angle, velocity);
                gParaTicks[player.UserId] = 0;
            }

            ++gParaTicks[player.UserId];
        }
    }
}

