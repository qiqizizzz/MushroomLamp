# MushroomLamp MVC 框架使用说明

这份说明面向 gamejam 场景：目标是让 `Assets/Scripts` 这个文件夹可以被单独导入到新 Unity 项目中，挂上 `GameScene` 后立刻得到一套轻量可扩展的 MVC 基础框架。

## 1. 框架定位

当前框架保留的是通用基础设施：

- `GameApp`：框架组合根，统一持有核心管理器
- `GameScene`：Unity 场景入口，负责初始化和每帧更新
- `ControllerManager`：控制器注册、初始化、销毁、模块间事件转发
- `ViewManager`：UI 视图注册、打开、关闭、缓存、返回栈管理
- `BaseController`：控制器基类
- `BaseModel`：数据模型基类
- `BaseView` / `BaseItem`：UI 视图与子组件基类
- `ResManager`：基于 `Resources` 的资源加载与对象池
- `TimerManager`：轻量延时回调
- `MessageCenter`：全局消息中心
- `SoundManager`：基于 `Resources/Sounds` 的 BGM 和音效播放
- `LoadingController` / `LoadingView` / `LoadingModel`：基础场景加载模块

旧项目里的业务模块已经移除，例如网络、配置表、战斗、匹配、角色、红点等。后续项目需要时再自己按模块添加。

## 2. 最快接入

1. 将整个 `Assets/Scripts` 文件夹导入 Unity 项目
2. 在首个场景中新建一个空物体，推荐命名为 `Game`
3. 给 `Game` 挂载 `GameScene.cs`
4. 运行场景

运行后框架会自动完成：

- 初始化 `GameApp`
- 创建 `ControllerManager`
- 创建 `ViewManager`
- 创建 `TimerManager`
- 创建 `MessageCenter`
- 创建 `SoundManager`
- 注册默认控制器：`GameUIController`、`LoadingController`
- 如果 `Game` 下没有 `Canvas`、`WorldCanvas`、`EventSystem`、`Audio`，会自动创建或收拢到 `Game` 下

`GameScene` 默认勾选 `DontDestroyOnSceneLoad`，切场景时不会销毁。

推荐场景层级：

```text
MainMenu
├─ Main Camera
└─ Game
   ├─ Canvas
   ├─ WorldCanvas
   ├─ EventSystem
   └─ Audio
      └─ BGM
```

框架会把 `GameScene` 所在物体当成框架根节点。自动创建的 `Canvas`、`WorldCanvas`、`EventSystem`、`Audio`、`GameAppRunner` 都会挂在这个根节点下面，避免散落在场景根级。

`WorldCanvas` 会被设置为 `World Space`，`Canvas` 会被设置为 `Screen Space Overlay`。

## 3. 文件结构

```text
Assets/Scripts
├─ GameApp.cs
├─ GameScene.cs
├─ Common
│  ├─ GameAppRunner.cs
│  ├─ MessageCenter.cs
│  ├─ QLog.cs
│  ├─ ResManager.cs
│  ├─ Defines
│  │  ├─ AddressDefines.cs
│  │  ├─ EventDefines.cs
│  │  └─ SceneDefines.cs
│  └─ Single
│     └─ Singleton.cs
├─ MVC
│  ├─ ControllerManager.cs
│  ├─ ControllerType.cs
│  ├─ ViewManager.cs
│  ├─ ViewType.cs
│  ├─ Controller
│  │  └─ BaseController.cs
│  ├─ Model
│  │  └─ BaseModel.cs
│  ├─ View
│  │  ├─ BaseItem.cs
│  │  ├─ BaseView.cs
│  │  └─ IBaseView.cs
│  └─ Extensions
│     └─ ViewExtensions.cs
└─ Module
   ├─ GameUI
   │  └─ GameUIController.cs
   ├─ Loading
   │  ├─ LoadingController.cs
   │  └─ LoadingModel.cs
   ├─ Sound
   │  └─ SoundManager.cs
   ├─ Timer
   │  ├─ GameTimer.cs
   │  ├─ GameTimerData.cs
   │  └─ TimerManager.cs
   └─ View
      └─ LoadingView.cs
```

