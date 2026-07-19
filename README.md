# iRacing Radar for SimHub

[中文](#readme-zh) | [English](#readme-en)

<a id="readme-zh"></a>

## 中文说明

一个用于 iRacing 的 SimHub 车辆雷达覆盖层。下载编译好的发布包后，直接解压到 SimHub 根目录即可使用。

> **重要提示：** 当车辆位于本车侧面时，雷达只能显示车辆在左侧还是右侧，以及它相对本车偏前或偏后；**无法提供两车之间的实际横向距离**。侧面红色标记的位置和间隔不能作为横向间距或碰撞余量使用。

### 演示视频

[![iRacing 车辆雷达演示视频](https://img.youtube.com/vi/-9Pv4CWri6g/maxresdefault.jpg)](https://youtu.be/-9Pv4CWri6g)

点击上方图片在 YouTube 观看雷达演示。

### 雷达状态说明

雷达中央的灰色区域代表本车，上方代表车头方向，下方代表车尾方向。以下四张图按顺序展示一辆对手车辆从后方靠近、并排超车，再到前方远离时的雷达画面。

#### 1. 后方远处靠近

![后方车辆从远处靠近](docs/radar-state-approaching-far.png)

雷达下方出现绿色弧形，表示后方提示范围内有车。距离和时差显示为红色时，表示后车正在接近。

#### 2. 车辆逼近

![后方车辆逼近](docs/radar-state-closing.png)

后车靠近时，下方提示逐渐变为红色扇形。车辆越近，红色区域越宽、颜色越明显。

#### 3. 车辆并排

![车辆在本车侧面并排](docs/radar-state-side-by-side.png)

车辆并排时，本车左侧或右侧会出现红色车辆标记。标记偏上表示对手更靠近本车车头，偏下表示更靠近本车车尾。并排状态不显示距离和时差。

#### 4. 超车后远离

![对手完成超车后从前方远离](docs/radar-state-separating.png)

对手完成超车后，提示出现在雷达上方。距离和时差显示为绿色时，表示前车正在远离。随着距离增大，前方红色提示变为绿色弧形，最后从雷达上消失。

### 其他视觉状态

- 附近没有车辆时，整个雷达自动隐藏。
- 前后车辆距离小于 2.5 米时，距离和时差文字隐藏，图形警示继续显示。
- 红色文字表示车辆正在靠近，绿色文字表示车辆正在远离。
- 侧面车辆只显示红色位置标记，不显示文字数值。

### 安装

先关闭 SimHub。下载发布包后，把 ZIP **直接解压到 SimHub 根目录**：

```text
C:\Program Files (x86)\SimHub
```

压缩包已经包含完整目录结构，不需要创建文件夹或分别移动文件。解压后的结构应为：

```text
SimHub\
├─ User.IRacingRadarPlugin.dll
├─ IRacingRadar.settings.ini
├─ IRacingRadar.Configurator.exe
├─ IRacingRadar.Updater.exe
└─ DashTemplates\
   └─ iRacing Radar\
      ├─ iRacing Radar.djson
      └─ iRacing Radar.djson.ressources
```

解压完成后：

1. 运行 **IRacingRadar.Configurator.exe**，并允许 Windows 权限提示。
2. 按照下方说明完成设置，然后点击“保存设置”。
3. 在 SimHub 插件列表中启用 **iRacing Radar**，并在 Dash Studio / Overlays 中启动同名 Overlay。
4. 启动 iRacing。建议使用无边框或窗口模式，以便 Windows Overlay 正常显示。
### 配置工具使用说明

运行 **IRacingRadar.Configurator.exe** 后，可直接通过界面完成全部设置，无需手动编辑配置文件。

#### 提示条件

- **显示模式**：选择前后车辆显示距离、相对时间、两者同时显示，或隐藏数值。隐藏数值不会关闭雷达图形警示。
- **距离提示范围**：车辆进入设定距离后开始显示雷达提示。
- **相对时间提示范围**：根据前后车辆与本车的相对时间触发提示，仅在对应显示模式下参与判断。
- **红色警示距离**：车辆进入近距离范围后，绿色提示逐渐切换为红色警示。
- **边缘渐显比例**：控制车辆刚进入或离开提示范围时雷达透明度的变化区间。

#### 显示效果

- **前方绿色提示条**和**后方绿色提示条**可以分别开启或关闭。
- **数值字体大小**控制前后车辆距离和相对时间文字的大小。
- **整体透明度**控制雷达在游戏画面上的可见程度。

#### 雷达效果预览

右侧预览使用与 SimHub Overlay 相同的图像资源，修改设置后会立即显示对应效果。

- 可直接查看后方绿色、后方红色、左侧并排、前方红色和前方绿色五种状态。
- 点击“动态演示”可观看车辆从后方接近、左侧完成超车并从前方远离的完整过程。
- 关闭某一方向的绿色提示条后，对应的绿色预览按钮会变灰，动态演示也会跳过该阶段。

#### 语言、主题与保存

- 顶部菜单可以切换中文或英文，以及白天或夜间主题。语言和主题会自动记忆。
- 点击“保存设置”后，如果 SimHub 没有运行，设置会直接保存。
- 如果 SimHub 正在运行，可以选择立即重启、稍后手动重启或取消。选择立即重启后，配置工具会自动完成重启。

#### 自动更新

- 配置工具启动时会自动检查 GitHub 是否发布了新版本。
- 发现新版本后，可以选择自动下载并安装或暂不更新。
- 自动更新会保留用户当前设置，并替换插件、Overlay、配置工具和更新器。
- 如果 SimHub 正在运行，更新器会先关闭 SimHub，更新完成后再自动启动。
- 更新失败时会尝试恢复原文件；断网或检查失败不会影响配置工具正常使用。
- **IRacingRadar.Updater.exe** 是自动更新所需文件，请不要从 SimHub 根目录中删除。
<a id="readme-en"></a>

## English

An iRacing radar overlay for SimHub. Download the prebuilt release package and extract it directly into the SimHub root folder.

> **Important:** When a car is alongside, the radar can only show whether it is on the left or right and whether it is relatively ahead or behind. It **cannot provide the actual lateral distance between the two cars**. Do not use the position or spacing of the red side marker as a measure of lateral clearance or collision margin.

### Demo video

[![iRacing Radar demo video](https://img.youtube.com/vi/-9Pv4CWri6g/maxresdefault.jpg)](https://youtu.be/-9Pv4CWri6g)

Click the image above to watch the radar demonstration on YouTube.

### Radar states

The grey area in the centre represents your car. The top is the front and the bottom is the rear. The following images show an opponent approaching from behind, moving alongside, and pulling away in front.

#### 1. Approaching from behind

![Opponent approaching from behind at a distance](docs/radar-state-approaching-far.png)

A green arc appears below the radar when a car is within the rear warning area. Red distance and time text indicates that the car behind is getting closer.

#### 2. Close proximity

![Opponent closing in behind](docs/radar-state-closing.png)

As the rear car gets closer, the lower warning changes into a red sector. The closer the car is, the wider and more visible the red area becomes.

#### 3. Side by side

![Opponent alongside the player](docs/radar-state-side-by-side.png)

When a car is alongside, a red vehicle marker appears on the corresponding side. A higher marker means the opponent is closer to your front; a lower marker means it is closer to your rear. Distance and time values are hidden while side by side.

#### 4. Moving away after the pass

![Opponent moving away in front](docs/radar-state-separating.png)

After the opponent passes, the warning appears above the radar. Green distance and time text indicates that the car ahead is moving away. As the gap increases, the front red warning changes into a green arc and eventually disappears.

### Other visual states

- The entire radar hides when no nearby cars are present.
- Below a 2.5-metre front or rear gap, the text values are hidden while the graphical warning remains visible.
- Red text means the car is approaching; green text means it is moving away.
- Side-by-side cars use red position markers without text values.

### Installation

Close SimHub first. Download the release package, then extract the ZIP **directly into the SimHub root folder**:

```text
C:\Program Files (x86)\SimHub
```

The archive already contains the complete directory structure. You do not need to create folders or move individual files. The extracted layout should be:

```text
SimHub\
├─ User.IRacingRadarPlugin.dll
├─ IRacingRadar.settings.ini
├─ IRacingRadar.Configurator.exe
├─ IRacingRadar.Updater.exe
└─ DashTemplates\
   └─ iRacing Radar\
      ├─ iRacing Radar.djson
      └─ iRacing Radar.djson.ressources
```

After extracting:

1. Run **IRacingRadar.Configurator.exe** and allow the Windows elevation prompt.
2. Follow the configurator guide below, then select **Save**.
3. Enable the **iRacing Radar** plugin in SimHub and start the matching Overlay from Dash Studio / Overlays.
4. Start iRacing. Borderless or windowed mode is recommended so the Windows Overlay can display correctly.
### Configurator guide

Run **IRacingRadar.Configurator.exe** to manage every radar setting through the interface. Manual configuration-file editing is not required.

#### Alert conditions

- **Display mode**: show distance, relative time, both values, or no numeric values for front and rear cars. Hiding values does not disable graphical radar alerts.
- **Radar range**: begins showing the radar when a car enters the selected distance.
- **Time-gap range**: triggers alerts from the relative time to a front or rear car when the selected display mode uses time.
- **Near-warning distance**: gradually changes a front or rear alert from green to red at close range.
- **Fade band**: controls the portion of the alert-range boundary used to fade the radar in or out.

#### Appearance

- The **front green alert** and **rear green alert** can be enabled independently.
- **Label size** controls the front/rear distance and relative-time text size.
- **Overlay opacity** controls how strongly the radar appears over the game.

#### Radar preview

The preview uses the same image resources as the SimHub Overlay and updates immediately when settings change.

- Preview rear green, rear red, left alongside, front red, and front green states directly.
- Select **Play demo** to watch a car approach from behind, pass on the left, and move away in front.
- Disabling a front or rear green alert greys out its preview button and removes that stage from the dynamic demo.

#### Language, theme, and saving

- Use the top menus to switch between Chinese and English and between day and night themes. Both preferences are remembered automatically.
- When **Save** is selected while SimHub is not running, the settings are saved immediately.
- When SimHub is running, choose **Restart now**, **Restart later**, or **Cancel**. The configurator handles the restart automatically when **Restart now** is selected.

#### Automatic updates

- The configurator checks GitHub for new releases at startup.
- When a newer version is available, choose whether to download and install it automatically or update later.
- Automatic updates preserve the current user settings while replacing the plugin, Overlay, configurator, and updater.
- If SimHub is running, the updater closes it before replacing files and starts it again afterward.
- If installation fails, the updater attempts to restore the previous files. Network and update-check failures do not interrupt normal configurator use.
- **IRacingRadar.Updater.exe** is required for automatic updates and should remain in the SimHub root folder.
## License

MIT License. See [LICENSE.md](LICENSE.md).
