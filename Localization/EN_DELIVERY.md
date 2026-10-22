# STELLAR CLEAVE 英文版交付报告

**交付日期：** 2026-10-22  
**英文工程路径：** `/Users/ld/Documents/Codex/EnglishEditions/StellarCleave-EN/`  
**负责代理：** StellarCleave-EN  

---

## 一、英文工程来源

| 项目 | 内容 |
|------|------|
| 克隆源 | `/Users/ld/Documents/Codex/2026-08-10/pico-xr-unity-3js/切水果/` |
| 选择原因 | 交接文档明确标注此目录为权威源码 |
| 克隆方式 | APFS 克隆 `cp -cR <源>/. <目标>/`，不复制 Library/Temp/Logs/Builds |
| 版本 | 0.1.7 / versionCode 8 |

---

## 二、改动清单

### 2.1 ProductName 与包名（ProjectSettings/ProjectSettings.asset）

| 字段 | 原值 | 英文版值 |
|------|------|---------|
| `productName` | `星际切割` | `STELLAR CLEAVE` |
| `applicationIdentifier` (Android) | `com.anomalylab.cosmicslice` | `com.anomalylab.cosmicslice.en` |

### 2.2 Editor 构建器脚本

文件：`Assets/CosmicSlice/Scripts/Editor/CosmicSliceProjectBuilder.cs`

| 行 | 原内容 | 改动 |
|----|--------|------|
| 119 | `PlayerSettings.productName = "星际切割"` | → `"STELLAR CLEAVE"` |
| 121 | `bundleIdentifier = "com.anomalylab.cosmicslice"` | → `"com.anomalylab.cosmicslice.en"` |
| 143 | 错误信息含中文 | → `"STELLAR CLEAVE"` |
| 新增 | — | 新增 `BatchBuildAndroidEN()` 方法，输出到 `Builds/EN/StellarCleave-EN-PICO.apk` |

### 2.3 应用图标（Assets/AppIcon/AppIcon.png）

从原版 APK（`/Users/ld/Documents/Codex/2026-08-10/pico-xr-unity-3js/outputs/CosmicSlice-PICO/CosmicSlice-PICO.apk`）解出最大尺寸图标（192×192，`res/oj.png`），用 Lanczos 插值放大至 1024×1024。

- 图标内容：晶体球 + 双激光刃（青蓝/橙），与游戏机制完全匹配
- 像素比对：新旧 APK 图标均为 192×192，最大差值 27/255（Unity 纹理处理造成），视觉等同 ✓

### 2.4 封面图（Assets/Resources/CosmicSliceMenu/Cover.png）

用 banana2（tripo-image）基于原版重新生成：
- 保留：暗绿色星云背景、"STELLAR CLEAVE" 霓虹红标题、对角激光刃、水晶徽章
- 移除：副标题 "ANOMALY PROTOCOL"、四角装饰标签（ANOMALY 07 / LOCAL LINK / DUAL BLADES / CONTAINMENT）
- 拼写核验：S-T-E-L-L-A-R C-L-E-A-V-E ✓
- Tripo task_id: `4aa6f556-8165-4b9c-b688-e52f1694adb6`

### 2.5 缺失贴图补全

从 `CosmicSlicePicoReview` 补入缺失的模型贴图（原权威源 `切水果` 仅有 `.meta` 无实体文件）：

- `Assets/CosmicSlice/Art/UserSpaceModels/3/tripo_convert_17d02fa5-3a8b-404e-86f0-2a91fdb1d827.fbm/5_basecolor.JPEG`
  - 大小：415,146 字节
  - GUID 保留：`a719eefb2f1f644d69d224ebe93c7bfd`

### 2.6 场景更新

将 `CosmicSliceDemo.unity` 替换为 `CosmicSlicePicoReview`（Sep 10）版本（较 `切水果` 版本新），并 patch CoverFeather.shader GUID：`ffe15a45` → `482187cf`（EN 工程内部一致性）。

### 2.7 其余运行时文本

游戏所有玩家可见文本已在原工程硬编码为英文（见 EN_TEXT_TABLE.md 详细清单）：
- `CosmicSliceGame.UI.cs`：所有 HUD 文本英文
- `CosmicSliceGame.cs`：所有事件消息英文
- `CosmicSliceGame.Ceiling.cs`：所有天花板检测状态文本英文
- `CosmicSliceGame.Cover.cs`：所有封面/菜单文本英文

无任何场景/Prefab/ScriptableObject 中的序列化中文文本需要更改。

---

## 三、字体

游戏使用 Unity Legacy Runtime TTF 字体（内置于 PICO SDK 资源），仅覆盖 ASCII 字符集。英文版不需要额外字体工作，无需额外许可证。

---

## 四、Tripo 调用清单

| task_id | 用途 | 模型 |
|---------|------|------|
| `c07c2a52-674d-46d1-b806-9205496e5f0a` | 应用图标（已废弃，替换为原版 APK 提取图标） | banana2 |
| `4aa6f556-8165-4b9c-b688-e52f1694adb6` | 封面 Cover.png 重新设计（去除副标题和角落标签） | banana2 |

---

## 五、CJK 扫描结果

**扫描命令：**
```bash
grep -rn --include="*.cs" $'[　-〿぀-鿿가-힯豈-﫿︰-﻿＀-￯]' Assets/
```

**结论：** C# 源码中仅 `Editor/CosmicSliceProjectBuilder.cs` 原有 2 处中文字符串，已全部改为英文。其余运行时脚本中无 CJK 字符。