## 4. 核心入口

### GameScene

`GameScene` 是唯一需要挂到场景里的脚本，推荐挂在名为 `Game` 的框架根节点上。

```csharp
public class GameScene : MonoBehaviour
{
    [SerializeField] private bool DontDestroyOnSceneLoad = true;
}
```

它会在 `Awake()` 中初始化框架：

```csharp
GameApp.Instance.Init();
registerModule();
GameApp.ControllerManager.InitAllModules();
```

如果你要添加自己的默认模块，推荐在 `GameScene.registerModule()` 里注册：

```csharp
GameApp.ControllerManager.Register(ControllerType.GameUI, new GameUIController());
GameApp.ControllerManager.Register(ControllerType.Loading, new LoadingController());
```

如果你的模块是某个关卡独有的，也可以在关卡脚本中动态注册，不一定要塞进 `GameScene`。

### GameApp

`GameApp` 持有全局管理器：

```csharp
GameApp.ControllerManager
GameApp.ViewManager
GameApp.TimerManager
GameApp.MessageCenter
GameApp.SoundManager
```

在业务代码里可以直接访问：

```csharp
GameApp.TimerManager.Register(1f, onDelayEnd);
GameApp.SoundManager.PlayBGM("main_theme");
GameApp.MessageCenter.PostEvent("PlayerDead");
```

## 5. 添加一个新 UI 界面

下面以添加 `MainMenuView` 为例。主菜单这类通用 UI 不需要单独创建 `MainMenuController`，统一注册在 `GameUIController` 中即可。

### 第一步：确认 ControllerType

打开 `MVC/ControllerType.cs`：

```csharp
public enum ControllerType
{
    GameUI = 0,
    Loading = 1
}
```

枚举保持递增，不要跳号。`MainMenuView` 属于 `GameUIController` 管理，不需要新增 `ControllerType.MainMenu`。

### 第二步：添加 ViewType

打开 `MVC/ViewType.cs`：

```csharp
public enum ViewType
{
    LoadingView = 0,
    MainMenuView = 1
}
```

`ViewType` 的名字要和 View 脚本类名一致，例如 `MainMenuView` 对应 `MainMenuView.cs`。

### 第三步：在 GameUIController 注册 View

打开 `Module/GameUI/GameUIController.cs`，在构造函数中注册：

```csharp
GameApp.ViewManager.Register(ViewType.MainMenuView, new ViewInfo
{
    PrefabName = "UI/MainMenuView",
    parentTf = GameApp.ViewManager.canvasTf,
    controller = this,
    Sorting_Order = 0
});
```

再注册打开事件：

```csharp
public override void InitModuleEvent()
{
    RegisterFunc(EventDefines.OpenMainMenuView, openMainMenuView);
}

public override void RemoveModuleEvent()
{
    UnRegisterFunc(EventDefines.OpenMainMenuView, openMainMenuView);
}

// 打开主菜单界面
private void openMainMenuView(object[] args)
{
    GameApp.ViewManager.Open(ViewType.MainMenuView, args);
}
```

### 第四步：添加事件定义

打开 `Common/Defines/EventDefines.cs`：

```csharp
public const string OpenMainMenuView = "OpenMainMenuView";
```

### 第五步：创建 View

新建 `Assets/Scripts/Module/View/MainMenuView.cs`：

```csharp
/*
* ┌──────────────────────────────────┐
* │  描    述: 主菜单界面
* │  类    名: MainMenuView.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using MVC.View;
using Common;
using UnityEngine.UI;

namespace Module.View
{
    public class MainMenuView : BaseView
    {
        private Button _startButton;

        public override void InitUI()
        {
            _startButton = Find<Button>("Btn_Start");
        }

        public override void InitData()
        {
            base.InitData();
            _startButton.onClick.AddListener(onStartClick);
        }

        // 处理开始按钮点击
        private void onStartClick()
        {
            QLog.Info($"[{nameof(MainMenuView)}] 开始游戏按钮点击");
        }
    }
}
```

### 第六步：打开界面

在任意合适的位置调用：

