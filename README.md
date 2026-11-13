# 新手套装（ONI-Starter）

《缺氧》Mod，兼容全部 DLC 与混合组合。

A quality-of-life mod for Oxygen Not Included. Compatible with all DLCs and mixed DLC combinations.

---

## 中文

### 设置入口

- 主菜单 → **Mods / Mod 列表** → 找到“新手套装” → 点击 **“设置”**
- 设置界面采用类似 DGSM 的面板样式，使用勾选框开关各项功能，不需要反复点击按钮切换。
- 不会在游戏内、暂停菜单、选项菜单或打印舱上增加设置入口。

### 7 个勾选项

默认全部关闭，首次安装保持原版行为。

| 勾选项 | 内容 |
|---|---|
| **食物保鲜** | 食物永不腐烂、始终 100% 新鲜度 |
| **简化复制人** | 仅标准复制人：固定 3 个特质、移除其他随机特质、压力反应只保留大哭、清空技能、只出现职业相关兴趣 |
| **自动打印** | 按打印舱“自动打印列表”的勾选自动打印补给包 |
| **打印舱选项刷新** | 在打印舱“选择蓝图”界面中，为复制人和补给包标题右侧显示刷新按钮 |
| **机械飞升** | 打印舱前可在标准复制人与仿生人之间转换，转换后永久清空技能点 |
| **技能点满级不增加** | 仅标准复制人：技能树点满后停止增长技能点 |
| **自动破碎流星** | 流星刚进入地图即被击碎，碎裂后掉落少量资源；灾星事件不受影响 |

### 各功能说明

#### 食物保鲜

- 开启后所有带 `Rottable` 的食物/物品始终为 100% 新鲜度。
- 状态机每秒读取到的腐烂值都会被补满。
- 关闭后恢复原版腐烂逻辑。

#### 简化复制人

仅作用于标准复制人（Minion）：

- **固定 3 个特质**：健步如飞（Twinkletoes）、健美者（StrongArm）、潜水员之肺（DiversLung）。
- 移除其他随机特质，包括负面特质、会禁用工作的特质、会直接授予技能的特质。
- 压力反应只保留无害的大哭（UglyCrier）。
- 只出现与职业相关的兴趣，属性按原版规则计算。
- 清空已学技能，但起始技能点保持原版。

**仿生复制人（BionicMinion）目前完全保持原版**：

- 不改特质、压力反应、技能、技能点、兴趣。
- 不改遇水漏电。
- 不改仿生升级/电池等相关逻辑。

#### 自动打印

- 选中打印舱后，**“自动打印列表”** 会显示在打印舱面板左侧的独立面板中，只列出补给包，不包含复制人。
- 有勾选且“自动打印”开关开启时，打印舱充满后自动打印匹配的补给包，然后继续充能。
- 没有勾选时不会自动打印。
- 关闭“自动打印”开关后，列表勾选仍会保留，但不会自动打印。
- 开启补给包时按原版概率生成 1 或 2 个补给包候选。

#### 打印舱选项刷新

- 点击打印舱打开“选择蓝图”界面时，每个复制人和补给包的标题右侧会显示“刷新”按钮。
- 点击某个选项的“刷新”只会重新生成该选项，不影响其他候选。
- 关闭该勾选项后恢复原版界面，不显示刷新按钮。

#### 机械飞升

- 标准复制人属性面板下方显示“机械飞升”按钮；点击后会先自动走到最近的打印舱前，再改造成仿生人。
- 仿生人显示“秽土转生”按钮；点击后会自动走到打印舱前，再恢复为标准复制人。
- 死亡的复制人或仿生人也可以使用对应按钮：复制人会把遗体搬运到打印舱，送达后自动转换。
- 两种转换都会**永久清空技能点与技能经验**。
- 转换后的标准复制人会遵守“简化复制人”设置；转换后的仿生人保持原版规则。
- 需要当前存档启用《仿生助推器》DLC。
- 转换会重新生成目标模型的属性与特质，并尽量保留名字、外观、个人优先级和日程。

