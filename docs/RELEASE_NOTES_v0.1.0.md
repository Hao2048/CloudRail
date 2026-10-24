# CloudRail v0.1.0 — 云上列车

首个开源展示版本。以观看火车穿越浮空群岛为核心，包含当前完整视觉场景、Unity 源代码和 macOS 试玩包。

## 本版内容

- 云端层叠城堡、钟塔、花园、风车、瀑布与沿线景观。
- 平滑展开的拱桥基础、枕木和钢轨。
- 含直线、弯道、上坡和下坡的火车路线。
- 指定路段的鸟群盘旋、拍翼和伴飞。
- 修复缺面的模型云、带自然细节的岩壁。
- 环绕、切换机位、滚轮与鼠标中键缩放。
- 可选的下车探索、星光收集、站点旅行与原创合成声音。

## 下载

- **CloudRail-v0.1.0-macOS-universal.dmg**：打开后将应用拖到 Applications。
- **CloudRail-v0.1.0-macOS-universal.zip**：便携压缩包，解压后运行应用。
- **CloudRail-Showcase-1080p.mp4**：约31秒新版实机展示片，15–23秒包含鸟群伴飞近景。
- **SHA256SUMS.txt**：安装包与展示片校验值。

Mac 包面向 **macOS 12+ / Apple Silicon 与 Intel**。当前版本没有 Developer ID 签名或 Apple 公证；首次打开若被阻止，请参照 README 中链接的 Apple 官方说明。

## 源码

Unity **6000.6.0f1**，URP **17.6.0**。打开 `Assets/Scenes/CloudRail_Visual.unity` 并点击 Play。MIT 协议；字体等第三方内容保留各自协议。

这是持续打磨的视觉原型。Intel 架构包含在 Universal 包中，本次本机运行检查使用 Apple Silicon；本次未发布 Windows / Linux 二进制。
