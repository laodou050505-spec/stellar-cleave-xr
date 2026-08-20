# Product

<!-- impeccable:product-schema 1 -->

## Platform

adaptive

## Users

PICO XR 玩家，在自己的客厅或房间内站立游玩，使用双控制器完成短时、身体参与感强的切割挑战。

## Product Purpose

《Cosmic Slice — Anomaly Protocol》把玩家真实天花板变成异常入口：扫描空间后，玩家用双色能量刀处理从黑洞坠落的异象物，在三条命用尽前完成收容。

## Positioning

游戏的舞台由玩家真实房间决定；太空异常以局部、可感知的方式侵入现实，而不是把用户放进一个普通的虚拟太空关卡。

## Operating Context

首局必须在无需说明书的情况下理解三类信号：青色用左手青刀、琥珀色用右手琥珀刀、红色奇点避开。桌面版作为无头显开发和演示回退。

## Capabilities and Constraints

- Unity 6000.5.5f1，官方 PICO XR SDK 6.0.0。
- 目标 PICO Android，ARM64、IL2CPP、Multiview 和 PICO passthrough/scene capture 已配置。
- 当前没有真实头显，需保留桌面鼠标回退与自动化 QA。
- UI 的关键文字必须由 Unity 绘制，不能依赖图像中的生成文字。

## Brand Commitments

高级、克制的太空异常感；真实空间始终可感知。拒绝廉价的赛博霓虹和杂乱的通用科幻道具。

## Evidence on Hand

- 运行场景：`Assets/CosmicSlice/Scenes/CosmicSliceDemo.unity`
- 当前玩法、控制器与 PICO 配置已实现。
- 九个用户提供的 FBX 已作为远景飞船、行星、卫星和陨石使用；程序化行星不参与最终场景布置。

## Product Principles

1. 真实房间是舞台，不被虚拟背景吞没。
2. 每个判定规则先被看懂，再被要求反应。
3. 视觉反馈让切中、错误和危险在一瞬间可辨。
4. 太空感来自尺度、光与异常物理，不来自堆砌霓虹。
