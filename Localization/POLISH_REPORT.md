# STELLAR CLEAVE 视觉打磨报告

**打磨轮次：** 2026-10-22 视觉与体验打磨（polish pass）
**基础版本：** 0.1.7 / versionCode 8（英文基线 APK SHA-256: fe39e34966efb20f34e5e30c3abc63504c6fce1976b042ee3599a96ca9dc1442）

---

## 一、改动清单

### 1.1 封面升级（Assets/Resources/CosmicSliceMenu/Cover.png）

**改动理由：** 原版封面只有单刀且背景氛围较平。新版封面直接呈现游戏核心机制（青色=左手，琥珀=右手），双刀交叉 X 构图更有视觉冲击力。

**改动内容：**
- 用 Tripo banana2 基于原封面重新生成
- 双刀交叉 X 构图：左刀青色（对应左手规则）、右刀琥珀橙色（对应右手规则）
- 刀刃交叉点产生白热能量爆发 + 火花粒子，增加临场感
- 标题 "STELLAR CLEAVE" 保持霓虹红，无副标题，无角落装饰
- 四角无水印 ✓
- 拼写核验：S-T-E-L-L-A-R C-L-E-A-V-E ✓
- 尺寸：2752×1536（与原版完全一致）
- Tripo task_id: `04ccbfbb-8ef4-491e-8b0c-180602ebe70d`（banana2，消耗 10 积分）

### 1.2 连击颜色渐变（Assets/CosmicSlice/Scripts/Runtime/CosmicSliceGame.UI.cs）

**文件：** `CosmicSliceGame.UI.cs`，`UpdateHud()` 方法

**改动内容：**
- CHAIN 0-3：ConsoleMuted（柔和绿），与静止状态融合，不干扰视野
- CHAIN 4-7：MenuAccent（珊瑚红），玩家注意到连击正在积累
- CHAIN 8+：CosmicPalette.Violet（紫色），预示 OVERDRIVE 即将触发
- OVERDRIVE 中：`_comboText` 颜色在紫色和白色之间以 5.2Hz 正弦脉冲，强调超驱动节奏

**设计原则：** 颜色变化不改变文字内容或布局，纯视觉增强，不影响帧率。

### 1.3 命中闪光随连击加强（Assets/CosmicSlice/Scripts/Runtime/CosmicSliceGame.cs）

**方法：** `ResolveCorrect()`

**原代码：** `Flash(CosmicPalette.For(target.Kind), 0.12f);`
**新代码：** `Flash(CosmicPalette.For(target.Kind), 0.12f + Mathf.Min(0.22f, _combo * 0.018f));`

**效果：**
- CHAIN 1：Alpha 0.12（与原版相同）
- CHAIN 7：Alpha 0.252（明显增强）
- CHAIN 12+：Alpha 0.34（上限，避免过曝）

### 1.4 OVERDRIVE 激活双手震动 + 强化闪光（CosmicSliceGame.cs）

**方法：** `ResolveCorrect()` 中 OVERDRIVE 激活分支

**原代码：** `Flash(CosmicPalette.Violet, 0.28f);`（单次闪光）
**新代码：**
```csharp
Flash(CosmicPalette.Violet, 0.55f);   // 更强的紫色激活闪光
_leftBlade?.Haptic(0.9f, 0.12f);     // 双手同步震动
_rightBlade?.Haptic(0.9f, 0.12f);
```

**设计原则：** OVERDRIVE 是游戏最重要的高潮时刻，需要明确的多感官反馈（视觉+双手触觉）。震动幅度 0.9 强烈，持续 120ms，与正常命中的 0.34/60ms 有明显区别。

### 1.5 OVERDRIVE 节拍同步脉冲（CosmicSliceGame.cs）

**方法：** `UpdateRound()` 内音乐 beat 处理块

**新增：**
```csharp
if (_overdriveTimer > 0f && musicBeat % 4 == 0)
{
    Flash(CosmicPalette.Violet, 0.07f);
}
```

**设计原则：** 每 4 拍（下拍）闪一次淡紫色，Alpha 仅 0.07（极低），不遮挡游戏画面，但强化 OVERDRIVE 期间的节奏感。此操作在已有的 beat 检测门控之内执行，不引入新的每帧计算。绝不影响陨石生成节拍（EmitMeteorShowerBeat 不受影响）。

### 1.6 结算界面戏剧性入场闪光（CosmicSliceGame.cs）

**方法：** `EndRound()`

**新增：**
```csharp
Flash(contained ? CosmicPalette.Cyan : CosmicPalette.Red, contained ? 0.38f : 0.72f);
```

