using System;
using System.Linq;
using UndertaleModLib;
using UndertaleModLib.Compiler;
using UndertaleModLib.Models;

namespace GameMaker_Mobiler.Services;

/// <summary>
/// 将自绘触控层（摇杆 + 按钮 + 编辑模式）注入到 data.win。
/// 全程只使用 UndertaleModLib 的官方 API：
///   - 用 <see cref="CodeImportGroup"/> 编译并挂接事件（对象/脚本会被自动创建）；
///   - 把对象实例放进第一个房间（GMS2 使用实例图层，GMS1 使用 GameObjects 列表）；
///   - 对象设为 persistent，保证切换房间后依旧存在。
/// </summary>
public static class TouchLayerInjector
{
    /// <summary>
    /// 执行注入。
    /// </summary>
    /// <param name="data">已加载的 data.win。</param>
    /// <param name="report">键位分析结果。</param>
    /// <param name="options">生成选项。</param>
    /// <param name="mainThreadAction">对 data 结构做变更时使用的调度器。</param>
    /// <param name="log">日志回调。</param>
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

        // 对象事件（不创建任何脚本资源：GMS 2.3 前后脚本机制差异极大，内联最安全）。
        group.QueueReplace($"gml_Object_{objectName}_Create_0", TouchLayerGenerator.BuildCreate(report, options));
        group.QueueReplace($"gml_Object_{objectName}_Step_0", TouchLayerGenerator.BuildStep());
        group.QueueReplace($"gml_Object_{objectName}_Draw_64", TouchLayerGenerator.BuildDrawGui());
        group.QueueReplace($"gml_Object_{objectName}_CleanUp_0", TouchLayerGenerator.BuildCleanUp());
        // Other_4 = Room Start
        group.QueueReplace($"gml_Object_{objectName}_Other_4", TouchLayerGenerator.BuildRoomStart());

        group.Import();

        var touchObject = data.GameObjects.ByName(objectName)
            ?? throw new InvalidOperationException($"未能创建触控对象 {objectName}。");

        touchObject.Persistent = true;
        touchObject.Visible = true;
        touchObject.Depth = -100000;

        PlaceInFirstRoom(data, touchObject, mainThreadAction, log);

        log?.Invoke($"触控层已注入：{report.Buttons.Count} 个按钮" +
                    $"{(report.NeedsJoystick && options.EnableJoystick ? " + 摇杆" : "")}。", false);
    }

    /// <summary>
    /// 把触控对象放进第一个房间，保证游戏一开始就有控制层。
    /// 已存在则不重复添加（重复移植时安全）。
    /// </summary>
    private static void PlaceInFirstRoom(
        UndertaleData data,
        UndertaleGameObject touchObject,
        Action<Action> mainThreadAction,
        Action<string, bool>? log)
    {
        if (data.Rooms.Count == 0)
        {
            log?.Invoke("警告：data.win 中没有任何房间，触控层无法自动放置。", true);
            return;
        }

        var room = data.Rooms[0];

        if (room.GameObjects.Any(o => o?.ObjectDefinition == touchObject))
        {
            log?.Invoke("第一个房间已存在触控层实例，跳过放置。", false);
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

            // GMS2 房间使用图层；必须把实例也加入某个实例图层，否则不会被创建。
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

            // 2024.13+ 需要维护首个房间的实例创建顺序表。
            room.InstanceCreationOrderIDs?.InstanceIDs.Add(instanceId);
        });

        log?.Invoke($"触控层实例已放入房间「{room.Name?.Content}」。", false);
    }
}