```csharp
GameApp.ControllerManager.ApplyFunc(
    (int)ControllerType.GameUI,
    EventDefines.OpenMainMenuView
);
```

也可以在某个 View 里调用：

```csharp
ApplyControllerFunc(ControllerType.GameUI, EventDefines.OpenMainMenuView);
```

## 6. UI 预制体放哪里

当前 `ResManager` 默认使用 `Resources.Load`，所以 UI 预制体建议放在：

```text
Assets/Resources/UI/MainMenuView.prefab
```

注册时写：

```csharp
PrefabName = "UI/MainMenuView"
```

不要写 `.prefab` 后缀。

如果预制体不存在，`ViewManager` 会创建一个空 GameObject 并尝试挂载对应的 View 脚本。这样可以避免 gamejam 期间因为 UI 资源没做完导致框架直接报错。

## 7. View 脚本查找规则

`ViewManager` 打开视图时会根据 `ViewType` 名字查找脚本：

```csharp
ViewType.MainMenuView -> MainMenuView.cs
```

因此要保证：

- `ViewType` 枚举名和 View 类名一致
- 每个 View 类继承 `BaseView`
- 每个 View 类在单独 `.cs` 文件中
- 文件名和类名一致

推荐 View 统一放在：

```text
Assets/Scripts/Module/View
```

## 8. Controller 事件

`BaseController` 内置模块事件字典。

注册事件：

```csharp
RegisterFunc("OpenMainMenu", openMainMenu);
```

注销事件：

```csharp
UnRegisterFunc("OpenMainMenu", openMainMenu);
```

触发本控制器事件：

```csharp
ApplyFunc("OpenMainMenu");
```

触发其他控制器事件：

```csharp
ApplyControllerFunc(ControllerType.Loading, EventDefines.LoadingScene, model);
```

跨模块事件推荐使用 `EventDefines` 常量，避免字符串写错。

## 9. Model 用法

Model 用来放控制器的数据状态。

```csharp
public class PlayerModel : BaseModel
{
    public int Hp { get; private set; }

    // 设置玩家生命值
    public void SetHp(int hp)
    {
        Hp = hp;
    }
}
```

在 Controller 中绑定：

```csharp
SetModel(new PlayerModel());
```

获取：

```csharp
PlayerModel model = GetModel<PlayerModel>();
```

## 10. 场景加载

框架自带 `LoadingController`，可以通过事件加载场景。

```csharp
LoadingModel model = new LoadingModel();
model.SetSceneName(SceneDefines.Game);
model.Callback = onLoadComplete;

GameApp.ControllerManager.ApplyFunc(
    (int)ControllerType.Loading,
    EventDefines.LoadingScene,
    model
);
```

如果在 View 中，可以用扩展方法：

```csharp
this.LoadScene(SceneDefines.Game, onLoadComplete);
```

注意：目标场景必须加入 Unity 的 Build Settings。

## 11. TimerManager

延迟执行：

```csharp
GameApp.TimerManager.Register(1.5f, onDelayEnd);
```

适合 gamejam 里的简单延时、切场景后回调、小 UI 动画流程。复杂循环计时器、暂停、时间缩放等功能可以之后再扩展。

## 12. MessageCenter

全局事件：

```csharp
GameApp.MessageCenter.AddEvent("PlayerDead", onPlayerDead);
GameApp.MessageCenter.PostEvent("PlayerDead", playerId);
GameApp.MessageCenter.RemoveEvent("PlayerDead", onPlayerDead);
```

对象事件：

```csharp
GameApp.MessageCenter.AddEvent(this, "HpChanged", onHpChanged);
GameApp.MessageCenter.PostEvent(this, "HpChanged", hp);
GameApp.MessageCenter.RemoveAllEvent(this);
```

临时事件：

```csharp
GameApp.MessageCenter.AddTempEvent("SelectCard", onSelectCard);
GameApp.MessageCenter.PostTempEvent("SelectCard", cardId);
```

临时事件触发一次后会自动移除。

## 13. SoundManager

音频资源默认放在：

```text
Assets/Resources/Sounds/main_theme.wav
Assets/Resources/Sounds/click.wav
```

