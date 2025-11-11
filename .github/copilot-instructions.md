## 快速定位（用于 AI 代码助手）

- 项目类型：Unity 包（Unity 2023.1），位于包根目录（含 `package.json`）。
- 主要命名空间：`LZ.WarGameMap.Runtime`（运行时）、`LZ.WarGameMap.MapEditor`（编辑器）。
- 主要 asmdef：`Unity.LZWarGameMap`（Runtime）和 `Unity.LZWarGameMap.MapEditor`（Editor），编辑器代码只在 Editor 平台生效。

## 项目概要
- 简介：这是一个用于 SLG 游戏内地图构建的 Unity 包，提供大规模 Terrain 生成、六边形网格地图、河流道路生成、植被建筑装饰、战争迷雾与寻路等功能。编辑器通过自定义窗口和工具栏集成在 Unity 编辑器中，用户通过操作编辑器来生成地图；

- 预期效果：参考《欧陆风云4》《文明6》《文明7》《钢铁雄心4》《十字军之王3》《全面战争系列》的游戏地图构建与玩法需求；（在完成功能或提供思路时，可以考虑以上游戏的实现方式）

## 代码结构要点

- 编辑器逻辑集中在 `Editor/`：例如 `Editor/RootMapEditor.cs`（菜单与窗口初始化入口）、`Editor/Base/BaseMapEditor.cs`（编辑器窗口基类）。
- 运行时代码在 `Runtime/`：例如 `Runtime/WGMapManager.cs`（运行时入口点、MonoBehaviour）。
- 资源与运行时数据使用 ScriptableObject 存储在 `MapStoreEnum` 指定的路径，编辑器通过 `AssetDatabase` 创建/查找这些 SO（参见 `BaseMapEditor.FindOrCreateSO`）。
- 项目依赖第三方插件（例如 `OdinInspector`、`SplineMesh`），编辑器窗口大量使用 `Sirenix.OdinInspector` 特性。

## 开发与调试工作流（具体、可执行）

1. 在 Unity 中添加包：Unity 菜单 -> Package Manager -> Add from disk -> 选择此包文件夹（根含 `package.json`）。
2. 打开编辑器窗口：Unity 顶部菜单 `GameMap` -> `InitMapEditor`（首次会创建 MapWindow 的 ScriptableObject 资产）-> `OpenMapEditor` 打开主编辑器。
3. 编辑器热重载：修改 `Editor/` 下代码后，切回 Unity 等待编译；若改动 asmdef，Unity 会重新导入对应 assembly。
4. 运行时逻辑调试：将 `WGMapManager` 挂到场景 GameObject 上并在 Play 模式下调试。（TODO : 目前是人工在go上挂接进行的调试，效率较低，后续变化自动化）

## 重要约定与示例（AI 需要遵守）

- 新的编辑器窗口应继承 `BaseMapEditor` 并根据需要实现编辑器窗口生命周期回调（Enable/Disable/OnSceneGUI）。将窗口作为 ScriptableObject 存放在 `MapWindowPath` 下，RootMapEditor 通过 `CreateWindowObj<T>` 创建并注册。
- 若要在主菜单中添加新的窗口：
  - 在 `RootMapEditor.InitMapEditor()` 中加入 CreateWindowObj 调用（见已有 `MapSetEditor`/`TerrainEditor` 的写法）。
  - 在 `BuildMenuTree()` 中用 `AddWinEditorAsMenuItem` 将 asset 加到菜单树。
- 编辑器与运行时的边界：
  - 不要在 `Runtime/` 中引用 Editor-only API（如 `UnityEditor.*` 或 `Sirenix.OdinInspector.Editor`）；把编辑器代码放 `Editor/` 或依赖 `Unity.LZWarGameMap.MapEditor` asmdef。
  - 反之，Editor 代码可以安全引用运行时代码命名空间。

## 常见改动模式（给补全与重构的具体提示）

- 添加新 SO（设置/窗口）时，优先在 `MapStoreEnum` 定义的位置创建；参照 `BaseMapEditor.FindOrCreateSO` 的实现逻辑以保证一致的路径与命名。
- 批量创建菜单/窗口请复用 `RootMapEditor.CreateWindowObj<T>` 与 `GetFileNames` 的方式，而非直接操作 AssetDatabase 路径字符串。
- 修改跨文件单例或 Editor 更新回调时，检查 `EditorSceneManager.GetInstance()`、`GizmosCtrl` 的注册/注销，避免泄漏（见 `RootMapEditor.OnEnable/OnDestroy`）。

## 依赖与集成点

- 插件：`OdinInspector`（命名空间 `Sirenix.OdinInspector` / `Sirenix.OdinInspector.Editor`）和 `SplineMesh` 在 `Plugins/` 下。AI 助手在生成引用时应使用这些命名空间并在注释中说明依赖。
- asmdef：修改或新增 asmdef 时需保证 `rootNamespace` 与引用 GUIDs 一致（示例：`Unity.LZWarGameMap.MapEditor` 只包含 Editor 平台）。

## 提示给 AI：优先级 & 禁止操作

- 优先级：修复 Editor 崩溃 > 修复编译错误 > 添加 Editor 功能 > 优化 Runtime 行为。
- 禁止操作：不要把 Editor API 移到 Runtime assembly；不要删除或绕过 `MapStoreEnum` 的路径管理。

## 参考文件（快速跳转）

- `README.md`（包说明与使用）
- `package.json`（包元数据，Unity 通过此识别 package）
- `Editor/RootMapEditor.cs`, `Editor/Base/BaseMapEditor.cs`（编辑器主入口与基类）
- `Runtime/WGMapManager.cs`（运行时入口）
- `Runtime/Unity.WarGameMap.asmdef`, `Editor/Unity.WarGameMap.Editor.asmdef`（assembly 划分）

---
