using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Modules.Admin;
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
    [JsonPropertyName("ParachuteModelEnabled")] public bool ParachuteModelEnabled { get; set; } = true;
    [JsonPropertyName("ParachuteModel")] public string ParachuteModel { get; set; } = "models/props_survival/parachute/chute.vmdl";
}

[MinimumApiVersion(179)]
public class Parachute : BasePlugin, IPluginConfig<ConfigGen>
{
    public override string ModuleName => "CS2 Parachute";
    public override string ModuleAuthor => "Franc1sco Franug";
    public override string ModuleVersion => "1.5.1";


    public ConfigGen Config { get; set; } = null!;
    public void OnConfigParsed(ConfigGen config) { Config = config; }

    private readonly Dictionary<int, bool> bUsingPara = new();
    private readonly Dictionary<int, int> gParaTicks = new();
    private readonly Dictionary<int, CBaseEntity?> gParaModel = new();

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
                bUsingPara.Add((int)player.Index, false);
                gParaTicks.Add((int)player.Index, 0);
                gParaModel.Add((int)player.Index, null);
            });
        }

        if (Config.ParachuteModelEnabled)
        {
            RegisterListener<Listeners.OnServerPrecacheResources>((manifest) =>
            {
                 manifest.AddResource(Config.ParachuteModel);

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
                bUsingPara.Add((int)player.Index, false);
                gParaTicks.Add((int)player.Index, 0);
                gParaModel.Add((int)player.Index, null);
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
                if (bUsingPara.ContainsKey((int)player.Index))
                {
                    bUsingPara.Remove((int)player.Index);
                }
                if (gParaTicks.ContainsKey((int)player.Index))
                {
                    gParaTicks.Remove((int)player.Index);
                }
                if (gParaModel.ContainsKey((int)player.Index))
                {
                    gParaModel.Remove((int)player.Index);
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
                    if ((buttons & PlayerButtons.Use) != 0 && !player.PlayerPawn.Value!.OnGroundLastTick)
                    {
                        StartPara(player);

                    } 
                    else if (bUsingPara[(int)player.Index])
                    {
                        bUsingPara[(int)player.Index] = false;
                        StopPara(player);
                    }
                }
            }
        });

        RegisterEventHandler<EventPlayerDeath>((@event, info) =>
        {
            var player = @event.Userid;

            if (bUsingPara.ContainsKey((int)player.Index) && bUsingPara[(int)player.Index])
            {
                bUsingPara[(int)player.Index] = false;
                StopPara(player);
            }
            return HookResult.Continue;
        }, HookMode.Pre);

    }

    private void StopPara(CCSPlayerController player)
    {
        player.GravityScale = 1.0f;
        gParaTicks[(int)player.Index] = 0;
        if (gParaModel[(int)player.Index] != null && gParaModel[(int)player.Index]!.IsValid)
        {
            gParaModel[(int)player.Index]?.Remove();
            gParaModel[(int)player.Index] = null;
        }
    }

    private void StartPara(CCSPlayerController player)
    {
        if (!bUsingPara[(int)player.Index])
        {
            bUsingPara[(int)player.Index] = true;
            player.GravityScale = 0.1f;
            if (Config.ParachuteModelEnabled)
            {
                var entity = Utilities.CreateEntityByName<CDynamicProp>("prop_dynamic_override");
                if (entity != null && entity.IsValid)
                {
                    entity.MoveType = MoveType_t.MOVETYPE_NOCLIP;
                    entity.Collision.CollisionGroup = (byte)CollisionGroup.COLLISION_GROUP_NONE;
                    entity.Collision.CollisionAttribute.CollisionGroup = (byte)CollisionGroup.COLLISION_GROUP_NONE;
                    // entity.RenderMode = RenderMode_t.kRenderNormal; // shadows test - dont work
                    entity.DispatchSpawn();

                    entity.SetModel(Config.ParachuteModel);
                    //entity.ShadowStrength = 1.0f; // shadows test - dont work
                    //entity.ShapeType = 0; // shadows test - dont work
                    //entity.AcceptInput("EnableShadow"); // shadows test - dont work
                    //entity.AcceptInput("EnableReceivingFlashlight"); // shadows test - dont work

                    // CBaseEntity_SetParent(entity, player); // need fix
                    gParaModel[(int)player.Index] = entity;
                }
            }
        }

        var fallspeed = Config.FallSpeed * (-1.0f);
        var isFallSpeed = false;
        var velocity = player.PlayerPawn.Value?.AbsVelocity;
        if (velocity?.Z >= fallspeed)
        {
            isFallSpeed = true;
        }

        if (velocity?.Z < 0.0f)
        {
            if (isFallSpeed && Config.Linear || Config.DecreaseVec == 0.0)
            {
                velocity.Z = fallspeed;

            }
            else
            {
                velocity.Z = velocity.Z + Config.DecreaseVec;
            }

            var position = player.PlayerPawn.Value?.AbsOrigin!;
            var angle = player.PlayerPawn.Value?.AbsRotation!;

            if (gParaTicks[(int)player.Index] > Config.TeleportTicks)
            {
                player.Teleport(position, angle, velocity);
                gParaTicks[(int)player.Index] = 0;
            }

            if (gParaModel[(int)player.Index] != null && gParaModel[(int)player.Index]!.IsValid)
            {
                gParaModel[(int)player.Index]?.Teleport(position, angle, velocity);
            }

            ++gParaTicks[(int)player.Index];
        }
    }

    /* // dont work not sure why
    public static string setParentFuncWindowsSig = @"\x4D\x8B\xD9\x48\x85\xD2\x74\x2A";
    public static string setParentFuncLinuxSig = @"\x48\x85\xF6\x74\x2A\x48\x8B\x47\x10\xF6\x40\x31\x02\x75\x2A\x48\x8B\x46\x10\xF6\x40\x31\x02\x75\x2A\xB8\x2A\x2A\x2A\x2A";

    private static MemoryFunctionVoid<CBaseEntity, CBaseEntity, CUtlStringToken?, matrix3x4_t?> CBaseEntity_SetParentFunc
        = new(RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? setParentFuncLinuxSig : setParentFuncWindowsSig);

    public static void CBaseEntity_SetParent(CBaseEntity childrenEntity, CBaseEntity parentEntity)
    {
        if (!childrenEntity.IsValid || !parentEntity.IsValid) return;

        var origin = parentEntity.AbsOrigin;
        var angle = parentEntity.AbsRotation!;
        CBaseEntity_SetParentFunc.Invoke(childrenEntity, parentEntity, null, null);
        // If not teleported, the childrenEntity will not follow the parentEntity correctly.
        childrenEntity.Teleport(origin, angle, new Vector(IntPtr.Zero));
        Console.WriteLine("CBaseEntity_SetParent() done!");
    }*/
}