#### 技能点满级不增加

仅作用于标准复制人：

- 当前模型、当前 DLC 下所有可用技能全部掌握后，停止继续增长技能点/技能经验。
- 属性经验仍然正常增长。
- 对现有标准复制人立即生效。
- 关闭后技能点按原版继续增长。

#### 自动破碎流星

- 常规流星刚进入当前地图边界时会被自动击碎，不再落到地表。
- 击碎效果与「流星爆破炮」拦截流星一致：掉落约 25% 质量的碎块，并保留特殊掉落（牛肉、太空树种子等）。
- 流星在进入地图前不会触发；开启设置时已经在地图内飞行的流星会立即被击碎。
- 太空中架设的太阳能面板、地表建筑和复制人都在击碎点下方，不会受到撞击伤害。
- 只作用于常规流星；DLC4 的**灾星事件**（大型撞击小行星）、地热喷泉的小流星、以及会送来喷气牛的**牛流星**不受影响。
- 关闭后恢复原版流星撞击逻辑。

### 配置保存

设置：

`Application.persistentDataPath/ONI-Starter/settings.json`

打印列表：

`Application.persistentDataPath/ONI-Starter/print_list.json`

同一台机器上的所有存档共用这些配置。

### 安装

1. 将 `ONI-Starter` 文件夹放到：
   `~/Library/Application Support/unity.Klei.Oxygen Not Included/mods/Local/`
   或 Steam 创意工坊的本地 Mod 目录。
2. 启动游戏，在 Mod 列表中启用“新手套装”。
3. 进入主菜单 Mod 列表，点击“新手套装”的 **设置**，勾选需要的功能。
4. 如需自动打印，选中打印舱，在“自动打印列表”中勾选补给包。

### 构建

```bash
./build.sh
```

构建后会部署到：

`~/Library/Application Support/unity.Klei.Oxygen Not Included/mods/Local/ONI-Starter`

### 说明

- 本项目已通过 `dotnet build -c Release` 编译验证。
- 复制人规则作用于新生成、打印以及通过低温舱/剧情等获得的标准复制人；已经存在的旧复制人不会被强制洗点。
- 仿生复制人当前不参与任何复制人修改。

---

## English

### Where to find the settings

- Main menu → **Mods** → find **ONI-Starter** → click **Settings**.
- The settings screen uses a DGSM-style panel with checkboxes. There is no need
  to click buttons repeatedly to cycle values.
- No additional entry points are added in game, in the pause menu, in the
  options menu, or on the printing pod.

### Seven checkboxes

Everything is disabled by default, so a fresh install behaves like vanilla.

| Checkbox | Description |
|---|---|
| **Food freshness** | Food never spoils and always stays at 100% freshness |
| **Simplify duplicants** | Standard duplicants only: three fixed traits, no other random traits, only the Ugly Crier stress reaction, no learned skills, job-related aptitudes only |
| **Auto print** | Automatically print care packages according to the printing pod's auto-print list |
| **Print option refresh** | Show a refresh button next to each duplicant and care-package title in the printing pod's "Choose Blueprint" screen |
| **Mechanical ascension** | Convert standard duplicants to bionics and back at the printing pod; both conversions permanently clear skill points |
| **Skill point cap** | Standard duplicants only: stop gaining skill points after the skill tree is fully mastered |
| **Auto shatter meteors** | Meteors are shattered as soon as they enter the map, dropping a small amount of resources; the calamity event is unaffected |

### Feature details

#### Food freshness

- While enabled, every food/item with `Rottable` stays at 100% freshness.
- The rot value read by the state machine is refilled every second.
- Disabling it restores vanilla spoilage.

#### Simplify duplicants

Applies to standard duplicants (Minion) only:

- **Three fixed traits**: Twinkletoes, StrongArm (Buff), and DiversLung.
- Other random traits are removed, including negative traits, traits that disable
  chore groups, and traits that grant skills.
- The stress reaction is always the harmless Ugly Crier.
- Only one job-related aptitude is generated; attributes follow vanilla rules.
- Learned skills are cleared, but starting skill points remain vanilla.

