# STELLAR CLEAVE 英文版交付报告

**交付日期：** 2026-10-22  
**英文工程路径：** `/Users/ld/Documents/Codex/EnglishEditions/StellarCleave-EN/`  
**负责代理：** StellarCleave-EN  

---

## 一、英文工程来源

| 项目 | 内容 |
|------|------|
| 克隆源 | `/Users/ld/Documents/Codex/2026-08-10/pico-xr-unity-3js/切水果/` |
| 选择原因 | 交接文档明确标注此目录为权威源码；`CosmicSlicePicoReview` 是错误回退副本，不使用 |
| 克隆方式 | APFS 克隆 `cp -cR <源>/. <目标>/`，不复制 Library/Temp/Logs/Builds |
| 版本 | 0.1.7 / versionCode 8 |

---

## 二、改动清单

### 2.1 ProductName 与包名（ProjectSettings/ProjectSettings.asset）

| 字段 | 原值 | 英文版值 |
|------|------|---------|
| `productName` | `星际切割`（星际切割） | `STELLAR CLEAVE` |
| `applicationIdentifier` (Android) | `com.anomalylab.cosmicslice` | `com.anomalylab.cosmicslice.en` |

### 2.2 Editor 构建器脚本

文件：`Assets/CosmicSlice/Scripts/Editor/CosmicSliceProjectBuilder.cs`

| 行 | 原内容 | 改动 |
|----|--------|------|
| 119 | `PlayerSettings.productName = "星际切割"` | → `"STELLAR CLEAVE"` |
| 121 | `bundleIdentifier = "com.anomalylab.cosmicslice"` | → `"com.anomalylab.cosmicslice.en"` |
| 143 | 错误信息含 `"星际切割"` | → `"STELLAR CLEAVE"` |
| 新增 | — | 新增 `BatchBuildAndroidEN()` 方法，输出到 `Builds/EN/StellarCleave-EN-PICO.apk` |

### 2.3 应用图标（Assets/AppIcon/AppIcon.png）

原工程只有 `.meta` 文件，无实际图标 PNG。使用 Tripo banana2 重新生成英文版图标：
- 风格：深绿星云 + 珊瑚色斜切光线 + "STELLAR CLEAVE" 文字
- 尺寸：1024×1024
- 拼写已逐字核验：S-T-E-L-L-A-R C-L-E-A-V-E ✓

### 2.4 其余运行时文本

游戏所有玩家可见文本已在原工程硬编码为英文（见 EN_TEXT_TABLE.md 详细清单）：
- `CosmicSliceGame.UI.cs`：所有 HUD 文本英文
- `CosmicSliceGame.cs`：所有事件消息英文
- `CosmicSliceGame.Ceiling.cs`：所有天花板检测状态文本英文
- `CosmicSliceGame.Cover.cs`：所有封面/菜单文本英文

无任何场景/Prefab/ScriptableObject 中的序列化中文文本需要更改。

---

## 三、字体

游戏使用 Unity Legacy Runtime TTF 字体（已内置于 PICO SDK 资源中），该字体仅覆盖 ASCII 字符集，不含中文回退。英文版不需要额外字体工作。无需额外许可证。

---

## 四、Tripo 调用清单

| task_id | 用途 | 模型 | 参考图 |
|---------|------|------|--------|
| `c07c2a52-674d-46d1-b806-9205496e5f0a` | 应用图标 AppIcon.png（1024×1024，深绿星云 + 珊瑚色斜切 + STELLAR CLEAVE 文字） | banana2 | 封面 Cover.png（风格参考） |

---

## 五、CJK 扫描结果

**扫描命令：**
```bash
grep -rn --include="*.cs" $'[　-〿぀-鿿가-힯豈-﫿︰-﻿＀-￯]' Assets/
```

**结论：** C# 源码中仅 `Editor/CosmicSliceProjectBuilder.cs` 原有 2 处中文字符串，已全部改为英文。其余运行时脚本中无 CJK 字符。