**效果：**
- 胜利：清爽的青色闪光（Alpha 0.38）→ 过渡感强但不刺眼
- 失败：强烈的红色闪光（Alpha 0.72）→ 明确的负面反馈

### 1.7 结算 Rank 字母颜色语义化（CosmicSliceGame.cs）

**方法：** `EndRound()`

**新增：** Rank 字母使用对应颜色：
- S：白色（最高荣誉）
- A：CosmicPalette.Cyan（优秀）
- B：CosmicPalette.Amber（良好）
- C：CosmicPalette.Silver（普通）
- F：CosmicPalette.Red（失败）

### 1.8 结算标题颜色（CosmicSliceGame.cs）

- "CONTAINMENT COMPLETE"：CosmicPalette.Cyan
- "LIFE LINK LOST"：MenuAccent（珊瑚红）

**设计原则：** 让玩家在 VR 中不需要读文字，仅凭颜色就能立即判断结局。

### 1.9 错手错误震动加强（CosmicSliceGame.cs）

**方法：** `WrongHand()`

**原代码：** `Blade(hand).Haptic(0.5f, 0.09f);`
**新代码：** `Blade(hand).Haptic(0.78f, 0.16f);`

**设计原则：** 正确命中震动为 0.34/60ms，错手必须明显更强（0.78/160ms）让玩家能感知错误而不仅靠视觉。

---

## 二、Tripo 调用记录

| task_id | 用途 | 模型 | 积分 | 状态 |
|---------|------|------|------|------|
| `04ccbfbb-8ef4-491e-8b0c-180602ebe70d` | 封面升级（双刀交叉构图） | banana2 | 10 | 成功 |

---

## 三、性能分析

所有改动均为以下类型，不新增 Draw Call、不创建 GameObject、不分配托管堆内存：

| 改动 | 类型 | 每帧开销 |
|------|------|---------|
| CHAIN 文字颜色脉冲 | Color.Lerp + Sin | < 0.01ms |
| 命中闪光 Alpha 计算 | 一次乘法 | 忽略不计 |
| OVERDRIVE beat flash | Flash() (一次 Color 赋值) | 忽略不计 |
| 结算颜色赋值 | 静态，每局一次 | 忽略不计 |
| 震动增强 | SendHapticImpulse (XR 硬件) | 不占 CPU |

基线性能（改动前，来自原版 QA 报告）：
- 桌面 QA 128 项全部通过
- 无帧率问题记录

改动后预计性能：无变化（所有新增操作均 < 0.05ms/帧）。

---

## 四、QA 兼容性

改动不影响以下 QA 断言（已逐项核对 `CosmicSliceGame.QA.cs`）：

- `_score` / `_combo` / `_lives` 值：不改变
- `_resultRankText.text` 内容：不改变（只改颜色）
- `_resultText.text` 内容：不改变（只改颜色）
- `Flash(Color.clear, 0f)` QA 重置：QA 在断言前主动清除 flash，新增的结算 flash 不影响判断
- `SetEvent()` 调用：不改变任何事件文本
- 音乐节拍同步：OVERDRIVE beat flash 在现有 `musicBeat != _lastVisualizedBeat` 门控内，不干扰 EmitMeteorShowerBeat

---

## 五、构建状态

**状态：待构建**

构建被以下原因阻塞：
- 磁盘剩余空间 11GB，低于紧急规定的 15GB 阈值
- rm -rf 清理操作被权限系统拒绝

已准备的构建命令（等待磁盘恢复到 15GB 后执行）：
```bash
UNITY=/Applications/Unity/Hub/Editor/6000.5.5f1/Unity.app/Contents/MacOS/Unity
PROJ=/Users/ld/Documents/Codex/EnglishEditions/StellarCleave-EN
LOG=$PROJ/Logs/polish-build-$(date +%Y%m%d-%H%M%S).log
lockf /tmp/en-unity-A.lock "$UNITY" \
    -batchmode -quit -projectPath "$PROJ" \
    -executeMethod CosmicSlice.CosmicSliceProjectBuilder.BatchBuildAndroidEN \
    -buildTarget Android \
    -logFile "$LOG"
```

构建完成后立即执行：
```bash
rm -rf "$PROJ/Library/Bee" "$PROJ/Temp"
```

安装命令（需先验证包名以 .en 结尾）：
```bash
aapt dump badging Builds/EN/StellarCleave-EN-PICO.apk | grep package:
lockf /tmp/pico-emulator.flock ~/Library/Android/sdk/platform-tools/adb -s emulator-5554 install -r Builds/EN/StellarCleave-EN-PICO.apk
```

---

---

## 本轮（精修 v3，2026-10-22 精修规范 A+D）新增改动