**Bionic duplicants (BionicMinion) are left completely vanilla**:

- Their traits, stress reaction, skills, skill points, and aptitudes are not changed.
- Their water damage behaviour is not changed.
- Bionic upgrades and battery logic are not changed.

#### Auto print

- Selecting the printing pod shows an **Auto-print list** in a separate panel
  to the left of the printing pod panel; the list contains care packages only,
  never duplicants.
- When at least one item is checked and the Auto print setting is enabled, the
  pod prints a matching care package as soon as it is ready, then recharges.
- Nothing is auto-printed when no item is checked.
- Disabling Auto print keeps the checked list but stops automatic printing.
- When care packages are enabled, vanilla probabilities produce one or two
  care-package candidates.

#### Print option refresh

- When the printing pod's "Choose Blueprint" screen opens, a **Refresh** button
  appears to the right of each duplicant and care-package title.
- Refreshing one option rerolls only that option; the other candidates are left
  untouched.
- Disabling this checkbox restores the vanilla screen without refresh buttons.

#### Mechanical ascension

- Standard duplicants show a **Mechanical Ascension** button in the user menu;
  clicking it makes them walk to the nearest printing pod and then converts
  them into bionics.
- Bionics show a **Reincarnation** button that walks them to the pod and turns
  them back into standard duplicants.
- Dead duplicants can use the same button: another duplicant hauls the body to
  the pod, and the conversion happens on delivery.
- Both conversions **permanently clear all skill points and skill experience**.
- A standard duplicant created by a conversion obeys the Simplify duplicants
  setting; bionics keep the vanilla rules.
- The current save must have The Bionic Booster Pack enabled.
- The target model's attributes and traits are regenerated. Name, appearance,
  personal priorities, and schedule are preserved where possible.

#### Skill point cap

Applies to standard duplicants only:

- Once every available skill for the current model and active DLCs is mastered,
  skill points and skill experience stop accumulating.
- Attribute experience still accumulates normally.
- Takes effect immediately for existing standard duplicants.
- Disabling it restores vanilla skill-point progression.

#### Auto shatter meteors

- Regular meteors are shattered as soon as they enter the current map, so they
  never reach the surface.
- The shatter behaves like a meteor-blaster interception: about 25% of the mass
  drops as debris, and special loot (meat, space tree seeds, etc.) is preserved.
- Nothing happens while a meteor is still outside the map; meteors already
  flying inside the map are shattered immediately when the setting is enabled.
- Solar panels built in space, surface buildings, and duplicants are all below
  the shatter point and take no impact damage.
- Only regular meteors are affected. The DLC4 **calamity event** (the large
  impactor asteroid), geothermal-vent mini comets, and **Moo meteors** (which
  deliver a live Gassy Moo) keep their vanilla behaviour.
- Disabling it restores vanilla meteor impacts.

### Configuration files

Settings:

`Application.persistentDataPath/ONI-Starter/settings.json`

Print list:

`Application.persistentDataPath/ONI-Starter/print_list.json`

All saves on the same machine share these files.

### Installation

1. Place the `ONI-Starter` folder in:
   `~/Library/Application Support/unity.Klei.Oxygen Not Included/mods/Local/`
   or in the local mod folder used by the Steam Workshop.
2. Launch the game and enable **ONI-Starter** in the Mods list.
3. Open the main-menu Mods list, click **Settings** on ONI-Starter, and check the
   features you want.
4. For auto-printing, select the printing pod and check care packages in the
   **Auto-print list**.

### Build

```bash
./build.sh
```

The build deploys to:

`~/Library/Application Support/unity.Klei.Oxygen Not Included/mods/Local/ONI-Starter`

### Notes

- The project is verified with `dotnet build -c Release`.
- Duplicant rules apply to newly generated duplicants, printed duplicants, and
  standard duplicants obtained through cryo pods/story content. Existing old
  duplicants are not forcibly re-specced.
- Bionic duplicants are currently excluded from all duplicant modifications.
