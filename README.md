# Both Thigh Slots — Casualties: Unknown

> 未知伤亡（Casualties: Unknown）的 BepInEx 5 插件：腿部挂包（材料包 / 腿包）不再钉死在固定那条腿，**两条腿任意分配、每条腿最多一件、总最多两件**。

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

游戏是 **2D 横版侧视图，角色默认面朝右边**。屏幕上“看起来在前还是在后”并不由坐标决定，而由**图层顺序**决定：越靠近观众的图层画在越上面。

**1. 两条腿是两套独立的图层**
角色左腿、右腿是两个独立肢体：ThighF（右大腿，**靠前、更靠近观众**，图层在上）和 ThighB（左大腿，**靠后、远离观众**，图层在下）。它们各自带 SpriteRenderer 和 sortingOrder，所以前腿会遮住后腿的一部分。

**2. 装备和腿是硬绑的**
腿包 legpouch 固定挂 ThighF（右腿），材料包 materialpouch 固定挂 ThighB（左腿）。玩家不能选边；那条腿断了，这件装备就彻底装不上。

**3. 前后关系由“偏移”决定**
穿上时物品的坐标被重置到腿的中心（位置、缩放、旋转全部归零），唯一影响观感的是：

```
物品 sortingOrder = 所在腿的 sortingOrder + ItemInfo.wearableVisualOffset
```

| 物品 | 偏移 | 结果 |
|---|---|---|
| 腿包 | +5 | 画在自己那条腿**之上** → 更靠近观众 → **永远显示在角色靠近玩家的一侧** |
| 材料包 | −5 | 画在自己那条腿**之下** → 更远离观众 → **永远在角色远离玩家的一侧，被身体挡掉一部分** |

从后往前的图层依次是：**材料包 → 后腿 → 躯干 / 前腿 → 腿包**。这就是“腿包一定在靠近玩家方向、材料包一定在远离玩家方向”的由来。

**4. 槽位唯一性是全局的**
Body.WearWearable 用 GetWearableBySlotID(wearSlotId) 扫全身肢体查重，所以同一个槽位 id 全身只能有一件：thigh 最多一件腿包、thighback 最多一件材料包。不同槽位互不冲突，因此“一腿包 + 一材料包”可以共存，“两个材料包”不行。

**5. 这就是为什么会“夹在两腿中间”**
放开第二件同种物品后，第二个材料包只能挂到前腿 ThighF，但它的偏移仍然是 **−5**，于是被画在**前腿之后、后腿之前**的那条夹缝里。两张贴图尺寸完全一样（12×12 像素、1.5×1.5 世界单位、pivot 居中），叠在一起就像两腿之间又冒出一个包，几乎看不出它是穿在前腿上的第二件。

**6. 两个材料包时，谁在上层**
本插件换到对侧腿时会把偏移**取反**，于是：

| 位置 | 偏移 | 图层 |
|---|---|---|
| 前腿（右腿）上的材料包 | 前腿 + 5 | **上层，最靠近观众**（盖住腿） |
| 后腿（左腿）上的材料包 | 后腿 − 5 | **下层，最远离观众**（被腿盖住） |

不取反（沿用原版固定值）时，前腿那件会被压在前腿后面，两件一起挤在夹缝里，都显得“在腿后”。两个**腿包**同理：前腿那个 +5 在上层，换到后腿的那个取反成 −5，落在下层。

## 本插件怎么改

1. **腿部挂包成组**：默认把 thigh（腿包）和 thighback（材料包）当作同一组，规则是“每条腿最多一件组内物品，总最多两件，必须分在两条腿上”。
2. 穿戴时按「原定腿 → 对侧腿」顺序，找第一条**没截肢、且组内一件都没有**的腿；找到就用自己复刻的原版流程挂上去。
3. 两条腿都满时，即使原版因为槽位 id 不同想放行，也会被拦住并提示「你已经穿着 …」。
4. 换到对侧腿时**翻转绘制层级偏移**：材料包原本 −5（画在腿后），换到前腿后改为 +5（画在腿前），否则会“夹在两腿中间”。
5. **悬停描述最上方**加一行 位置：左大腿 / 右大腿（不按 Shift 展开也能看见）。
6. **修复拖不下来**：原版 Body.GetWearable(itemid) 只查「原定肢体」，导致换到对侧的装备拖拽脱下失败；插件在其结果为空时**扫描全部肢体兜底**。

