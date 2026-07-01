# 音效配置使用说明

本文说明当前项目的音效配置方式。普通 UI 按钮音效不需要在业务代码里手动写播放逻辑，只需要修改 `SoundCatalog.json`。

## 配置文件位置

```text
Assets/Config/Sound/SoundCatalog.json
```

运行时通过 Addressable 地址读取：

```text
Sound/SoundCatalog
```

## 音频资源位置

音频文件放在：

```text
Assets/Resources/Sounds
```

配置里的 `path` 从 `Sounds` 目录下面开始写，不需要文件后缀。

例如：

```text
Assets/Resources/Sounds/Effetcs/ui_click.wav
```

JSON 中写：

```json
{
  "id": "ui_click",
  "path": "Effetcs/ui_click",
  "volume": 1.0
}
```

## 普通按钮音效

普通 UI 按钮不需要写代码。

只要按钮是 `Button`，并且在某个 View 下面，界面打开时系统会自动绑定：

- 点击音效
- 鼠标经过音效

默认音效配置在：

```json
{
  "defaults": {
    "buttonClick": "ui_click",
    "buttonHover": "ui_hover"
  }
}
```

只要把 `ui_click` 和 `ui_hover` 对应的 `path` 填好，所有按钮都会自动生效。

## 配置音效 ID

`clips` 用来定义音效 ID 和真实资源路径的映射。

```json
{
  "clips": [
    {
      "id": "ui_click",
      "path": "Effetcs/ui_click",
      "volume": 1.0
    },
    {
      "id": "ui_hover",
      "path": "Effetcs/ui_hover",
      "volume": 0.8
    }
  ]
}
```

说明：

- `id`：程序和配置中使用的音效名字
- `path`：`Assets/Resources/Sounds` 下的资源路径，不写后缀
- `volume`：该音效自己的音量倍率

## 单独配置某个界面

如果某个界面想用自己的按钮音效，可以在 `viewBindings` 中配置。

```json
{
  "view": "MainMenuView",
  "bgm": "",
  "buttonClick": "menu_click",
  "buttonHover": "menu_hover",
  "disableAutoButtonSound": false,
  "buttons": []
}
```

说明：

- `view`：View 脚本类名
- `bgm`：打开该界面时播放的 BGM 音效 ID，留空表示不切换
- `buttonClick`：该界面所有按钮默认点击音效
- `buttonHover`：该界面所有按钮默认经过音效
- `disableAutoButtonSound`：设为 `true` 时，该界面不自动绑定按钮音效
- `buttons`：单个按钮的特殊配置

## 单独配置某个按钮

按钮路径是相对当前 View 根节点的层级路径。

例如主菜单开始按钮：

```text
ButtonGroup/Btn_Start
```

配置示例：

```json
{
  "view": "MainMenuView",
  "buttons": [
    {
      "path": "ButtonGroup/Btn_Start",
      "click": "start_click",
      "hover": "ui_hover",
      "muteClick": false,
      "muteHover": false
    }
  ]
}
```

说明：

- `path`：按钮在 View 下的相对路径
- `click`：该按钮点击音效
- `hover`：该按钮经过音效
- `muteClick`：设为 `true` 时，该按钮不播放点击音效
- `muteHover`：设为 `true` 时，该按钮不播放经过音效

## 手动播放特殊音效

普通按钮音效会自动绑定，但玩法事件仍然建议手动播放。

适合手动播放的情况：

- 金币增加
- 卡牌飞行动画
- 烹饪成功或失败
- 结算弹出
- 特殊技能触发

代码示例：

```csharp
GameApp.SoundManager.PlayEffect("coin_gain", transform.position);
```

然后在 JSON 中配置：

```json
{
  "id": "coin_gain",
  "path": "Effetcs/coin_gain",
  "volume": 1.0
}
```

## BGM 配置

普通界面 BGM 轮播配置在：

```json
{
  "defaults": {
    "bgmPlaylist": [
      "bgm_alchemical_clockwork",
      "bgm_cinder_crucible",
      "bgm_murmur_vatcall"
    ]
  }
}
```

烹饪玩法 BGM 配置在：

```json
{
  "defaults": {
    "gameplayBgm": "bgm_ingame"
  }
}
```

对应音频 ID 仍然需要在 `clips` 中配置：

```json
{
  "id": "bgm_ingame",
  "path": "BGM/ingame",
  "volume": 1.0
}
```

## 常见问题

### 改了 JSON 没声音

检查：

- 音频文件是否在 `Assets/Resources/Sounds` 下
- `path` 是否没有写 `Sounds/` 前缀
- `path` 是否没有写 `.wav`、`.mp3`、`.ogg` 后缀
- `id` 是否和 `buttonClick`、`buttonHover`、代码里传入的名字一致
- 音效开关和音量是否在设置里关闭了

### 某个按钮不想有声音

在对应 View 的 `buttons` 中配置：

```json
{
  "path": "ButtonGroup/Btn_Exit",
  "muteClick": true,
  "muteHover": true
}
```

### 某个界面全部不要自动音效

```json
{
  "view": "CookView",
  "disableAutoButtonSound": true
}
```

## 推荐使用方式

- 普通 UI 按钮：只改 JSON，不写代码
- 特殊玩法音效：代码里调用 `PlayEffect("音效id")`
- 换音频资源：只改 `clips` 里的 `path`
- 改某个界面风格：改 `viewBindings`
- 改单个按钮：改 `buttons`