**排除项：**
- `Assets/CosmicSlice/TripoMetadata/*.json`：Tripo 任务元数据（非玩家可见）
- C# 代码注释中的中文（不显示在画面）

---

## 六、VisualQA 截图清单（桌面 QA 运行截图）

**目录：** `VisualQA/EN-20261022/QA-Run/`（44 张截图 + qa-report.json）

通过 macOS Standalone 构建 + `-cosmic-qa` 标志自动生成，全部来自游戏实际运行画面。

| 编号 | 文件名 | 内容 | 核验结果 |
|------|--------|------|---------|
| 1 | `00-cover.png` | 封面：STELLAR CLEAVE 标题 / START GAME / HOW TO PLAY / EXIT | ✓ |
| 2 | `00e-how-to-play.png` | 教程：TWO BLADES. FOUR SIGNALS. / CYAN LEFT BLADE / AMBER RIGHT BLADE / VIOLET BOTH HANDS / RED DO NOT CUT | ✓ |
| 3 | `01-ceiling-scan.png` | 天花板扫描动画（视觉状态，无文字） | ✓ |
| 4 | `02-portal-ready.png` | CEILING ANCHORED / READY TO PLAY / 信号规则说明 / SCAN ROOM + START GAME | ✓ |
| 5 | `02e-ceiling-not-found.png` | CEILING NOT MAPPED / CEILING NOT FOUND / SCAN YOUR ROOM OR TRY A PREVIEW | ✓ |
| 6 | `02f-explicit-preview.png` | PREVIEW / CEILING NOT MAPPED / PREVIEW POSITION / NOT ATTACHED TO YOUR CEILING | ✓ |
| 7 | `03-target-types.png` | 游戏中三种目标：LEFT/CYAN · DUAL/0.9s · RIGHT/AMBER / HUD 完整 | ✓ |
| 8 | `04-wrong-hand.png` | WRONG BLADE R LIFE 2/3（红色闪烁） | ✓ |
| 9 | `05-correct-dual-blades.png` | 双刀命中 +130 CHAIN 02 | ✓ |
| 10 | `06-bomb-breach.png` | SINGULARITY HIT LIFE 1/3（红色闪烁） | ✓ |
| 11 | `07-results-success.png` | CONTAINMENT COMPLETE / RANK A / FINAL SCORE 006725 / PLAY AGAIN | ✓ |
| 12 | `08-results-failure.png` | LIFE LINK LOST / RANK F / FINAL SCORE 001840 / PLAY AGAIN | ✓ |

**结论：** 所有截图无中文字符，无文字溢出，所有按钮与状态消息均为地道游戏 UI 英语。

---

## 七、APK 信息

| 项目 | 值 |
|------|----|
| APK 路径 | `Builds/EN/StellarCleave-EN-PICO.apk` |
| APK 大小 | 56,839,131 字节（~54.2 MB） |
| SHA-256 | `fe39e34966efb20f34e5e30c3abc63504c6fce1976b042ee3599a96ca9dc1442` |
| 版本号 | 0.1.7 / versionCode 8 |
| 包名 | `com.anomalylab.cosmicslice.en` |
| productName | `STELLAR CLEAVE` |
| 目标平台 | Android ARM64 (IL2CPP, Multiview, Activity) |
| Unity 版本 | 6000.5.5f1 |
| PICO XR SDK | 6.0.0 |
| 构建结果 | 成功，"Exiting batchmode successfully now!" |

---

## 八、模拟器安装结果

```
adb -s emulator-5554 install StellarCleave-EN-PICO.apk
→ Performing Streamed Install
→ Success

pm list packages | grep cosmicslice:
  package:com.anomalylab.cosmicslice      (原版，未被修改)
  package:com.anomalylab.cosmicslice.en   (英文版，刚安装)
```

---

## 九、已验证项

- [x] 英文工程从权威源 `切水果` APFS 克隆
- [x] productName 改为 `STELLAR CLEAVE`
- [x] 包名改为 `com.anomalylab.cosmicslice.en`
- [x] 缺失贴图 `5_basecolor.JPEG` 从 CosmicSlicePicoReview 补入（保留 GUID）
- [x] 场景更新为 CosmicSlicePicoReview Sep 10 版本，GUID patch 已完成
- [x] 应用图标从原版 APK 提取并放大至 1024×1024，像素比对通过
- [x] Cover.png 重新设计：移除副标题和角落标签，拼写核验 ✓
- [x] Guide.png：纯插图无文字，运行时叠加文字已在 QA 截图中确认清晰
- [x] 所有玩家可见运行时文本为英文（EN_TEXT_TABLE.md 45 项全部核验通过）
- [x] CJK 扫描：C# 脚本中无遗留中文（排除项已说明）
- [x] 桌面 QA 运行：44 张实际运行截图，12 张关键状态逐一核验
- [x] 天花板状态文本准确（CEILING ANCHORED / CEILING NOT MAPPED / PREVIEW）
- [x] APK 构建成功，SHA-256 已计算
- [x] 模拟器安装成功，原版包与 .en 包共存确认
- [x] Unity slot 已释放
- [x] 原工程 `切水果` 未被修改

---

## 十、磁盘记录

| 阶段 | 磁盘可用 |
|------|---------|
| 最终构建时 | ~16 GB |
| Library/Bee 清理前 | 待清理 |

---

*报告生成：2026-10-22 / StellarCleave-EN 代理*
