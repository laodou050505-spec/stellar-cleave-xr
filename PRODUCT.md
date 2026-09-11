# Product

<!-- impeccable:product-schema 1 -->

## Platform

android

Unity 世界空间 UI，PICO 原生 MR 游戏；macOS 版用于无头显开发与 QA。

## Users

PICO XR 玩家，在自己的客厅或房间内站立游玩，使用双控制器完成短时、身体参与感强的切割挑战。

## Product Purpose

《星际切割 / STELLAR CLEAVE — ANOMALY PROTOCOL》让虫洞与流星闯入玩家的真实房间：游戏读取天花板语义锚点的位置、朝向与多边形边界，把虫洞放在边界内、表面下方 18mm，并按可用空间缩放；玩家在持续可见的真实空间中，用双色能量刀处理异常目标。

## Positioning

当前权威方向是 MR，不再使用纯沉浸式宇宙背景。PICO 玩法阶段显示真实房间透视，隐藏天空盒、星球、飞船和中央宇宙尘；Scene Capture 提供天花板的位置、朝向与边界。虫洞可在 X/Z 上移入真实边界，但不移动玩家、竞技场地标或世界空间 UI；找不到有效天花板时隐藏虫洞，只有用户明确选择 TRY PREVIEW 才使用标明未贴合天花板的设计位置。

## Operating Context

首局必须在无需说明书的情况下理解四类信号：青色用左手青刀、琥珀色用右手琥珀刀、紫色须双手在 0.9 秒内接续、红色奇点避开。玩家既可直接挥刀，也可用左右手柄各自的 8 米世界空间射线配合扳机处理远处目标。桌面版保留鼠标与键盘回退。

## Capabilities and Constraints

- Unity 6000.5.5f1，官方 PICO XR SDK 6.0.0。
- 目标 PICO Android，Input System-only、PICO Loader、XR App Mode、Multiview、ARM64、IL2CPP 与 Android Activity 已序列化并通过离线验证。
- PICO 端启用 Video See-Through、Scene Capture 与 MR Safeguard；Spatial Mesh、Plane Detection 和外部 MRC 录制保持关闭。
- 2026-09-09：当前0.1.5 final2 / versionCode6已于18:19:28安装，设备哈希与本地一致。虫洞入口优先沿当前视线选择天花板落点，并受真实边界约束；连续洞体向上1.85m收窄，洞芯居中，新增独立星云光雾羽化口沿。0.1.3的自动双波扫描、封面和流星不变。仍坚持自动开始、不增加按键；VST状态不是像素亮度检测，不能承诺完全零黑屏；实体天花板仍待真机验收。
- 用户于 2026-09-08 允许封面使用生成的英文和字体。标题可由 Tripo Nano Banana 生成；所有可见按钮必须是真实交互控件。规则和按钮文字当前使用 Unity 文本，以确保清晰与可验证。

## Launch Presentation

- 封面与 HOW TO PLAY 整页外缘以 0.16 双轴柔和羽化融入纯黑空间，按钮和文字保持清晰；不把介绍页局部插画当作整页羽化的替代。
- 用户 2026-09-09 11:11:41 截图确认当前封面位置合适：标题接近视线中心、按钮在下方、上方有黑色留白。保留现有 3m 世界空间距离、1.72m 面板中心高度、标题专属 1.35 倍缩放；不继续放大/抬高，也不改成跟头贴脸界面。模拟器重启已复查，实际投影仍会随头部追踪变化。
- VST 在不透明黑色封面后提前准备。START 等系统准备与 XR/相机及已知 VST 显示状态，再 0.38 秒淡入房间，独立准备段 2 秒（无 VST 回报时保守为 4 秒）后自动扫描；不增加操作。失焦或异常恢复帧不得消耗演出。
- 扫描以玩家当前眼位 X/Z 投影到追踪地面为起点，首圈半径 0.08m，发出后固定在世界空间。两波同速 1.5m/s，第二波延后 1.2 秒、保持 1.8m 间距，亮度为主波 78%；总演出 6.8 秒。网格按主波半径裁切显现，近到远扩散，末段才显示环境网格、虫洞和 HUD。

