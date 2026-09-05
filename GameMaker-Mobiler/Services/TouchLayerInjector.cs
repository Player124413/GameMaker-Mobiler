using System;
using System.Linq;
using UndertaleModLib;
using UndertaleModLib.Compiler;
using UndertaleModLib.Models;

namespace GameMaker_Mobiler.Services;

/// <summary>
/// Injects the self-drawn touch layer (joystick + buttons + EDIT mode) into data.win.
/// Only official UndertaleModLib APIs are used:
///   - <see cref="CodeImportGroup"/> compiles the code and links the events (the object is created automatically);
///   - the instance is placed in the first room (instance layer on GMS2, GameObjects list on GMS1);
///   - the object is persistent so it survives room changes.
/// </summary>
public static class TouchLayerInjector
{
    /// <summary>
    /// Runs the injection.
    /// </summary>
    /// <param name="data">The loaded data.win.</param>
    /// <param name="report">Result of the key usage analysis.</param>
    /// <param name="options">Generation options.</param>
    /// <param name="mainThreadAction">Dispatcher used when mutating data structures.</param>
    /// <param name="log">Logging callback.</param>
    public static void Inject(
        UndertaleData data,
        KeyUsageReport report,
        TouchLayerOptions options,
        Action<Action> mainThreadAction,
        Action<string, bool>? log = null)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(report);
        ArgumentNullException.ThrowIfNull(options);
        mainThreadAction ??= static a => a();

        var objectName = TouchLayerGenerator.ObjectName;

        var group = new CodeImportGroup(data)
        {
            MainThreadAction = mainThreadAction,
            AutoCreateAssets = true
        };

        // Object events only: no script assets are created, because the script mechanism changed drastically in GMS 2.3.
        group.QueueReplace($"gml_Object_{objectName}_Create_0", TouchLayerGenerator.BuildCreate(report, options));
        group.QueueReplace($"gml_Object_{objectName}_Step_0", TouchLayerGenerator.BuildStep());
        group.QueueReplace($"gml_Object_{objectName}_Draw_64", TouchLayerGenerator.BuildDrawGui());
        group.QueueReplace($"gml_Object_{objectName}_CleanUp_0", TouchLayerGenerator.BuildCleanUp());
        // Other_4 = Room Start
        group.QueueReplace($"gml_Object_{objectName}_Other_4", TouchLayerGenerator.BuildRoomStart());

        group.Import();

        var touchObject = data.GameObjects.ByName(objectName)
            ?? throw new InvalidOperationException($"Failed to create the touch object {objectName}.");

        touchObject.Persistent = true;
        touchObject.Visible = true;
        touchObject.Depth = -100000;

        PlaceInFirstRoom(data, touchObject, mainThreadAction, log);

        log?.Invoke($"Touch layer injected: {report.Buttons.Count} button(s)" +
                    $"{(report.NeedsJoystick && options.EnableJoystick ? " + joystick" : "")}.", false);
    }

    /// <summary>
    /// Places the touch object in the first room so controls exist from the start.
    /// Existing instances are not duplicated, so re-porting stays safe.
    /// </summary>
    private static void PlaceInFirstRoom(
        UndertaleData data,
        UndertaleGameObject touchObject,
        Action<Action> mainThreadAction,
        Action<string, bool>? log)
    {
        if (data.Rooms.Count == 0)
        {
            log?.Invoke("Warning: data.win contains no rooms, the touch layer could not be placed.", true);
            return;
        }

        var room = data.Rooms[0];

        if (room.GameObjects.Any(o => o?.ObjectDefinition == touchObject))
        {
            log?.Invoke("The first room already contains a touch layer instance, skipping placement.", false);
            return;
        }

        var instanceId = data.GeneralInfo is not null
            ? data.GeneralInfo.LastObj++
            : 100000u;

        var instance = new UndertaleRoom.GameObject
        {
            InstanceID = instanceId,
            ObjectDefinition = touchObject,
            X = 0,
            Y = 0,
            ScaleX = 1,
            ScaleY = 1,
            Color = 0xFFFFFFFF
        };

        mainThreadAction(() =>
        {
            room.GameObjects.Add(instance);

            // GMS2 rooms use layers: the instance must also join an instance layer or it is never created.
            if (data.IsGameMaker2())
            {
                var layer = room.Layers.FirstOrDefault(l =>
                    l.LayerType == UndertaleRoom.LayerType.Instances && l.InstancesData is not null);

                if (layer is null)
                {
                    layer = new UndertaleRoom.Layer
                    {
                        LayerName = data.Strings.MakeString("GMM_Touch"),
                        LayerId = room.Layers.Count == 0 ? 1 : room.Layers.Max(l => l.LayerId) + 1,
                        LayerType = UndertaleRoom.LayerType.Instances,
                        LayerDepth = -100000,
                        IsVisible = true,
                        Data = new UndertaleRoom.Layer.LayerInstancesData()
                    };
                    layer.ParentRoom = room;
                    room.Layers.Add(layer);
                }

                layer.InstancesData!.Instances.Add(instance);
            }

            // GameMaker 2024.13+ keeps an instance creation order list for the first room.
            room.InstanceCreationOrderIDs?.InstanceIDs.Add(instanceId);
        });

        log?.Invoke($"Touch layer instance placed in room \"{room.Name?.Content}\".", false);
    }
}