## 配置

配置文件：BepInEx/config/humanresistor.cu.boththighslots.cfg

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
| 一条腿截肢 | 只用剩下的那条腿 |
| 两条腿都截肢 | 腿包 / 材料包都彻底装不上 |

> 插件只在“穿戴 / 脱下”动作上判定；**已经叠在一起的旧存档不会自动清理**，需要手动把多余的卸下再穿回。

## 安装 / 构建

1. 游戏根目录装 BepInEx 5.4.23.5 x64（出现 winhttp.dll 与 BepInEx 目录）
2. 把 BothThighSlots.dll 放进 BepInEx/plugins/
3. 从 Steam 启动游戏；更新 DLL 前先退出游戏

构建（需要 .NET SDK，本机用 8.0.400 验证）：

```
dotnet build -c Release
# 产物 bin/Release/netstandard2.1/BothThighSlots.dll
```

目标框架必须是 **netstandard2.1**（游戏程序集是 2.1，用 2.0 会报 CS1705），引用游戏 Managed 下的 Assembly-CSharp.dll、UnityEngine.dll、UnityEngine.CoreModule.dll、UnityEngine.Physics2DModule.dll，以及 BepInEx/core 的 BepInEx.dll、0Harmony.dll。

## 开发者问题参考（Dev notes）

以下都是做这个插件时真实踩到的坑，按最后修复的形态记录，供其他开发者避雷。（比较关键的是 1、2、3、7）

### 1. 原版唯一性判定是“按槽位 id、扫全身”，不是“按肢体格数”

GetWearableBySlotID(id) 遍历 limbs[] 全部肢体，所以同一个 wearSlotId 全身只能一件；而不同槽位可以同时挂同一条腿。想“每条腿一件”必须自己接管判定，不能靠原版。

### 2. 只在“目标腿被占 / 截肢”时接管会漏两种情形

早期版本（v1.2.0）只在目标腿不可用时才换腿，结果：

- **目标腿空着、但对侧腿占着同槽位**：判定交回原版 → 原版全局查同槽位 → 误弹「已经穿着」，明明有腿位却装不上。
- **同一条腿叠两个不同槽位的包**：原版按槽位判定互不冲突 → 出现“三件同时在身上”的 bug。

修正为“组规则 + 自己接管全部腿部挂包穿戴”，并在两条腿都满且原版本来会放行时主动拦截。

### 3. 换腿后贴图“夹在两腿中间” → 层级偏移要翻符号

原版用 ItemInfo.wearableVisualOffset 决定相对自身腿的绘制层级：材料包 −5（画在腿后，因为它天生在后腿）、腿包 +5（画在腿前）。换到对侧腿后仍沿用原偏移，就会画在前腿背后、视觉上夹在两腿之间。修正：换侧时 offset = −offset（可用 MirrorSortingOffset 关闭）。

### 4. 贴图本身没有左右变体，靠镜像没用

用 UnityPy 拆 resources.assets 看过：materialpouch 的 sprite 是 12×12、pivot 正中、PPU 8（世界 1.5×1.5）；legpouch 是 10×12。prefab 的 Transform 是 0/1，挂载时代码又把 localPosition / scale / rotation 全重置，所以“官方实现”本质是**层级偏移**，不是像素偏移。材料包贴图左右基本对称，腿包只有背带略不对称——所以方案选层级而不是换图 / 翻转。

### 5. Harmony 补丁类写了但没 PatchAll → 完全不生效

Awake() 里只 PatchAll(typeof(WearWearablePatch))，后来新增的悬停描述补丁类忘了注册，表现为“代码看着没毛病但界面毫无变化”。排查办法：日志里搜补丁失败（Failed to patch），或确认每个补丁类都被 PatchAll。

### 6. TMP 富文本颜色标签没闭合 → 后面的文字全被染色

插入“<color=#91d1ff>位置：左大腿\n”忘了 </color>，TextMeshPro 会把颜色带到整段描述末尾，表现为“物品描述全变蓝”。补上闭合标签即可。

