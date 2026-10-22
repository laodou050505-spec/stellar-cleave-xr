# STELLAR CLEAVE — 英文版文本表
生成日期：2026-10-22
工程：`/Users/ld/Documents/Codex/EnglishEditions/StellarCleave-EN`
原版：`/Users/ld/Documents/Codex/2026-08-10/pico-xr-unity-3js/切水果`

> 本游戏原版 HUD/UI 已全部使用英文硬编码（由 Unity 实时文本渲染），无中文玩家可见文字需翻译。
> 下表完整记录所有玩家可见文本，同时校对原版英文是否存在语法或用词问题。

---

## 术语表

| 术语 | 英文 | 说明 |
|---|---|---|
| 星际切割（游戏名）| STELLAR CLEAVE | 官方英文名，已确认 |
| 异常协议（副标题）| ANOMALY PROTOCOL | 封面副标题 |
| 青色目标 | Cyan signal | 左手切割 |
| 琥珀色目标 | Amber signal | 右手切割 |
| 紫色目标 | Violet signal | 双手交替，0.9s 内 |
| 红色目标 | Red hazard | 避开，勿切 |
| 连击 | Chain | 连续切中计数 |
| 超限驱动 | Overdrive | 连击满后的强化阶段 |
| 生命 | Life / Lives | 剩余生命数 |
| 扫描房间 | Scan Room | 扫描功能按钮 |
| 天花板锚定 | Ceiling Anchored | MR 天花板识别成功 |
| 未映射天花板 | Ceiling Not Mapped | MR 天花板识别失败 |
| 预览模式 | Preview | 无实际天花板，仅设计位置预览 |
| 收容完成 | Containment Complete | 胜利结算标题 |
| 生命链路中断 | Life Link Lost | 失败结算标题 |
| 重新游玩 | Play Again | 重试按钮 |
| 退出 | Exit | 退出按钮 |

---

## 文本清单

### 封面图片（Assets/Resources/CosmicSliceMenu/Cover.png）
| key | 原文 | 英文 | 来源 | 使用场景 | 状态 |
|---|---|---|---|---|---|
| cover.title | （图片烘焙）| STELLAR CLEAVE | Cover.png（Tripo 生成）| 主封面标题 | ✓ 已确认英文，逐字核对无误 |
| cover.subtitle | （图片烘焙）| ANOMALY PROTOCOL | Cover.png（Tripo 生成）| 主封面副标题 | ✓ 已确认英文 |
| cover.corner.tl | （图片烘焙）| ANOMALY 07 | Cover.png | 左上角装饰文字 | ✓ 已确认英文 |
| cover.corner.tr | （图片烘焙）| LOCAL LINK | Cover.png | 右上角装饰文字 | ✓ 已确认英文 |
| cover.corner.bl | （图片烘焙）| DUAL BLADES | Cover.png | 左下角装饰文字 | ✓ 已确认英文 |
| cover.corner.br | （图片烘焙）| CONTAINMENT | Cover.png | 右下角装饰文字 | ✓ 已确认英文 |

### 封面菜单按钮（CosmicSliceGame.Cover.cs，运行时文本）
| key | 原文 | 英文 | 来源文件:行 | 使用场景 | 状态 |
|---|---|---|---|---|---|
| cover.btn.start | — | START GAME | Cover.cs:383,388 | 主页"开始游戏"按钮 | ✓ 原版已英文，用词准确 |
| cover.btn.howtoplay | — | HOW TO PLAY | Cover.cs:384 | 主页"如何游玩"按钮 | ✓ 原版已英文，用词准确 |
| cover.btn.exit | — | EXIT | Cover.cs:385 | 主页"退出"按钮 | ✓ 原版已英文 |
| cover.btn.back | — | BACK | Cover.cs:387 | 说明页"返回"按钮 | ✓ 原版已英文 |
| cover.status.ready | — | ANOMALY PROTOCOL READY | Cover.cs:389 | 封面底部状态提示 | ✓ 原版已英文，语义准确 |
| cover.status.connecting | — | CONNECTING TO YOUR ROOM | Cover.cs:132 | 点击 START 后过渡状态 | ✓ 原版已英文，语义准确 |
| cover.status.starting | — | YOUR OPENING SURVEY WILL BEGIN SHORTLY | Cover.cs:133 | 启动扫描前提示 | ✓ 原版已英文，语义准确 |
| cover.status.exit_confirm | — | EXIT REQUEST ACKNOWLEDGED | Cover.cs:132 | 点击 EXIT 后确认状态 | ✓ 原版已英文 |
| cover.status.exit_ending | — | ENDING LOCAL SESSION | Cover.cs:133 | 退出中状态 | ✓ 原版已英文 |
| cover.status.exit_pico | — | EXIT TO PICO HOME | Cover.cs:227 | 退出到主界面 | ✓ 原版已英文 |
| cover.prompt.gamepad | — | POINT + TRIGGER TO SELECT   /   ESC TO RETURN | Cover.cs:242 | 手柄操作提示 | ✓ 原版已英文，标点正确 |
| cover.prompt.desktop | — | POINT + TRIGGER   /   MOUSE CLICK   /   ARROWS + ENTER | Cover.cs:242 | 桌面操作提示 | ✓ 原版已英文 |

