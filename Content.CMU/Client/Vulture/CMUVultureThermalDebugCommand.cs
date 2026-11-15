using Content.Client.CMU14.Vulture;
using Robust.Client.Graphics;
using Robust.Shared.Console;

namespace Content.Client.CMU14.Vulture;

/// <summary>
///     Switches the Vulture thermal picture between its layers to find out which one is wrong. Client-side only.
/// </summary>
public sealed class CMUVultureThermalDebugCommand : IConsoleCommand
{
    [Dependency] private IOverlayManager _overlays = default!;

    public string Command => "vulturethermal";
    public string Description => "Switches the Vulture thermal picture between its layers, or prints what it sees.";

    public string Help =>
        "vulturethermal [mode]\n" +
        "  0  full picture\n" +
        "  1  plain scope view, thermal switched off\n" +
        "  2  grey backdrop only\n" +
        "  3  heat silhouettes and crosshair only\n" +
        "With no argument it prints the current mode and what the scope window contains. " +
        "Look through the thermal scope first.";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (!_overlays.TryGetOverlay<CMUVultureThermalOverlay>(out var overlay))
        {
            shell.WriteError("The thermal overlay is not loaded.");
            return;
        }

        if (args.Length >= 1)
        {
            if (!int.TryParse(args[0], out var mode) || mode is < 0 or > 3)
            {
                shell.WriteError("Mode must be 0, 1, 2 or 3.");
                return;
            }

            overlay.DebugMode = mode;
        }

        shell.WriteLine($"mode {overlay.DebugMode}");
        shell.WriteLine($"bodies in the scope window: {overlay.LastBodiesInWindow}, drawn as heat: {overlay.LastBodiesDrawn}");
        shell.WriteLine(overlay.LastScreenTextureMissing
            ? "WARNING: the screen texture was missing last frame, so the backdrop could not be drawn."
            : "screen texture ok");
    }
}
