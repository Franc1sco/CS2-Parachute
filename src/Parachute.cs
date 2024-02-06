using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Entities.Constants;
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
    [JsonPropertyName("ParachuteModelEnabled")] public bool ParachuteModelEnabled { get; set; } = false;
    [JsonPropertyName("ParachuteModel")] public string ParachuteModel { get; set; } = "models/props_survival/parachute/chute.vmdl";
}

[MinimumApiVersion(139)]
public class Parachute : BasePlugin, IPluginConfig<ConfigGen>
{
    public override string ModuleName => "CS2 Parachute";
    public override string ModuleAuthor => "Franc1sco Franug";
    public override string ModuleVersion => "1.4";


    public ConfigGen Config { get; set; } = null!;
    public void OnConfigParsed(ConfigGen config) { Config = config; }

    private readonly Dictionary<int?, bool> bUsingPara = new();
    private readonly Dictionary<int?, int> gParaTicks = new();
    private readonly Dictionary<int?, CBaseEntity?> gParaModel = new();

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
                gParaModel.Add(player.UserId, null);
            });
        }

        if (Config.ParachuteModelEnabled)
        {
            RegisterListener<Listeners.OnMapStart>(map =>
            {
                Server.PrecacheModel(Config.ParachuteModel);
            });
        }

        RegisterEventHandler<EventPlayerConnectFull>((@event, info) =>
        {
            var player = @event.Userid;

            if (player.IsBot || !player.IsValid)
            {
                return HookResult.Continue;

            }
            else
            {
                bUsingPara.Add(player.UserId, false);
                gParaTicks.Add(player.UserId, 0);
                gParaModel.Add(player.UserId, null);
                return HookResult.Continue;
            }
        });

        RegisterEventHandler<EventPlayerDisconnect>((@event, info) =>
        {
            var player = @event.Userid;

            if (player == null || player.IsBot || !player.IsValid)
            {
                return HookResult.Continue;

            }
            else
            {
                if (bUsingPara.ContainsKey(player.UserId))
                {
                    bUsingPara.Remove(player.UserId);
                }
                if (gParaTicks.ContainsKey(player.UserId))
                {
                    gParaTicks.Remove(player.UserId);
                }
                if (gParaModel.ContainsKey(player.UserId))
                {
                    gParaModel.Remove(player.UserId);
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

                    } 
                    else if (bUsingPara[player.UserId])
                    {
                        bUsingPara[player.UserId] = false;
                        StopPara(player);
                    }
                }
            }
        });

        RegisterEventHandler<EventPlayerDeath>((@event, info) =>
        {
            var player = @event.Userid;

            if (bUsingPara[player.UserId])
            {
                bUsingPara[player.UserId] = false;
                StopPara(player);
            }
            return HookResult.Continue;
        }, HookMode.Pre);

    }

    private void StopPara(CCSPlayerController player)
    {
        player.GravityScale = 1.0f;
        gParaTicks[player.UserId] = 0;
        if (gParaModel[player.UserId] != null && gParaModel[player.UserId].IsValid)
        {
            gParaModel[player.UserId].Remove();
            gParaModel[player.UserId] = null;
        }
    }

    private void StartPara(CCSPlayerController player)
    {
        if (!bUsingPara[player.UserId])
        {
            bUsingPara[player.UserId] = true;
            player.GravityScale = 0.1f;
            if (Config.ParachuteModelEnabled)
            {
                var entity = Utilities.CreateEntityByName<CBaseProp>("prop_dynamic_override");
                if (entity != null && entity.IsValid)
                {
                    entity.SetModel(Config.ParachuteModel);
                    entity.MoveType = MoveType_t.MOVETYPE_NOCLIP;
                    entity.Collision.CollisionGroup = (byte)CollisionGroup.COLLISION_GROUP_NONE;
                    entity.Collision.CollisionAttribute.CollisionGroup = (byte)CollisionGroup.COLLISION_GROUP_NONE;
                    entity.DispatchSpawn();

                    gParaModel[player.UserId] = entity;
                }
            }
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
            if (isFallSpeed && Config.Linear || Config.DecreaseVec == 0.0)
            {
                velocity.Z = fallspeed;

            }
            else
            {
                velocity.Z = velocity.Z + Config.DecreaseVec;
            }

            var position = player.PlayerPawn.Value.AbsOrigin!;
            var angle = player.PlayerPawn.Value.AbsRotation!;

            if (gParaTicks[player.UserId] > Config.TeleportTicks)
            {
                player.Teleport(position, angle, velocity);
                gParaTicks[player.UserId] = 0;
            }

            if (gParaModel[player.UserId] != null && gParaModel[player.UserId].IsValid)
            {
                gParaModel[player.UserId].Teleport(position, angle, velocity);
            }

            ++gParaTicks[player.UserId];
        }
    }
}