### 封面选项悬停文字（Cover.cs）
| key | 英文 | 来源:行 | 使用场景 | 状态 |
|---|---|---|---|---|
| cover.hover.0 | ANOMALY PROTOCOL READY | Cover.cs:220 | 悬停 START GAME 时 | ✓ |
| cover.hover.1 | EXIT TO DESKTOP | Cover.cs:221 | 悬停 EXIT 时 | ✓ |
| cover.hover.2 | FOUR SIGNALS. TWO BLADES. ONE PROTOCOL. | Cover.cs:222 | 悬停 HOW TO PLAY 时 | ✓ 语义准确 |
| cover.hover.3 | RETURN TO MAIN MENU | Cover.cs:223 | 悬停 BACK 时 | ✓ |
| cover.hover.4 | READY TO CONTAIN THE ANOMALY | Cover.cs:224 | 悬停说明页 START GAME 时 | ✓ |

### 说明页（"HOW TO PLAY"页，Cover.cs）
| key | 英文 | 来源:行 | 使用场景 | 状态 |
|---|---|---|---|---|
| guide.title | HOW TO PLAY | Cover.cs:454 | 说明页标题 | ✓ |
| guide.goal | 75 SECONDS. 3 LIVES. | Cover.cs:455 | 说明页副标题 | ✓ |
| guide.rule.cyan.title | LEFT / CYAN | Cover.cs:457 | 规则条：左/青色 | ✓ |
| guide.rule.cyan.body | Cut cyan signals\nwith your left blade. | Cover.cs:457 | 规则说明 | ✓ 语法正确 |
| guide.rule.amber.title | RIGHT / AMBER | Cover.cs:458 | 规则条：右/琥珀色 | ✓ |
| guide.rule.amber.body | Cut amber signals\nwith your right blade. | Cover.cs:458 | 规则说明 | ✓ |
| guide.rule.violet.title | DUAL / VIOLET | Cover.cs:459 | 规则条：双手/紫色 | ✓ |
| guide.rule.violet.body | Use one hand, then the\nother within 0.9s. | Cover.cs:459 | 规则说明 | ✓ |
| guide.rule.red.title | AVOID / RED | Cover.cs:460 | 规则条：避开/红色 | ✓ |
| guide.rule.red.body | Let red hazards pass.\nNever cut them. | Cover.cs:460 | 规则说明 | ✓ |
| guide.life_rule | Wrong hand or missed non-red signal: lose 1 life. | Cover.cs:461 | 生命规则说明 | ✓ 语法正确 |
| guide.input.xr | Swing your blades, or aim + pull the trigger. | Cover.cs:463 | XR 操作说明 | ✓ |
| guide.input.desktop | Left click: cyan blade   /   Right click: amber blade | Cover.cs:465 | 桌面操作说明 | ✓ |

### HUD 品牌与天花板状态（UI.cs）
| key | 英文 | 来源:行 | 使用场景 | 状态 |
|---|---|---|---|---|
| hud.brand | STELLAR CLEAVE | UI.cs:BuildHudFrame | 左上品牌标识 | ✓ |
| hud.ceiling.not_mapped | CEILING NOT MAPPED | UI.cs:RefreshConsolePresentation | 未识别天花板 | ✓ 如实表达限制 |
| hud.ceiling.anchored | CEILING ANCHORED | UI.cs:RefreshConsolePresentation | 天花板识别成功 | ✓ |
| hud.ceiling.preview | PREVIEW / CEILING NOT MAPPED | UI.cs:RefreshConsolePresentation | 预览模式（明确标注未映射）| ✓ 符合规范要求 |