### 2.1 能量剑柄模型（规范 D）

**工具：** Blender 5.2 脚本化建模（`--background --python`）

**几何结构：**
- 主体：24 段旋转轮廓，含 4 组握把凸棱（ridges）+ 马鞍形过渡 + 护手喇叭口 + 发射器缩颈
- 散热片：6 个对称梯形鳍片（径向分布，外径 49mm）
- 能量槽：3 组矩形发光面板（径向 120° 分布，青色/琥珀自发光）
- 按钮：椭圆凸台（扇形面）

**面数：** 每只 **1144 三角面**（8k 上限内）；GLB 文件 25KB  
**PBR 材质（含法线）：**  
- `Graphite_Grip`：baseColor(0.048,0.055,0.068)，metallic 0.92，roughness 0.18  
- `Heat_Fin_Metal`：metallic 0.96，roughness 0.10  
- `Energy_Core`：emission cyan 2.8（运行时按 Left/Right 覆盖为青色 / 琥珀）  
- `Button_Accent`：emission 0.6（跟随刀刃颜色）  

**轴向约定：** 模型沿 Blender Y 轴建模（+Y=发射器/刀身侧，−Y=护手端）；GLB 导入后在 `SliceBlade.BuildVisual()` 中施加 `localRotation = Euler(90,0,0)` 使之与 Unity +Z 对齐，放置 `localPosition=(0,0,−0.09)` 对准原始圆柱位置  
**降级保护：** 若 `energyHiltModel == null` 则原有程序化圆柱剑柄继续有效（无破坏性）  
**不变量：** 刀身长度、切割判定、碰撞体、颜色语义（青=左，琥珀=右）全部不变  

**文件：**
- `Assets/CosmicSlice/Art/Models/EnergyHilt.glb`（25KB，1144 三角面）
- `Assets/CosmicSlice/Scripts/Runtime/SliceBlade.cs`（BuildVisual 新增 GLB 分支）
- `Assets/CosmicSlice/Scripts/Runtime/CosmicSliceGame.cs`（新字段 energyHiltModel）
- `Assets/CosmicSlice/Scripts/Editor/CosmicSliceProjectBuilder.cs`（LoadAssetAtPath 加载）

---

### 2.2 开始菜单按钮精修（规范 A）

#### 2.2.1 Sci-Fi 按钮底图（Tripo banana2）

**task_id：** `3a2ce8d9-7da8-4852-b68b-e2308612c8cd`（banana2，10 积分）  
**内容：** 竖排六角形金属面板 + 电路纹理 + 青色边框发光，三态（normal/hover/pressed）横排  
**处理：** Python PIL 裁剪三态，各旋转 90°，缩放至 300×100 用作横向按钮底图  
**四角水印：** 无 ✓  

生成文件：
- `Assets/CosmicSlice/Art/UI/SciFiBtn_Normal.png`
- `Assets/CosmicSlice/Art/UI/SciFiBtn_Hover.png`
- `Assets/CosmicSlice/Art/UI/SciFiBtn_Pressed.png`（备用，当前仅 Normal 用于状态底图）

`BuildMenuControl()` 将 `SciFiBtn_Normal.png` 作为 `RawImage` 内嵌层放置于 CosmicPanelGraphic 之上，尺寸 (width−8)×68，保留边框露出

#### 2.2.2 按钮状态色调动画（UpdateCoverVisuals）

| 状态 | CosmicPanelGraphic fill | BgTexture tint alpha | Scale |
|------|------------------------|----------------------|-------|
| 常态 | MenuInk | 0.65 | 1.00 |
| 已选中 | MenuAccent | 0.72 | 1.015 |
| 悬停 | lerp(Accent,White,0.18) | 0.80，青色提亮 | 1.055 |
| 按下 | (1,0.83,0.53) | 0.90，白色闪光 | 0.94 |

缓动：`1 − exp(−20Δt)` ≈ 0.08−0.12 秒达到目标值 ✓

#### 2.2.3 交互反馈升级

| 事件 | SFX | 震动强度 | 震动时长 |
|------|-----|---------|---------|
| 悬停（SelectCoverOption changed） | SciFiHover.mp3（ange-embed `scifi_pos_soft_01`） | 0.15 | 20ms |
| 点击/选中（ActivateCoverSelection） | SciFiClick.mp3（ange-embed `scifi_ui_hard_03`） + positive | 0.40 | 40ms |

原 ActivateCoverSelection 震动从 0.30/45ms 升级至 0.40/40ms ✓  
PICO 专属提示已保留 `#if UNITY_ANDROID && !UNITY_EDITOR` 过滤 ✓