**排除项：**
- `Assets/CosmicSlice/TripoMetadata/*.json`：Tripo 任务元数据，含文件路径中的中文目录名（非玩家可见，不加载到运行时）
- C# 代码注释中的中文（不显示在画面）

---

## 六、VisualQA 截图清单

**目录：** `VisualQA/EN-20261022/`

| 文件名 | 内容 | 类型 |
|--------|------|------|
| `01-cover-stellar-cleave.png` | 游戏封面（"STELLAR CLEAVE" 标题，"ANOMALY PROTOCOL" 副标题） | 资源静态图 |
| `02-guide-illustration.png` | 教程插图（无文字，双刃示意图） | 资源静态图 |
| `03-app-icon.png` | 生成的英文 AppIcon（banana2 生成） | 生成图标 |
| `04-hud-backdrop.png` | HUD 背景底图（无文字） | 资源静态图 |

**⚠ 未验证边界 — 实机运行截图：**  
ADB 显示无设备/模拟器连接（`adb devices` 返回空列表），无法取得游戏运行画面截图。
实机运行截图（封面交互、天花板扫描、游戏 HUD、计分、重试等状态）需在实体 PICO 设备上安装 APK 后补充。

---

## 七、APK 信息

| 项目 | 值 |
|------|----|
| APK 路径 | `Builds/EN/StellarCleave-EN-PICO.apk` |
| APK 大小 | 53.7 MB（56,278,369 字节） |
| SHA-256 | `6b2eb37502aa8d8902e96e3e5aab37d58b42c6f62d6a19be61e5c1ae76354103` |
| 版本号 | 0.1.7 / versionCode 8 |
| 包名 | `com.anomalylab.cosmicslice.en` |
| productName | `STELLAR CLEAVE` |
| 目标平台 | Android ARM64 (IL2CPP, Multiview, Activity) |
| Unity 版本 | 6000.5.5f1 |
| PICO XR SDK | 6.0.0 |
| 构建结果 | 成功，0 编译错误，正常退出 (batchmode successfully) |

---

## 八、已验证项

- [x] 英文工程从权威源 `切水果` APFS 克隆
- [x] productName 改为 `STELLAR CLEAVE`
- [x] 包名改为 `com.anomalylab.cosmicslice.en`
- [x] Editor 构建器脚本中所有中文字符串改为英文
- [x] AppIcon.png 生成为英文版（原工程缺失）
- [x] 所有玩家可见运行时文本为英文（C# 脚本确认）
- [x] CJK 扫描：C# 脚本中无遗留中文（排除项已说明）
- [x] 天花板状态描述准确（CEILING ANCHORED / CEILING NOT MAPPED / PREVIEW / CEILING NOT MAPPED）
- [x] 预览模式明确标注 "CEILING NOT MAPPED"
- [x] APK 构建成功，SHA-256 已计算
- [x] Unity slot-2 使用后已释放
- [x] Library/Logs/build 中间产物已删除
- [x] 原工程 `切水果` 未被修改
- [x] 用户正在使用的 Unity 编辑器 (pid 6422) 未被干扰

## 九、未验证边界

- [ ] **实机运行截图**：无 PICO 设备/模拟器连接，未能截取游戏各画面
- [ ] **字体 SDF 清晰度**：依赖原工程 Legacy TTF，未在实机上验证中文回退缺失
- [ ] **手柄操作验证**：[Left Trigger] 切割信号、[Right Trigger] 切割信号、[X Button] 交替切割，均未在实机上验证
- [ ] **MR 天花板检测实测**：需要真实空间扫描验证天花板状态文本正确显示

---

## 十、磁盘使用记录

| 阶段 | 磁盘可用 |
|------|---------|
| 开始克隆前 | ~23 GB |
| 构建前 | 14 GB |
| 构建后 + 清理前 | 6.8 GB（已低于 10 GB 红线，立即清理） |
| 清理 Library/Logs/build 后 | 12 GB |
| 最终（slot-2 释放后） | ~12 GB |

---

*报告生成：2026-10-22 / StellarCleave-EN 代理*