播放 BGM：

```csharp
GameApp.SoundManager.PlayBGM("main_theme");
```

运行时音频节点会保持在框架根节点下面：

```text
Game
└─ Audio
   ├─ BGM
   └─ Effect_click
```

`BGM` 是循环背景音乐音源。音效会临时创建在 `Audio` 下，播放结束后自动销毁，不会散落在场景根级。

播放音效：

```csharp
GameApp.SoundManager.PlayEffect("click", transform.position);
```

控制音量：

```csharp
GameApp.SoundManager.BgmVolume = 0.8f;
GameApp.SoundManager.EffectVolume = 0.6f;
```

静音：

```csharp
GameApp.SoundManager.IsStop = true;
```

## 14. ResManager

同步加载实例：

```csharp
GameObject obj = ResManager.Instantiate("Prefabs/Bullet", parent);
```

异步加载实例：

```csharp
ResManager.InstantiateAsync("Prefabs/Bullet", onLoaded, parent);
```

对象池获取：

```csharp
GameObject bullet = ResManager.InstantiateFromPool("Prefabs/Bullet", parent);
```

对象池回收：

```csharp
ResManager.ReleaseToPool("Prefabs/Bullet", bullet);
```

清理对象池：

```csharp
ResManager.ClearPool("Prefabs/Bullet");
ResManager.ClearAllPools();
```

资源路径对应 `Assets/Resources` 目录，不需要写扩展名。

## 15. Addressables 是否需要

当前版本不强依赖 AA 包。

gamejam 推荐默认不用 AA，原因是：

- 配置更少
- 导入更快
- 出错点更少
- 资源少时 `Resources` 已经够用
- 框架文件夹可以单独复制，不需要额外 package 配置

什么时候再考虑 AA：

- 资源很多，首包体积需要控制
- 有远程资源更新需求
- 有明确的资源分组、热更、异步加载需求
- 项目周期比 gamejam 更长

如果后续要切回 AA，建议不要直接改业务代码，而是在 `ResManager` 内部做加载策略切换，让业务层仍然调用：

```csharp
ResManager.Instantiate(path);
ResManager.LoadAsset<T>(path);
```

这样未来可以平滑替换加载后端。

## 16. 推荐开发流程

添加一个功能模块时，按这个顺序做：

1. 如果是通用 UI，优先注册在 `GameUIController`
2. 在 `ViewType` 里按递增顺序加视图枚举
3. 在 `EventDefines` 里加事件名
4. 在 `GameUIController` 构造函数中注册 View
5. 在 `GameUIController.InitModuleEvent()` 中注册事件
6. 新建对应 View
7. 创建 UI 预制体并放到 `Resources/UI`
8. 运行场景，触发事件打开界面

## 17. 常见问题

### 打开 View 时提示未注册视图

说明没有调用：

```csharp
GameApp.ViewManager.Register(...)
```

通常应该写在对应 Controller 的构造函数中。

### 打开 View 时提示未找到视图脚本

检查：

- `ViewType` 名字是否和 View 类名一致
- View 类是否继承 `BaseView`
- 类名和文件名是否一致
- 是否有编译错误导致脚本没有进程序集

### UI 节点 Find 返回 null

检查预制体层级路径。

```csharp
_startButton = Find<Button>("Bg/Btn_Start");
```

路径必须相对当前 View 根节点。

### 场景加载失败

检查：

- 场景名是否和 `SceneDefines` 一致
- 目标场景是否加入 Build Settings
- 是否在加载前初始化了 `GameScene`

### 音频播放失败

检查资源是否在：

```text
Assets/Resources/Sounds
```

调用时不要带扩展名：

```csharp
PlayBGM("main_theme");
```

## 18. gamejam 建议

为了速度，建议保持这个框架简单：

- UI 走 MVC
- 小型数据直接放 Model
- 复杂配置可以先用 ScriptableObject 或 JSON，不急着上配置表系统
- 资源先走 `Resources`
- 不急着拆 asmdef
- 不急着上 Addressables
- 不急着做复杂对象池策略

等游戏玩法跑通后，再把常用模块沉淀成固定模板。
