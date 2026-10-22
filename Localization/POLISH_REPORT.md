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

## 六、未完成项与风险

| 项目 | 状态 | 风险 |
|------|------|------|
| APK 构建 | 待执行（磁盘不足） | 磁盘恢复后可直接构建 |
| 模拟器安装 | 待执行 | 依赖构建成功 |
| VisualQA 对比截图 | 待执行 | 依赖构建/运行 |
| 实体头显声画同步验证 | 未验证 | 历史遗留边界，非本轮新增风险 |
| 真实天花板识别 | 未验证 | 历史遗留边界 |

---

*报告生成：2026-10-22 / StellarCleave-EN 打磨代理*