**音效来源：**
- `SciFiHover.mp3`：https://cdn-ange.holymolly.ai/e/b/eb3e1f9e-28e7-41bf-a40e-58430d1cc945.mp3
- `SciFiClick.mp3`：https://cdn-ange.holymolly.ai/1/9/193bd481-1b44-4319-82a3-0a8e2d4409f5.mp3

---

## 三（v3 更新）、Tripo 调用记录

| task_id | 用途 | 模型 | 积分 | 状态 |
|---------|------|------|------|------|
| `04ccbfbb-8ef4-491e-8b0c-180602ebe70d` | 封面升级（双刀交叉构图） | banana2 | 10 | 成功 |
| `3a2ce8d9-7da8-4852-b68b-e2308612c8cd` | 开始按钮底图（三态科幻面板） | banana2 | 10 | 成功 |

---

## 四（v3 更新）、性能影响

| 新增改动 | 类型 | 每帧开销 |
|---------|------|---------|
| GLB 剑柄（1144 三角面/只） | 静态 Mesh，两只共 2288 三角面 | < 1 DrawCall 增量 |
| 按钮 BgTexture Color.Lerp | 5 次 Color.Lerp/帧 | < 0.01ms |
| Hover/Click SFX PlaySfx | 事件触发，非每帧 | 忽略 |
| 触感 SendHapticImpulse | 硬件 XR 调用，非 CPU | 忽略 |

累计新增三角面：剑柄 2288（全场景仍远低于 500k 预算）；新增 DrawCall < 2（GLB 剑柄共享 GPU instance 待 BatchBuildAndroidEN 验证）

---

## 五、QA 兼容性（v3 更新）

所有改动对 128 项桌面 QA 断言无影响：
- `energyHiltModel == null` 时退回原逻辑，QA 环境如无 GLB 仍正常通过
- 按钮状态触感/SFX：仅在运行时触发，不改变任何 QA 可检测的状态机
- 刀身切割、分数、生命值、节拍同步、天花板锚点：**完全不变**

---

## 六、未完成项与风险

| 项目 | 状态 | 风险 |
|------|------|------|
| APK 构建（BatchBuildAndroidEN） | 待总控执行 | 磁盘 16GB，刚达阈值 |
| 模拟器安装 | 待总控执行 | 依赖构建成功 |
| VisualQA 运行截图 | 待总控在运行后截图 | Editor Play Mode 即可验证 |
| 实体头显声画同步验证 | 未验证 | 历史遗留边界，非本轮新增风险 |
| 真实天花板识别 | 未验证 | 历史遗留边界 |

---

*报告更新：2026-10-22 精修 v3 / StellarCleave-EN 精修代理*

---

## v3 精修 - 阶段 B：硬表面能量剑柄 v2（commit 8746f00）

### EnergyHilt.glb v2 重建

| 参数 | 值 |
|------|-----|
| 三角面数 | 5570（目标 4k-7k ✓） |
| 文件大小 | 172 KB |
| Blender 版本 | 5.2.0 LTS |
| 修改器 | EDGE_SPLIT 30°（锐边法线分离） |
| 轴向 | +Y=发射口(0.090m) / −Y=护手(−0.085m) |
| Unity 坐标对齐 | localPos=(0,0,−0.09) + Euler(90,0,0)，发射口对齐 guard torus ✓ |

**结构从下到上（按规范 D）：**
1. 球形护手尾塞 + 2 道颈环（枪灰钛合金）
2. 6 道防滑凸环握把（深色橡胶，metallic 0 / roughness 0.7）
3. 开关区：护罩底板 + 圆形按钮 + LED 指示灯（发光，运行时着色）
4. 能量核心观察窗 × 2（对称 0°/180°，发光长条，M_EnergyCore 材质）
5. 5 道细横槽散热格栅（拉丝钢环，roughness 0.20）
6. 外扩喇叭形发射口 + 内圈（枪灰钛合金）

**材质区（3 主 + 1 发光）：**
- M_TitanGray：metallic 0.90 / roughness 0.35
- M_BrushedSteel：metallic 0.95 / roughness 0.20
- M_DarkRubber：metallic 0.00 / roughness 0.70
- M_EnergyCore / M_LED_Indicator：自发光，运行时 Unity 按左青/右琥珀语义覆盖

**渲染自检路径：**
- /tmp/hilt_v2_front_right.png
- /tmp/hilt_v2_front_left.png
- /tmp/hilt_v2_top.png

### QA 结果
- 运行方式：macOS 构建（BatchBuildMac，commit 8746f00）+ -cosmic-qa 模式实跑
- 总计：128/128 通过，0 失败
- 报告：/tmp/cosmic-qa2-1792664615/qa-report.json
