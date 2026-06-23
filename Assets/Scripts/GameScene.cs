/*
* ┌──────────────────────────────────┐
* │  描    述: 框架场景入口，负责初始化和更新 GameApp
* │  类    名: GameScene.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System;
using Common.Defines;
using Module.Cook;
using Module.GameUI;
using Module.Loading;
using MVC;
using UnityEngine;

public class GameScene : MonoBehaviour
{
    [Header("基础设置")]
    [Tooltip("切换场景时是否保留该对象")]
    [SerializeField] private bool DontDestroyOnSceneLoad = true;

    private static bool _isLoaded;
    private bool _isMainScene;

    private void Awake()
    {
        if (_isLoaded)
        {
            Destroy(gameObject);
            return;
        }

        _isLoaded = true;
        _isMainScene = true;

        if (DontDestroyOnSceneLoad)
            DontDestroyOnLoad(gameObject);

        GameApp.Instance.SetRoot(transform);
        GameApp.Instance.Init();
        registerModule();
        GameApp.ControllerManager.InitAllModules();
    }

    private void Start()
    {
        GameApp.ControllerManager.ApplyFunc(
            (int)ControllerType.GameUI,
            EventDefines.OpenMainMenuView
        );
    }

    private void Update()
    {
        GameApp.Instance.Update(Time.deltaTime);
    }

    private void OnDestroy()
    {
        if (!_isMainScene || !_isLoaded || DontDestroyOnSceneLoad) return;

        GameApp.Instance.Destroy();
        _isLoaded = false;
    }

    // 注册框架自带控制器
    private void registerModule()
    {
        GameApp.ControllerManager.Register(ControllerType.GameUI, new GameUIController());
        GameApp.ControllerManager.Register(ControllerType.Loading, new LoadingController());
        GameApp.ControllerManager.Register(ControllerType.Cook, new CookController());
    }
}