### HUD 分数/计时/生命（UI.cs，动态文本）
| key | 格式 | 来源 | 状态 |
|---|---|---|---|
| hud.score.label | SCORE | UI.cs:BuildHud | ✓ |
| hud.score.value | {score:000000} | UI.cs:UpdateHud | ✓ 6位补零 |
| hud.timer.label | TIME REMAINING | UI.cs:BuildHud | ✓ |
| hud.timer.value | {mm:02}:{ss:02} | UI.cs:UpdateHud | ✓ |
| hud.round.label | ANOMALY PROTOCOL | UI.cs:BuildHud | ✓ 品牌一致 |
| hud.lives.value | LIVES  {lives} / {max} | UI.cs:UpdateHud | ✓ 双空格格式对齐 |
| hud.combo | CHAIN {combo:00} | UI.cs:UpdateHud | ✓ |
| hud.overdrive | OVERDRIVE {timer:0.0} | UI.cs:UpdateHud | ✓ 显示剩余时间 |

### 教程面板（UI.cs:BuildTutorial）
| key | 英文 | 来源 | 状态 |
|---|---|---|---|
| tutorial.title | TWO BLADES. FOUR SIGNALS. | UI.cs:BuildTutorial | ✓ 简洁有力 |
| tutorial.rule.cyan | CYAN / Left blade | UI.cs:BuildTutorial | ✓ |
| tutorial.rule.amber | AMBER / Right blade | UI.cs:BuildTutorial | ✓ |
| tutorial.rule.violet | VIOLET / Both hands | UI.cs:BuildTutorial | ✓ |
| tutorial.rule.red | RED / Do not cut | UI.cs:BuildTutorial | ✓ |
| tutorial.body | Violet: alternate hands within 0.9s.\nWrong hand or missed non-red signal: lose 1 life. | UI.cs:BuildTutorial | ✓ 语法正确 |
| tutorial.btn.start | START GAME | UI.cs:BuildTutorial | ✓ |
| tutorial.btn.scan | SCAN ROOM | UI.cs:BuildTutorial | ✓ |

### 规则条（UI.cs:BuildRuleStrip，玩法中常驻显示）
| key | 英文 | 来源 | 状态 |
|---|---|---|---|
| rulestrip.left | LEFT / CYAN | UI.cs:BuildRuleStrip | ✓ |
| rulestrip.dual | DUAL / 0.9s | UI.cs:BuildRuleStrip | ✓ |
| rulestrip.right | RIGHT / AMBER | UI.cs:BuildRuleStrip | ✓ |

### 结算面板（UI.cs:BuildResultPanel）
| key | 英文 | 来源 | 状态 |
|---|---|---|---|
| result.heading.win | CONTAINMENT COMPLETE | CosmicSliceGame.cs:635 | ✓ 胜利结算 |
| result.heading.lose | LIFE LINK LOST | CosmicSliceGame.cs:635 | ✓ 失败结算 |
| result.rank_caption | RANK | UI.cs:BuildResultPanel | ✓ |
| result.score_caption | FINAL SCORE | UI.cs:BuildResultPanel | ✓ |
| result.detail | BEST CHAIN  {bestCombo:00}          LIVES  {lives}/{max} | CosmicSliceGame.cs:639 | ✓ 包含多空格对齐 |
| result.btn.retry | PLAY AGAIN | UI.cs:BuildResultPanel | ✓ 用词符合游戏惯例 |
| result.btn.exit | EXIT | UI.cs:BuildResultPanel | ✓ |

### 扫描开场状态（CosmicSliceGame.cs:Opening/Ceiling）
| key | 英文 | 来源 | 使用场景 | 状态 |
|---|---|---|---|---|
| scan.opening | OPENING ROOM LINK\nLOOK UP TOWARD THE CEILING | CosmicSliceGame.cs:392 | 开场扫描引导 | ✓ |
| scan.anchored_preparing | CEILING ANCHORED\nPREPARING THE ANOMALY | CosmicSliceGame.cs:433 | 天花板已锁定，准备中 | ✓ |
| scan.survey_complete | SURVEY COMPLETE\nCHECKING YOUR CEILING | CosmicSliceGame.cs:433 | 扫描完成，检查中 | ✓ |
| scan.ceiling_status | CEILING ANCHORED\nREADY TO PLAY | Ceiling.cs:146 | 天花板成功且可开始 | ✓ |
| scan.preview_status | PREVIEW POSITION\nNOT ATTACHED TO YOUR CEILING | Ceiling.cs:146 | 预览模式，明确未贴合 | ✓ 如实表达限制 |
| scan.not_found | CEILING NOT FOUND\nSCAN YOUR ROOM OR TRY A PREVIEW | Ceiling.cs:146 | 未找到天花板 | ✓ |
| scan.in_progress | LOOK UP TO FIND THE CEILING\nROOM SCAN IN PROGRESS | Ceiling.cs:146 | 扫描进行中 | ✓ |

