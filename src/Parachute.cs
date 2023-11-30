using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using System.Text.Json;

namespace Parachute;


[MinimumApiVersion(43)]
public class Parachute : BasePlugin
{
    public override string ModuleName => "CS2 Parachute";
    public override string ModuleAuthor => "Franc1sco Franug";
    public override string ModuleVersion => "0.0.1";

    private List<CCSPlayerController> connectedPlayers = new List<CCSPlayerController>();
    private readonly Dictionary<CCSPlayerController, bool> bUsingPara = new();


    public static ConfigOptions? _configs = new();

    public override void Load(bool hotReload)
    {
        LoadConfigs();

        if (!_configs!.Enabled)
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
            });
        }

        RegisterEventHandler<EventPlayerConnectFull>((@event, info) =>
        {
            var player = @event.Userid;

            if (player.IsBot || !player.IsValid)
            {
                return HookResult.Continue;

            } else {
                connectedPlayers.Add(player);
                return HookResult.Continue;
            }
        });

        RegisterEventHandler<EventPlayerDisconnect>((@event, info) =>
        {
            var player = @event.Userid;

            if (player.IsBot || !player.IsValid)
            {
                return HookResult.Continue;

            } else {
                connectedPlayers.Remove(player);
                return HookResult.Continue;
            }
        });

        RegisterListener<Listeners.OnTick>(() =>
        {
            foreach (var player in connectedPlayers)
            {
                if (player.IsValid && !player.IsBot && player.PawnIsAlive)
                {
                    var buttons = player.Buttons;
                    if ((buttons & PlayerButtons.Use) != 0)
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
        player.GravityScale = 0.1f;
        var velocity = player.PlayerPawn.Value.AbsVelocity;

        if (velocity.Z >= 0.0f) { 
            return; 
        }
        velocity.Z = velocity.Z + (float)_configs!.DecreaseVec;
        var position = player.PlayerPawn.Value.AbsOrigin!;
        var angle = player.PlayerPawn.Value.AbsRotation!;
        player.Teleport(position, angle, velocity);
    }

    private void LoadConfigs()
    {
        string path = Path.Join(ModuleDirectory, "parachute.json");
        _configs = !File.Exists(path) ? CreateConfig(path, LoadConfig) : JsonSerializer.Deserialize<ConfigOptions>(File.ReadAllText(path));
    }

    private static T CreateConfig<T>(string path, Func<T> dataLoader)
    {
        var data = dataLoader();
        File.WriteAllText(path, JsonSerializer.Serialize(data,
            new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            }));

        return data;
    }

    private ConfigOptions LoadConfig()
    {
        return new ConfigOptions
        {
            Enabled = true,
            DecreaseVec = (float)10.0,
     
        };
    }

    public class ConfigOptions
    {
        public bool Enabled { get; init; }
        public float DecreaseVec { get; init; }
    }
}