### 7. 原版 Body.GetWearable(itemid) 只查“原定肢体” → 换到对侧的装备拖不下来

PlayerCamera.TryPerformWorldActions 用 body.GetWearable(dragItem.id) 决定能否脱下，Body.DropWearable 内部也用同一个函数；该函数只扫描 GlobalItems[id].desiredWearLimb 那一条腿。物品一旦不在原定腿上就返回 null，于是：

- 换到对侧腿的包拖不下来；
- 同 id 两件时能否拖动，取决于“原定腿上那件还在不在”。

修正：Harmony 后置补丁，结果为 null 时扫描全部肢体，按 id 找 wearable 物品兜底。

### 8. 脱装备有 10 像素拖拽门槛

原版判定 Vector2.Distance(Input.mousePosition, clickPos) > 10f 才会执行掉落；小于这个距离会被当成“点一下打开容器”。调试时不要用“几乎不动的拖拽”去验证。

### 9. 编译目标框架必须 netstandard2.1

游戏程序集是 netstandard2.1，项目用 netstandard2.0 会直接报 CS1705。另外用到 Rigidbody2D 时要引用 UnityEngine.Physics2DModule。

### 10. 用 UnityPy 读游戏资源时的小坑

需要先 pip install UnityPy；Vector3f / Rectf 之类的结构体不可迭代（不能 tuple(v)），要按字段取 .x/.y/.z 或 .x/.y/.width/.height；MonoBehaviour 的字段要靠 typetree，读不到时只能拿脚本引用名。

---

## English

A BepInEx 5 plugin for **Casualties: Unknown 1.0.2** that lets thigh pouches (material pouch / leg pouch) be distributed freely across both thighs: **at most one per thigh, at most two in total**.

### Vanilla behaviour (detailed)

The game is a 2D side view and the character faces right by default. What looks "in front" is decided by layer order, not by position: the layer closest to the viewer is drawn on top.

**Two independent legs.** The left and right thighs are separate limbs: ThighF (right thigh, the near side, drawn above) and ThighB (left thigh, the far side, drawn below). Each has its own SpriteRenderer and sortingOrder, so the near thigh partially covers the far one.

**Hard-bound equipment.** The leg pouch is bound to ThighF and the material pouch to ThighB; the player cannot choose a side, and losing that thigh makes the item completely unwearable.

**Layering comes from an offset.** On wear, the item's transform is reset to the limb centre (position, scale and rotation zeroed). The only thing that matters is:

```
item.sortingOrder = limb.sortingOrder + ItemInfo.wearableVisualOffset
```

The leg pouch uses +5, so it is drawn above its (near) thigh and always appears on the side facing the player; the material pouch uses −5, so it is drawn below its (far) thigh and always appears on the far side, partially hidden by the body. Back to front: material pouch → far thigh → torso / near thigh → leg pouch.

**Slot uniqueness is global.** GetWearableBySlotID(wearSlotId) scans every limb, so one thigh and one thighback item at most; two material pouches are impossible in vanilla.

**Why the second pouch ends up between the legs.** A second material pouch has to sit on ThighF, but it keeps its −5 offset, so it is drawn behind the near thigh and in front of the far thigh. With identical 12×12 px sprites (1.5×1.5 world units, centred pivot) it reads as a pouch caught between the legs.

**Which of two material pouches is on top.** This plugin negates the offset when an item is worn on the mirrored thigh: the pouch on the near thigh becomes near-thigh + 5 (top layer, closest to the viewer, covering the thigh), while the pouch on the far thigh stays far-thigh − 5 (bottom layer, hidden behind the thigh). Without negating, the near-thigh pouch would be squeezed behind the near thigh and both would look like they sit behind the legs. Two leg pouches behave the same way.

### What this plugin does