## Brand Commitments

所有 UI 延续封面：近黑松绿底、珊瑚细线、薄荷白字和充足留白。高级、克制的“异常侵入真实房间”感。虚拟内容只保留虫洞、玩法目标、左右持续流星与必要的世界空间仪表；不再用天空盒、星球、飞船或房间中央的宇宙尘遮蔽真实环境。

## Evidence on Hand

- 2026-09-10音乐同步：用户明确不换模型，仅改节奏。小节重拍/半小节/Overdrive逐拍出场，180ms入场补偿，卡顿/满场跳过而不积压，停播无声不生成，恢复/回退/循环重新对齐。桌面QA 128/128通过、44截图，报告 `outputs/CosmicSlice-Music-Sync-20260910-QA/qa-report.json`；音乐实时采样记录同目录 `music-sync-live.csv`。打包和安装状态以最新交付说明为准。

- 2026-09-09 局部动态星云版：0.1.6 / versionCode7，虫洞周围新增两层蓝紫细丝云气、稀疏星尘与自然羽化；只作用于虫洞附近，不恢复全景宇宙背景。使用 Tripo Nano Banana 生成贴图，任务 `18e74f6c-604e-4e71-8a7e-be223bc05ebb` 成功，5 credits。来源/提示词见 `Assets/Resources/CosmicSliceNebula/ASSET-SOURCES.md`。
- 最新桌面QA：`outputs/CosmicSlice-Nebula-Final-20260909-QA/qa-report.json`，107通过、0失败、64截图（含20帧动态序列）；动图 `outputs/CosmicSlice-Nebula-20260909/nebula-motion.gif`。Android构建与安装状态以该输出目录的交付说明和知识库最新状态为准。以下0.1.5数据为上一版归档，不代表当前星云版本。
- 运行场景：`Assets/CosmicSlice/Scenes/CosmicSliceDemo.unity`。
- 当前桌面 QA：103/103、0失败、41张实际渲染截图，报告 `/Users/ld/Documents/Codex/2026-08-10/pico-xr-unity-3js/outputs/CosmicSlice-View-Centered-Feathered-Final-20260909-QA/qa-report.json`。覆盖扫描、菜单、流星、玩法、向上纵深、居中与羽化，含正常、正下方、斜视及浅色背景。Ceiling输入是合成测试数据，不证明实体房间扫描。
- 当前 PICO APK：`/Users/ld/Documents/Codex/2026-08-10/pico-xr-unity-3js/outputs/CosmicSlice-PICO-0.1.5-final2-20260909/CosmicSlice-PICO.apk`，56,476,483 bytes，SHA256 `1de70d0ae9d086e55269ed39949d3be56f13e264a8fa69453b0f788176d4d097`，18:19:28已安装并核对设备哈希；通用输出同内容。PICO配置全部通过；模拟器前台仍为地宫争锋，未切走，本版PICO虫洞画面尚未验收。最终Android构建和验证日志在 `work/center-feather-20260909/build-android-final2.log`、`validate-pico-final2.log`。
- 用户提供的星球、飞船、卫星等远景素材仍保留在工程资源中，但当前 MR 玩法阶段不显示它们。

## Product Principles

1. 视觉模式保持单一：玩法阶段呈现真实房间透视与少量空间异常，不叠加沉浸式宇宙背景。
2. 有效天花板必须具备语义、朝向与边界；预览必须明确标识，不能把设计高度冒充真实天花板。玩家、玩法竞技场地标和 UI 保持稳定。
3. 每个判定规则先被看懂，再被要求反应；左右手射线必须给出清楚的目标、错误和触发反馈。
4. 生成节奏与虫洞反馈跟随正在播放的音乐采样时钟，而不是独立漂移的计时器。
5. 流星在身体左右各 1.85 米处发射，以进场/重置水平头向近似身体朝向；转头不转动左右分区，行走只移动新发射区，已生粒子和拖尾保持世界空间。中央区域留给真实房间、虫洞与可切目标。
