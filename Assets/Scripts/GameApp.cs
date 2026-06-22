/*
* ┌──────────────────────────────────┐
* │  描    述: 游戏框架入口，统一持有核心管理器
* │  类    名: GameApp.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using Common;
using Module.Timer;
using MVC;
using Sound;
using UnityEngine;

public class GameApp : Singleton<GameApp>
{
    public static Transform RootTf;
    public static ControllerManager ControllerManager;
    public static ViewManager ViewManager;
    public static TimerManager TimerManager;
    public static MessageCenter MessageCenter;
    public static SoundManager SoundManager;

    private bool _isInit;

    // 设置框架根节点
    public void SetRoot(Transform rootTf)
    {
        RootTf = rootTf;
    }

    public override void Init()
    {
        if (_isInit) return;

        ControllerManager = new ControllerManager();
        ViewManager = new ViewManager();
        TimerManager = new TimerManager();
        MessageCenter = new MessageCenter();
        SoundManager = new SoundManager();
        _isInit = true;
    }

    public override void Update(float dt)
    {
        TimerManager?.OnUpdate(dt);
    }

    public override void Destroy()
    {
        if (!_isInit) return;

        ControllerManager?.Destroy();
        ResManager.ClearAllPools();
        ControllerManager = null;
        ViewManager = null;
        TimerManager = null;
        MessageCenter = null;
        SoundManager = null;
        RootTf = null;
        _isInit = false;
    }
}