1. Groups thigh and thighback together; the rule becomes "at most one group item per thigh, at most two in total, one on each thigh".
2. On wear, it walks [original thigh → mirrored thigh] and picks the first limb that is not dismembered and holds no group item yet, then performs the vanilla-style attach itself.
3. When both thighs are full it blocks the third item even if vanilla would have allowed it (different slot ids do not conflict in vanilla).
4. When an item is placed on the mirrored thigh, the sprite sorting offset is flipped (material pouch −5 → +5, leg pouch +5 → −5) so it no longer looks sandwiched between the legs.
5. The item tooltip gets a first line "Position: Left thigh / Right thigh", visible without expanding the tooltip with Shift.
6. It patches Body.GetWearable(string) with a fallback that scans all limbs, so an item worn on the mirrored thigh can still be dragged off (vanilla only looks at the hard-coded limb).

### Configuration

File: BepInEx/config/humanresistor.cu.boththighslots.cfg

| Key | Default | Meaning |
|---|---|---|
| General / Enabled | true | Master switch |
| General / VerboseLog | true | Log a line when an item is moved to the mirrored thigh / offset flipped |
| General / MirrorSortingOffset | true | Flip the sorting offset when the item sits on the mirrored thigh |
| General / GroupSlots | thigh,thighback | Wear slots treated as one "thigh pouch" group, comma separated |
| General / ShowLimbInTooltip | true | Show the worn limb at the top of the item tooltip |

### Capacity rules

| Situation | Result |
|---|---|
| Material pouch + material pouch | One per thigh, allowed |
| Leg pouch + leg pouch | One per thigh, allowed |
| Material pouch + leg pouch | One per thigh, allowed |
| Any third pouch | Rejected with the vanilla "already wearing" alert |
| One thigh dismembered | Only the remaining thigh is used |
| Both thighs dismembered | Neither material pouch nor leg pouch can be worn at all |

The plugin only validates the wear / unequip actions; saves that already contain stacked pouches are not cleaned up automatically — unequip the extras and wear them again.

### Install / build

1. Install BepInEx 5.4.23.5 x64 into the game root (you should see winhttp.dll and a BepInEx folder).
2. Put BothThighSlots.dll into BepInEx/plugins/.
3. Launch the game through Steam; close the game before replacing the DLL.

Build (requires the .NET SDK, verified with 8.0.400):

```
dotnet build -c Release
# output: bin/Release/netstandard2.1/BothThighSlots.dll
```

The target framework must be **netstandard2.1** (the game assemblies are 2.1; netstandard2.0 fails with CS1705). References: Assembly-CSharp.dll, UnityEngine.dll, UnityEngine.CoreModule.dll, UnityEngine.Physics2DModule.dll from the game's Managed folder, plus BepInEx.dll and 0Harmony.dll from BepInEx/core.

### Dev notes (English summary)

The full Chinese write-up above documents ten real pitfalls hit while building this plugin; items 1, 2, 3 and 7 are the important ones:

1. Vanilla enforces one item per **wear-slot id across the whole body** (GetWearableBySlotID scans every limb), so "one per thigh" cannot be achieved without taking over the check.
2. Only redirecting when the original thigh is occupied/dismembered misses two cases: the target thigh is free while the mirrored thigh holds the same slot id (vanilla then wrongly reports "already wearing"), and two different slot ids stacking on the same thigh (three items end up equipped).
3. The "sandwiched between the legs" look comes from ItemInfo.wearableVisualOffset (−5 for the material pouch, +5 for the leg pouch); it has to be negated when the item is worn on the mirrored side.
4. The sprites have no left/right variants (material pouch 12×12, leg pouch 10×12, centred pivot, 8 PPU); vanilla solves layering purely through the sorting offset, not through pixel offsets or mirroring.
5. A Harmony patch class that is never passed to PatchAll silently does nothing.
6. An unclosed TextMeshPro color tag paints the whole remaining tooltip text.
7. Body.GetWearable(itemid) only scans the hard-coded desired limb, and both the "can I unequip" check and DropWearable rely on it, so items worn on the mirrored thigh cannot be dragged off; a postfix that scans all limbs fixes it.
8. Unequipping requires a drag distance above 10 pixels; a shorter drag is treated as "click to open container".
9. The project must target netstandard2.1, and Rigidbody2D usage needs UnityEngine.Physics2DModule.
10. When reading the game assets with UnityPy, Vector3f / Rectf are not iterable — read their fields directly.

## License

[MIT](LICENSE) © 2026 humanresistor — free to modify and redistribute, including closed-source.