### 按钮动态标签（UI.cs:RefreshConsolePresentation）
| 条件 | 英文 | 使用场景 | 状态 |
|---|---|---|---|
| CanUsePortal=true | START GAME | 可以开始游戏 | ✓ |
| 扫描完成但无天花板 | TRY PREVIEW | 建议尝试预览 | ✓ 如实表达限制 |
| 扫描中 | SCANNING... | 扫描进行中 | ✓ |

### 玩法事件通知（CosmicSliceGame.cs:SetEvent/LoseLife）
| key | 英文 | 来源:行 | 使用场景 | 状态 |
|---|---|---|---|---|
| event.run_active | CONTAINMENT RUN ACTIVE | .cs:472 | 游戏开始时通知 | ✓ |
| event.overdrive_complete | OVERDRIVE COMPLETE | .cs:488 | 超限驱动结束 | ✓ |
| event.score_chain | +{awarded:000}   CHAIN {combo:00} | .cs:782 | 切中目标得分 | ✓ |
| event.overdrive | OVERDRIVE | .cs:791 | 进入超限驱动 | ✓ |
| event.dual_charged | DUAL LINK CHARGED  >  SECOND BLADE | .cs:739 | 紫色目标第一刀后提示 | ✓ 语义清晰 |
| event.dual_cut | DUAL LINK  >  CUT WITH OTHER BLADE | .cs:747 | 提示换手 | ✓ |
| event.hazard_evaded | HAZARD EVADED  +035 | .cs:821 | 成功避开红色 | ✓ 固定分值 +035 |
| event.wrong_left | WRONG BLADE  L | .cs:808 | 用错手（左手） | ✓ L=Left 缩写 |
| event.wrong_right | WRONG BLADE  R | .cs:808 | 用错手（右手） | ✓ R=Right 缩写 |
| event.singularity_hit | SINGULARITY HIT | .cs:728 | 红色目标被切中扣血 | ✓ |
| event.signal_missed | SIGNAL MISSED | .cs:831 | 信号超时未切扣血 | ✓ |
| event.life_lost | {reason}   LIFE {lives}/{max} | .cs:839 | 扣血后显示剩余生命 | ✓ |

### ProductName / 应用标签
| 位置 | 原文 | 英文 | 状态 |
|---|---|---|---|
| ProjectSettings.asset:productName | 星际切割 | STELLAR CLEAVE | ✓ 已修改 |
| CosmicSliceProjectBuilder.cs:119 | 星际切割 | STELLAR CLEAVE | ✓ 已修改 |
| Android 包名 | com.anomalylab.cosmicslice | com.anomalylab.cosmicslice.en | ✓ 已修改 |

---

## 英文校对意见

所有已有英文文本语法正确，用词符合游戏行业惯例，无需修改。以下几处特别确认：

1. **天花板状态**：`PREVIEW / CEILING NOT MAPPED` 和 `PREVIEW POSITION / NOT ATTACHED TO YOUR CEILING` 均如实标明了预览模式的限制，符合规范要求。
2. **`TRY PREVIEW`** 按钮文字准确表达"尝试预览（非真实天花板）"的语义。
3. **HAZARD EVADED +035**：固定分值奖励写死在代码中，不是文本翻译问题。
4. **DUAL LINK CHARGED > SECOND BLADE**：语义清晰，">" 表示行动指向，通顺。
5. **结算标题**：CONTAINMENT COMPLETE（收容完成）/LIFE LINK LOST（生命链路中断）—— 符合游戏科幻主题风格。

---

## 图片资源确认

| 图片 | 路径 | 中文文字 | 状态 |
|---|---|---|---|
| 封面 | Resources/CosmicSliceMenu/Cover.png | 无 | ✓ 全英文，逐字核对：STELLAR CLEAVE / ANOMALY PROTOCOL / ANOMALY 07 / LOCAL LINK / DUAL BLADES / CONTAINMENT |
| 说明插图 | Resources/CosmicSliceMenu/Guide.png | 无 | ✓ 纯插画，无任何文字 |
| HUD 底图 | CosmicSlice/Art/UI/AnomalyHUD_Spatial.png | 无 | ✓ 纯装饰背景图，无文字 |
| 星云纹理 | Resources/CosmicSliceNebula/NebulaWisps.png | 无 | ✓ 纯纹理 |
| AppIcon | Assets/AppIcon/AppIcon.png | — | 原版文件缺失，英文版用 Tripo 生成（task_id: c07c2a52-674d-46d1-b806-9205496e5f0a）|

---

## 字体说明

游戏使用 Unity 内置字体 `LegacyRuntime.ttf`（即 Arial/Helvetica 系列），为全 ASCII 字体，天然支持所有英文字符。无需额外字体处理。原版中文字体回退在英文版中不存在（无中文文本）。
