using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Logging;

using System.Runtime.CompilerServices;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using System.Runtime.InteropServices;

namespace HelloWorldPlugin;

public class HelloWorldPlugin : BasePlugin
{
    public override string ModuleName => "Hello World Plugin";
    public override string ModuleVersion => "0.0.1";

    public required IRunCommand RunCommand { get; set; }

    private int movementServices;
    private int movementPtr;

    public override void Load(bool hotReload)
    {
        RegisterEventHandler<EventWeaponFire>((@event, info) => {
            Logger.LogInformation($"Weapon Fired");

            return HookResult.Continue;
        });

        // check if runtime is windows or linux
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            movementServices = 3;
            movementPtr = 2;
            RunCommand = new RunCommandWindows();
        }
        else
        {
            movementServices = 0;
            movementPtr = 1;
            RunCommand = new RunCommandLinux();
        }

        RunCommand.Hook(OnRunCommand, HookMode.Pre);
    }


    private HookResult OnRunCommand(DynamicHook h)
    {
        //var player = h.GetParam<CCSPlayer_MovementServices>(0).Pawn.Value.Controller.Value?.As<CCSPlayerController>(); // Linux
        var player = h.GetParam<CCSPlayer_MovementServices>(movementServices).Pawn.Value.Controller.Value?.As<CCSPlayerController>(); // Windows
        if (!player.IsPlayer())
            return HookResult.Continue;

        //var userCmd = new CUserCmd(h.GetParam<IntPtr>(1)); // Linux
        var userCmd = new CUserCmd(h.GetParam<IntPtr>(movementPtr)); // Windows
        var getMovementButton = userCmd.GetMovementButton();

        var movementButtons = string.Join(", ", getMovementButton);
        Logger.LogInformation($"Movement Buttons: {movementButtons}");

        if (getMovementButton.Contains("Left Click"))
        {
            // cancel the shot if the player is holding the left click
            // SetLeftClick(h.GetParam<IntPtr>(1)); // Linux
            SetLeftClick(h.GetParam<IntPtr>(2)); // Windows
        }

        return HookResult.Changed;
    }

    private unsafe void SetLeftClick(IntPtr userCmd)
    {
        Unsafe.Write((void*)(userCmd + 0x50), Unsafe.Read<IntPtr>((void*)(userCmd + 0x50)) ^ 1);
    }

    public override void Unload(bool hotReload)
    {
        base.Unload(hotReload);
        RunCommand.Unhook(OnRunCommand, HookMode.Pre);
    }
}

public class CUserCmd
{
    private IntPtr Handle { get; set; }
    private Dictionary<Int64, string> buttonNames = new Dictionary<Int64, string>
    {
        {1, "Left Click"},
        {2, "Jump"},
        {4, "Crouch"},
        {8, "Forward"},
        {16, "Backward"},
        {32, "Use"},
        // 64 ??
        {128, "Turn Left"},
        {256, "Turn Right"},
        {512, "Left"},
        {1024, "Right"},
        {2048, "Right Click"},
        {8192, "Reload"},
        // 16384 ??
        // 32768 ??
        {65536, "Shift"},
        /* 
        131072 ??
        262144 ??
        524288 ??
        1048576 ??
        2097152 ??
        4194304 ??
        8388608 ??
        16777216 ??
        33554432 ??
        67108864 ??
        134217728 ??
        268435456 ??
        536870912 ??
        1073741824 ??
        2147483648 ??
        4294967296 ?? 
        */
        {8589934592, "Scoreboard"},
        {34359738368, "Inspect"}
    };
    public CUserCmd(IntPtr pointer)
    {
        Handle = pointer;
    }

    // we want to return a list and a nint
    public unsafe List<String> GetMovementButton()
    {
        if (Handle == IntPtr.Zero)
            return ["None"];

        nint moveMent = Unsafe.Read<IntPtr>((void*)(Handle + 0x50));
        
        // System.Console.WriteLine(moveMent); // Use this to see the value of the button you are pressing

        var binary = Convert.ToString(moveMent, 2);
        binary = binary.PadLeft(64, '0');
        
        var movementButtons = new List<String>();

        movementButtons.Add(binary);

        foreach (var button in buttonNames)
        {
            if ((moveMent & button.Key) == button.Key)
            {
                movementButtons.Add(button.Value);
            }
        }

        return movementButtons;
    }
}
