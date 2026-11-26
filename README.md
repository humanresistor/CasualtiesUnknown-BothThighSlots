# Both Thigh Slots — Casualties: Unknown

> 未知伤亡（Casualties: Unknown）的 BepInEx 5 插件：腿部挂包（材料包 / 腿包）不再钉死在固定那条腿，**两条腿任意分配、每条腿最多一件、最多两件**。

- 中文 | [English](#english)
- 游戏版本：Casualties: Unknown **1.0.2**（正式版）
- 当前版本：**v1.5.0**

> ### ⚠️ AI 生成声明 / AI Disclosure
> **本项目的全部代码与文档由 AI（DeepSeek Harness 智能体）编写**，人类只提需求、做测试与验收。
> 测试范围仅限单人 / 沙盒模式，未做联机（Casualties: Together）完整回归，请自行备份存档。
> **All code and documentation in this repository were written by an AI assistant**; a human only specified requirements, tested, and accepted the result.
>
> ### 📜 随便用 / Use it freely
> 采用 [MIT](LICENSE) 协议：**任何人都可以自由拿去修改、再发布、商用、闭源二次分发**，只需保留版权与许可声明。
> **自制自用，如有雷同纯属巧合。**

---

## 原版是什么情况

- 两条腿是两个独立肢体：**ThighF = 右大腿**，**ThighB = 左大腿**（游戏 Lang/zh-CN.json 里就这么写的）。
- 腿包 legpouch：槽位 thigh，固定挂 ThighF（右大腿）。
- 材料包 materialpouch：槽位 thighback，固定挂 ThighB（左大腿）。
- 穿戴判定在 Body.WearWearable：

```
Item already = GetWearableBySlotID(item.Stats.wearSlotId);   // 遍历【所有肢体】
if (already != null) { 弹「你已经穿着 …」; return; }          // 同一槽位 id 全身只能一件
Limb limb = LimbByName(item.Stats.desiredWearLimb);          // 然后只挂这条「原定」腿
if (limb.dismembered) { 弹「肢体缺失」; return; }
```

结果就是：**一腿包 + 一材料包可以；两个材料包或两个腿包不行；断腿就彻底装不上**（不会自动换到另一条腿）。

## 本插件怎么改

1. **腿部挂包成组**：默认把 `thigh`（腿包）和 `thighback`（材料包）当作同一组，规则是“每条腿最多一件组内物品，总共最多两件，必须分在两条腿上”。
2. 穿戴时按「原定腿 → 对侧腿」顺序，找第一条**没截肢、且组内一件都没有**的腿；找到就用自己复刻的原版流程挂上去。
3. 两条腿都满时，即使原版因为槽位 id 不同想放行，也会被拦住并提示「你已经穿着 …」。
4. 换到对侧腿时**翻转绘制层级偏移**：材料包原本 −5（画在腿后），换到前腿后改为 +5（画在腿前），否则会“夹在两腿中间”。
5. **悬停描述最上方**加一行 `位置：左大腿` / `Position: Left thigh`（不按 Shift 展开也能看见）。
6. **修复拖不下来**：原版 `Body.GetWearable(itemid)` 只查「原定肢体」，导致换到对侧的装备拖拽脱下失败；插件在其结果为空时**扫描全部肢体兜底**。

## 配置

`BepInEx/config/humanresistor.cu.boththighslots.cfg`

| 键 | 默认 | 说明 |
|---|---|---|
| General / Enabled | true | 总开关 |
| General / VerboseLog | true | 换腿、换层时在日志打印一行 |
| General / MirrorSortingOffset | true | 换侧时翻转排序偏移 |
| General / GroupSlots | thigh,thighback | 视为同一组“腿部挂包”的槽位，逗号分隔 |
| General / ShowLimbInTooltip | true | 悬停描述顶部显示所在腿 |

## 容量规则

| 场景 | 结果 |
|---|---|
| 材料包 + 材料包 | ✅ 一左一右 |
| 腿包 + 腿包 | ✅ 一左一右 |
| 材料包 + 腿包 | ✅ 一左一右 |
| 任何第三件 | ❌ 提示已经穿着 |
| 一条腿截肢 | 只用剩下的那条腿；两条都断才彻底装不上 |

> 插件只在“穿戴/脱下”动作上判定；**已经叠在一起的旧存档不会自动清理**，需要手动把多余的卸下再穿回。

## 安装 / 构建

1. 游戏根目录装 BepInEx 5.4.23.5 x64（出现 winhttp.dll 与 BepInEx 目录）
2. 把 BothThighSlots.dll 放进 BepInEx/plugins/
3. 从 Steam 启动游戏；更新 DLL 前先退出游戏

构建（需要 .NET SDK，本机用 8.0.400 验证）：

```
dotnet build -c Release
# 产物 bin/Release/netstandard2.1/BothThighSlots.dll
```

目标框架必须是 **netstandard2.1**（游戏程序集是 2.1，用 2.0 会报 CS1705），引用游戏 Managed 下的 Assembly-CSharp.dll、UnityEngine.dll、UnityEngine.CoreModule.dll、UnityEngine.Physics2DModule.dll 与 BepInEx/core 的 BepInEx.dll、0Harmony.dll。

## 开发者问题参考（Dev notes）

以下都是做这个插件时真实踩到的坑，按最后修复的形态记录，供后来者避雷。

### 1. 原版唯一性判定是“按槽位 id、扫全身”，不是“按肢体格数”

`GetWearableBySlotID(id)` 遍历 `limbs[]` 全部肢体，所以同一个 wearSlotId 全身只能一件；而不同槽位可以同时挂同一条腿（例如 torsofront 能同时放腰包和随身水包）。想“每条腿一件”必须自己接管判定，不能靠原版。

### 2. 只在“目标腿被占/截肢”时接管会漏两种情形

早期版本（v1.2.0）只在目标腿不可用时才换腿，结果：

- **目标腿空着、但对侧腿占着同槽位**：判定交回原版 → 原版全局查同槽位 → 误弹「已经穿着」，明明有腿位却装不上。
- **同一条腿叠两个不同槽位的包**：原版按槽位判定互不冲突 → 出现“三件同时在身上”的 bug。

修正为“组规则 + 自己接管全部腿部挂包穿戴”，并在两条腿都满且原版本来会放行时主动拦截。

### 3. 换腿后贴图“夹在两腿中间” → 层级偏移要翻符号

原版用 `ItemInfo.wearableVisualOffset` 决定相对自身腿的绘制层级：材料包 −5（画在腿后，因为它天生在后腿）、腿包 +5（画在腿前）。换到对侧腿后仍沿用原偏移，就会画在前腿背后、视觉上夹在两腿之间。修正：换侧时 `offset = -offset`（可用 MirrorSortingOffset 关闭）。

### 4. 贴图本身没有左右变体，靠镜像没用

用 UnityPy 拆 resources.assets 看过：materialpouch 的 sprite 是 12×12、pivot 正中、PPU 8（世界 1.5×1.5）；legpouch 是 10×12。prefab 的 Transform 是 0/1，挂载时代码又把 localPosition/scale/rotation 全重置，所以“官方实现”本质是**层级偏移**，不是像素偏移。材料包贴图左右基本对称，腿包只有背带略不对称——所以方案选层级而不是换图/翻转。

### 5. Harmony 补丁类写了但没 PatchAll → 完全不生效

`Awake()` 里只 `PatchAll(typeof(WearWearablePatch))`，后来新增的悬停描述补丁类忘了注册，表现为“代码看着没毛病但界面毫无变化”。排查办法：日志里搜补丁失败（`Failed to patch`），或确认每个补丁类都被 PatchAll。

### 6. TMP 富文本颜色标签没闭合 → 后面的文字全被染色

插入 `"<color=#91d1ff>位置：左大腿\n"` 忘了 `</color>`，TextMeshPro 会把颜色带到整段描述末尾，表现为“物品描述全变蓝”。补上闭合标签即可。

### 7. 原版 `Body.GetWearable(itemid)` 只查“原定肢体” → 换到对侧的装备拖不下来

`PlayerCamera.TryPerformWorldActions` 用 `body.GetWearable(dragItem.id)` 决定能否脱下，`Body.DropWearable` 内部也用同一个函数；该函数只扫描 `GlobalItems[id].desiredWearLimb` 那一条腿。物品一旦不在原定腿上就返回 null，于是：

- 换到对侧腿的包拖不下来；
- 同 id 两件时能否拖动，取决于“原定腿上那件还在不在”。

修正：Harmony 后置补丁，结果为 null 时扫描全部肢体，按 id 找 wearable 物品兜底。

### 8. 脱装备有 10 像素拖拽门槛

原版判定 `Vector2.Distance(Input.mousePosition, clickPos) > 10f` 才会执行掉落；小于这个距离会被当成“点一下打开容器”。调试时不要用“几乎不动的拖拽”去验证。

### 9. 编译目标框架必须 netstandard2.1

游戏程序集是 netstandard2.1，项目用 netstandard2.0 会直接报 CS1705。另外用到 Rigidbody2D 时要引用 UnityEngine.Physics2DModule。

### 10. 用 UnityPy 读游戏资源时的小坑

需要先 `pip install UnityPy`；`Vector3f` / `Rectf` 之类的结构体不可迭代（不能 `tuple(v)`），要按字段取 `.x/.y/.z` 或 `.x/.y/.width/.height`；MonoBehaviour 的字段要靠 typetree，读不到时只能拿脚本引用名。

---

## English

A BepInEx 5 plugin for **Casualties: Unknown 1.0.2** that lets thigh pouches (material pouch / leg pouch) be distributed freely across both thighs: **at most one per thigh, at most two in total**.

Vanilla binds each item to one specific limb (leg pouch → ThighF/right, material pouch → ThighB/left) and enforces one item per wear-slot id across the whole body, so you can never have two material pouches, and losing the bound thigh makes the item unwearable.

This plugin groups `thigh` and `thighback`, picks the first thigh that is neither dismembered nor already holding a group item (original side first, then the mirrored side), performs the wear itself, flips the visual sorting offset when mirrored, shows the worn limb in the item tooltip, and patches `Body.GetWearable(string)` so items worn on the mirrored thigh can still be dragged off.

Configuration: `BepInEx/config/humanresistor.cu.boththighslots.cfg` (Enabled, VerboseLog, MirrorSortingOffset, GroupSlots, ShowLimbInTooltip).

See [Dev notes](#开发者问题参考dev-notes) above for the implementation pitfalls (slot-id uniqueness, visual offset, Harmony PatchAll, TMP color tags, GetWearable lookup, 10px drag threshold, netstandard2.1).

## License

[MIT](LICENSE) © 2026 humanresistor — free to modify and redistribute, including closed-source.
