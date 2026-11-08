# 新手套装（ONI-Starter）

《缺氧》Mod，兼容全部 DLC 与混合组合。

A quality-of-life mod for Oxygen Not Included. Compatible with all DLCs and mixed DLC combinations.

---

## 中文

### 设置入口

- 主菜单 → **Mods / Mod 列表** → 找到“新手套装” → 点击 **“设置”**
- 设置界面采用类似 DGSM 的面板样式，使用勾选框开关各项功能，不需要反复点击按钮切换。
- 不会在游戏内、暂停菜单、选项菜单或打印舱上增加设置入口。

### 4 个勾选项

默认全部关闭，首次安装保持原版行为。

| 勾选项 | 内容 |
|---|---|
| **食物保鲜** | 食物永不腐烂、始终 100% 新鲜度 |
| **简化复制人** | 仅标准复制人：固定 3 个特质、移除其他随机特质、压力反应只保留大哭、清空技能、只出现职业相关兴趣 |
| **自动打印** | 按打印舱“自动打印列表”的勾选自动打印补给包 |
| **技能点满级不增加** | 仅标准复制人：技能树点满后停止增长技能点 |

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

- 打印舱右侧仍有 **“自动打印列表”**，只列出补给包，不包含复制人。
- 有勾选且“自动打印”开关开启时，打印舱充满后自动打印匹配的补给包，然后继续充能。
- 没有勾选时不会自动打印。
- 关闭“自动打印”开关后，列表勾选仍会保留，但不会自动打印。
- 开启补给包时按原版概率生成 1 或 2 个补给包候选。

#### 技能点满级不增加

仅作用于标准复制人：

- 当前模型、当前 DLC 下所有可用技能全部掌握后，停止继续增长技能点/技能经验。
- 属性经验仍然正常增长。
- 对现有标准复制人立即生效。
- 关闭后技能点按原版继续增长。

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

### Four checkboxes

Everything is disabled by default, so a fresh install behaves like vanilla.

| Checkbox | Description |
|---|---|
| **Food freshness** | Food never spoils and always stays at 100% freshness |
| **Simplify duplicants** | Standard duplicants only: three fixed traits, no other random traits, only the Ugly Crier stress reaction, no learned skills, job-related aptitudes only |
| **Auto print** | Automatically print care packages according to the printing pod's auto-print list |
| **Skill point cap** | Standard duplicants only: stop gaining skill points after the skill tree is fully mastered |

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

- The printing pod still shows an **Auto-print list** on its side screen, and the
  list contains care packages only, never duplicants.
- When at least one item is checked and the Auto print setting is enabled, the
  pod prints a matching care package as soon as it is ready, then recharges.
- Nothing is auto-printed when no item is checked.
- Disabling Auto print keeps the checked list but stops automatic printing.
- When care packages are enabled, vanilla probabilities produce one or two
  care-package candidates.

#### Skill point cap

Applies to standard duplicants only:

- Once every available skill for the current model and active DLCs is mastered,
  skill points and skill experience stop accumulating.
- Attribute experience still accumulates normally.
- Takes effect immediately for existing standard duplicants.
- Disabling it restores vanilla skill-point progression.

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
