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
    [JsonPropertyName("AccessFlag")] public string AccessFlag { get; set; } = "@css/vip";
}

[MinimumApiVersion(43)]
public class Parachute : BasePlugin, IPluginConfig<ConfigGen>
{
    public override string ModuleName => "CS2 Parachute";
    public override string ModuleAuthor => "Franc1sco Franug";
    public override string ModuleVersion => "0.1.0";
    public ConfigGen Config { get; set; } = null!;
    public void OnConfigParsed(ConfigGen config) { Config = config; }

    private List<CCSPlayerController> connectedPlayers = new List<CCSPlayerController>();
    private readonly Dictionary<CCSPlayerController, bool> bUsingPara = new();

    public override void Load(bool hotReload)
    {
        if (!Config.Enabled)
        {
            Console.WriteLine("[Parachute] Plugin not enabled!");
            return;
        }

        if (hotReload)
        {
            Utilities.GetPlayers().ForEach(controller =>
            {
                connectedPlayers.Add(controller);
                bUsingPara[controller] = false;
                bUsingPara.Add(controller, false);
            });
        }

        RegisterEventHandler<EventPlayerConnectFull>((@event, info) =>
        {
            var player = @event.Userid;

            if (player.IsBot || !player.IsValid)
            {
                return HookResult.Continue;

            } else {
                bUsingPara.Add(player, false);
                connectedPlayers.Add(player);
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
                connectedPlayers.Remove(player);
                if (bUsingPara.ContainsKey(player))
                {
                    bUsingPara.Remove(player);
                }
                return HookResult.Continue;
            }
        });

        RegisterListener<Listeners.OnTick>(() =>
        {
            foreach (var player in connectedPlayers)
            {
                if (player.IsValid && !player.IsBot && player.PawnIsAlive && (Config.AccessFlag == "" || AdminManager.PlayerHasPermissions(player, Config.AccessFlag)))
                {
                    var buttons = player.Buttons;
                    if ((buttons & PlayerButtons.Use) != 0 && !player.PlayerPawn.Value.OnGroundLastTick)
                    {
                        bUsingPara[player] = true;
                        StartPara(player);

                    } else if (bUsingPara[player]){
                        bUsingPara[player] = false;
                        StopPara(player);
                    }
                }
            }
        });
    }

    public override void Unload(bool hotReload)
    {
        connectedPlayers.Clear();
        bUsingPara.Clear();
    }

    private void StopPara(CCSPlayerController player)
    {
        player.GravityScale = 1.0f;
    }

    private void StartPara(CCSPlayerController player)
    {
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
            player.Teleport(position, angle, velocity);
            player.GravityScale = 0.1f;
        }
    }
}

