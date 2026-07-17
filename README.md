# iRacing Radar for SimHub

[中文](#readme-zh) | [English](#readme-en)

<a id="readme-zh"></a>

## 中文说明

一个用于 iRacing 的 SimHub 车辆雷达覆盖层。下载编译好的发布包后，只需要把 DLL、配置文件和 overlay 文件复制到 SimHub 指定目录即可使用。

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

### 需要复制哪些文件

发布包里应该包含这些文件：

```text
User.IRacingRadarPlugin.dll
IRacingRadar.settings.ini
iRacing Radar.djson
iRacing Radar.djson.ressources
```

### 文件放在哪里

先关闭 SimHub，然后复制文件。

把插件 DLL 和配置文件放到 SimHub 根目录：

```text
C:\Program Files (x86)\SimHub\User.IRacingRadarPlugin.dll
C:\Program Files (x86)\SimHub\IRacingRadar.settings.ini
```

把 overlay 文件放到 SimHub 的 DashTemplates 目录。没有 `iRacing Radar` 文件夹就手动创建：

```text
C:\Program Files (x86)\SimHub\DashTemplates\iRacing Radar\iRacing Radar.djson
C:\Program Files (x86)\SimHub\DashTemplates\iRacing Radar\iRacing Radar.djson.ressources
```

复制完成后：

1. 启动 SimHub。
2. 在 SimHub 插件列表中启用 **iRacing Radar**。
3. 在 Dash Studio / Overlays 中启动 **iRacing Radar**。
4. 启动 iRacing。建议使用无边框或窗口模式，方便 Windows overlay 正常显示。

### 配置文件位置

推荐把配置文件放在 DLL 同目录：

```text
C:\Program Files (x86)\SimHub\IRacingRadar.settings.ini
```

插件会优先读取这个文件。如果找不到，会兼容旧位置：

```text
%USERPROFILE%\Documents\iRacingRadar\IRacingRadar.settings.ini
%USERPROFILE%\Documents\iraing_Rader\IRacingRadar.settings.ini
```

### 配置项说明

```ini
DisplayMode=Both
```

控制前后车辆显示哪些数字，并决定使用距离条件、时间差条件还是两者来显示图形警示。

- `None`：不显示距离和时差文字；图形警示仍然正常显示，并采用与 `Both` 相同的触发条件。
- `Distance`：只看距离条件，只显示米数。
- `Time`：只看时间差条件，只显示秒数。
- `Both`：距离或时间差只要有一个达到设定范围，就显示图形警示，并同时显示米数和秒数。

```ini
RadarRangeMeters=70
```

距离条件的范围，单位是米。设置为 `70` 表示前后车辆距离本车不超过 70 米时，距离条件成立。

雷达会在距离范围最外侧的 15% 区间内按比例渐显。例如范围为 70 米时，70 米处透明度为 0，65 米处于渐显状态，约 59.5 米及以内完全显示。

- `DisplayMode=Distance`：只根据这个距离判断是否提示。
- `DisplayMode=Both` 或 `None`：距离条件和时间差条件满足任意一个，都会显示图形警示。
- `DisplayMode=Time`：不使用这个距离条件。

```ini
TimeAlertSeconds=0.7
```

时间差条件的范围，单位是秒。设置为 `0.7` 表示前后车辆与本车的时间差不超过 0.7 秒时，时间差条件成立。

时间差使用相同的比例区间，并会随着 `TimeAlertSeconds` 的设置自动缩放。

```ini
RadarFadeBandPercent=15
```

控制距离和时间差范围最外侧用于透明度变化的比例，范围为 `1` 到 `50`。设置为 `15` 时，最外侧 15% 从透明度 0 按比例增加，进入内部 85% 后完全显示。

- `DisplayMode=Time`：只根据这个时间差判断是否提示。
- `DisplayMode=Both` 或 `None`：时间差条件和距离条件满足任意一个，都会显示图形警示。
- `DisplayMode=Distance`：不使用这个时间差条件。

例如同时设置 `RadarRangeMeters=70` 和 `TimeAlertSeconds=0.7`：车辆相距 60 米但时间差为 1.0 秒时，距离条件成立；车辆相距 90 米但时间差为 0.5 秒时，时间差条件成立。`Both` 和 `None` 在这两种情况下都会显示图形警示，但 `None` 不显示任何数字。

```ini
NearDistanceMeters=20
```

近距离红色警示范围，单位是米。前后车辆进入这个范围后，雷达会从绿色提示逐渐变成红色警示。

```ini
FrontGreenArcEnabled=true
RearGreenArcEnabled=true
```

分别控制前方和后方的绿色远距离提示条。设置为 `true` 时显示，设置为 `false` 时隐藏。关闭绿色条后，该方向只在红色近距离警示期间显示；红色扇形结束后，雷达和文字会一起渐隐。侧面标记不受影响。

```ini
OverlayOpacity=92
```

雷达整体透明度，范围建议 `0` 到 `100`。数值越大越明显。

```ini
LabelFontSize=22
```

前后车辆距离/时间文字大小。

<a id="readme-en"></a>

## English

An iRacing radar overlay for SimHub. Download the prebuilt release package, copy the DLL, settings file, and overlay files into the SimHub folders, then enable the overlay in SimHub.

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

### Files to copy

The prebuilt release package should include these files:

```text
User.IRacingRadarPlugin.dll
IRacingRadar.settings.ini
iRacing Radar.djson
iRacing Radar.djson.ressources
```

### Where to put the files

Close SimHub first, then copy the files.

Put the plugin DLL and settings file in the SimHub root folder:

```text
C:\Program Files (x86)\SimHub\User.IRacingRadarPlugin.dll
C:\Program Files (x86)\SimHub\IRacingRadar.settings.ini
```

Put the overlay files in the SimHub DashTemplates folder. Create the `iRacing Radar` folder if it does not exist:

```text
C:\Program Files (x86)\SimHub\DashTemplates\iRacing Radar\iRacing Radar.djson
C:\Program Files (x86)\SimHub\DashTemplates\iRacing Radar\iRacing Radar.djson.ressources
```

After copying:

1. Start SimHub.
2. Enable the **iRacing Radar** plugin.
3. Start **iRacing Radar** from Dash Studio / Overlays.
4. Start iRacing. Borderless or windowed mode is recommended for Windows overlays.

### Settings file location

Recommended location:

```text
C:\Program Files (x86)\SimHub\IRacingRadar.settings.ini
```

The plugin reads this file first because it is next to `User.IRacingRadarPlugin.dll`. If it is missing, the plugin falls back to these legacy locations:

```text
%USERPROFILE%\Documents\iRacingRadar\IRacingRadar.settings.ini
%USERPROFILE%\Documents\iraing_Rader\IRacingRadar.settings.ini
```

### Settings

```ini
DisplayMode=Both
```

Controls which front/rear values are shown and whether the graphical alert uses the distance condition, the time-gap condition, or both.

- `None`: show no distance or time text; graphical alerts remain active and use the same trigger conditions as `Both`.
- `Distance`: use only the distance condition and show metres only.
- `Time`: use only the time-gap condition and show seconds only.
- `Both`: show the graphical alert when either condition is met, and display both metres and seconds.

```ini
RadarRangeMeters=70
```

The distance-condition range in metres. A value of `70` means the distance condition is met when a front or rear car is no more than 70 metres away.

The radar fades in proportionally over the outermost 15% of the configured distance range. With a 70-metre range, opacity is zero at 70 metres, partial at 65 metres, and fully visible at approximately 59.5 metres and below.

- `DisplayMode=Distance`: only this distance condition controls the alert.
- `DisplayMode=Both` or `None`: the graphical alert appears when either the distance condition or time-gap condition is met.
- `DisplayMode=Time`: this distance condition is not used.

```ini
TimeAlertSeconds=0.7
```

The time-gap-condition range in seconds. A value of `0.7` means the time-gap condition is met when a front or rear car is no more than 0.7 seconds away.

The time-gap condition uses the same proportional region, scaled automatically from `TimeAlertSeconds`.

```ini
RadarFadeBandPercent=15
```

Controls the percentage of the outer distance/time range used for opacity transition, from `1` to `50`. At `15`, opacity increases proportionally through the outermost 15%, then remains fully visible through the inner 85%.

- `DisplayMode=Time`: only this time-gap condition controls the alert.
- `DisplayMode=Both` or `None`: the graphical alert appears when either the time-gap condition or distance condition is met.
- `DisplayMode=Distance`: this time-gap condition is not used.

For example, with `RadarRangeMeters=70` and `TimeAlertSeconds=0.7`: a car at 60 metres with a 1.0-second gap meets the distance condition; a car at 90 metres with a 0.5-second gap meets the time-gap condition. `Both` and `None` show the graphical alert in either case, but `None` shows no numeric values.

```ini
NearDistanceMeters=20
```

Close-warning range in meters. Front/rear alerts gradually change from green to red inside this range.

```ini
FrontGreenArcEnabled=true
RearGreenArcEnabled=true
```

Control the front and rear green far-distance arcs independently. When an arc is disabled, that direction is shown only during the red close-warning phase; after the red sector ends, the radar and text fade out together. Side markers are unaffected.

```ini
OverlayOpacity=92
```

Overall radar opacity. Recommended range is `0` to `100`. Higher values are more visible.

```ini
LabelFontSize=22
```

Font size for front/rear distance and time labels.

## License

MIT License. See [LICENSE.md](LICENSE.md).
