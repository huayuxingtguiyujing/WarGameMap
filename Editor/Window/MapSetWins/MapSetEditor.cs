using LZ.WarGameMap.Runtime;
using LZ.WarGameMap.Runtime.Enums;
using LZ.WarGameMap.Runtime.Model;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor.VersionControl;
using UnityEngine;
using System;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine.AddressableAssets;
using Object = UnityEngine.Object;

namespace LZ.WarGameMap.MapEditor
{

    public class MapSetEditor : BaseMapEditor {

        public override string EditorName => MapEditorEnum.MapSetEditor;

        #region behaviors

        public override void Enable()
        {
            base.Enable();
            InitializeRuntimeHeightAssets();
        }

        #endregion


        [FoldoutGroup("配置scene")]
        [LabelText("地图Runtime配置")]
        public MapRuntimeSetting mapSet;

        [FoldoutGroup("配置scene")]
        [LabelText("地形配置")]
        public TerrainSettingSO terSet;     // 自觉不要在外部对这个东西进行修改

        [FoldoutGroup("配置scene")]
        [LabelText("地图Hex配置")]
        public HexSettingSO hexSet;

        [FoldoutGroup("配置scene")]
        [LabelText("格子地形数据")]
        public GridTerrainSO gridTerrainSO;

        [FoldoutGroup("配置scene")]
        [LabelText("区域数据")]
        public CountrySO countrySO;

        protected override void InitEditor() {
            //if (terSet == null) {
            //    string terrainSettingPath = MapStoreEnum.WarGameMapSettingPath + ;
            //    terSet = AssetDatabase.LoadAssetAtPath<TerrainSettingSO>(terrainSettingPath);
            //    if (terSet == null) {
            //        terSet = CreateInstance<TerrainSettingSO>();
            //        AssetDatabase.CreateAsset(terSet, terrainSettingPath);
            //        Debug.Log($"successfully create Terrain Setting, path : {terrainSettingPath}");
            //    }
            //}
            //if (hexSet == null) {
            //    string hexSettingPath = MapStoreEnum.WarGameMapSettingPath + ;
            //    hexSet = AssetDatabase.LoadAssetAtPath<HexSettingSO>(hexSettingPath);
            //    if (hexSet == null) {       // create it !
            //        hexSet = CreateInstance<HexSettingSO>();
            //        AssetDatabase.CreateAsset(hexSet, hexSettingPath);
            //        Debug.Log($"successfully create Hex Setting, path : {hexSettingPath}");
            //    }
            //}

            //FindOrCreateSO<MapRuntimeSetting>(ref mapSet, MapStoreEnum.WarGameMapSettingPath, "TerrainRuntimeSet_Default.asset");
            //FindOrCreateSO<TerrainSettingSO>(ref terSet, MapStoreEnum.WarGameMapSettingPath, "TerrainSetting_Default.asset");
            //FindOrCreateSO<HexSettingSO>(ref hexSet, MapStoreEnum.WarGameMapSettingPath, "HexSetting_Default.asset");
            mapSet = EditorSceneManager.MapSet;
            terSet = EditorSceneManager.TerSet;
            hexSet = EditorSceneManager.HexSet;
            gridTerrainSO = EditorSceneManager.GridTerrainSO;
            countrySO = EditorSceneManager.CountrySO;

            EditorSceneManager.GetInstance().LoadMapRenderer(mainMaterial, terrainLandformMat, riverMaterial);

            base.InitEditor();
        }


        #region Terrain Scene/数据

        [FoldoutGroup("Editor 场景配置")]
        [LabelText("Terrain生成方式")]
        public TerMeshGenMethod GenMethod;

        [FoldoutGroup("Editor 场景配置")]
        [LabelText("Terrain Material")]
        public List<HeightDataModel> heightDataModels;

        // TODO： 材质应该动态加载，后续要改
        [FoldoutGroup("Editor 场景配置")]
        [LabelText("地图主材质")]
        public Material mainMaterial;

        [FoldoutGroup("Editor 场景配置")]
        [LabelText("地貌材质")]
        public Material terrainLandformMat;

        [FoldoutGroup("Editor 场景配置")]
        [LabelText("河流材质")]
        public Material riverMaterial;

        [FoldoutGroup("Editor 场景配置")]
        [LabelText("ter材质-用于编辑")]
        [Tooltip("地形资产使用的材质，编辑时态")]
        public Material terMaterial;

        [FoldoutGroup("Editor 场景配置")]
        [LabelText("hex材质-用于编辑")]
        [Tooltip("hexmap 资产使用的材质，编辑时态")]
        public Material hexMaterial;

        [FoldoutGroup("Editor 场景配置")]
        [LabelText("贝塞尔节点Prefab")]
        [Tooltip("用于提供标记 prefab，可供需要的地方调用")]
        public GameObject signObj;

        [FoldoutGroup("Editor 场景配置")]
        [LabelText("Hex涂刷CS")]
        [Tooltip("用于快速涂刷 hex grid，请见")]
        public ComputeShader paintRTShader;


        [FoldoutGroup("Editor 场景配置")]
        [LabelText("Terrain Mesh 数据")]    // serialized file data
        public TerrainMeshDataBinder terAssetBinder;

        [FoldoutGroup("Editor 场景配置")]
        [LabelText("Terrain Binder 文件夹路径")]
        public string terBinderPath = MapStoreEnum.WarGameMapEditObjPath;

        [FoldoutGroup("Editor 场景配置")]
        [LabelText("Terrain Mesh 文件夹路径")]
        public string clsMeshDataPath = MapStoreEnum.TerrainMeshSerializedPath;

        [FoldoutGroup("Editor 场景配置")]
        [Button("一键导入所有 Terrain Mesh 数据", ButtonSizes.Medium)]
        private void ImportClusterMeshDatas() {
            FindOrCreateSO(ref terAssetBinder, terBinderPath, "TerrainMeshDataBinder.asset");

            // read mesh data from the path
            string[] filePaths = Directory.GetFiles(clsMeshDataPath, "*", SearchOption.AllDirectories)
                .Where(path => !path.EndsWith(".meta")).ToArray();
            terAssetBinder.LoadAsset(filePaths);
        }

        [FoldoutGroup("Editor 场景配置")]
        [Button("初始化 地形场景", ButtonSizes.Medium)]   // so that you can view the terrain cluster in scene
        private void InitSceneManagerTer() {

            System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
            stopwatch.Start();

            EditorSceneManager.GetInstance().LoadTerScene(5, terAssetBinder.MeshBinderList, heightDataModels, terMaterial);

            EditorSceneManager.GetInstance().LoadHexScene(hexMaterial);

            EditorSceneManager.GetInstance().LoadMapRenderer(mainMaterial, terrainLandformMat, riverMaterial);

            EditorSceneManager.GetInstance().SetEditorAssets(terMaterial, signObj, paintRTShader, hexMaterial);

            stopwatch.Stop();
            Debug.Log($"init scene manager ter scene successfully! cost {stopwatch.ElapsedMilliseconds} ms");
        }

        [FoldoutGroup("Editor 场景配置")]
        [Button("清空 地形场景", ButtonSizes.Medium)]
        private void ClearTerScene() {
            EditorSceneManager.GetInstance().ClearTerScene();
        }

        [FoldoutGroup("Editor 场景配置")]
        [Button("初始化 Hex 场景", ButtonSizes.Medium)]
        private void InitSceneManagerHex() {

        }

        [FoldoutGroup("Editor 场景配置")]
        [Button("清空 Hex 场景", ButtonSizes.Medium)]
        private void ClearHexScene() {
            EditorSceneManager.GetInstance().ClearHexScene();
        }

        #endregion

        #region 运行时资产配置

        internal static void SetRuntimeHexMapData(HexMapSO data) {
            MapRuntimeAssetsBuilder.SetAsset(MapRuntimeAssetRole.HexMapData, data);
        }

        [ShowInInspector, ReadOnly, FoldoutGroup("运行时资产配置")]
        [LabelText("默认资源清单")]
        [InfoBox("配置资产后，要点击按钮并更新 Addressables")]
        private MapRuntimeAssetsSO RuntimeManifest => MapRuntimeAssetsBuilder.GetManifest();

        [ShowInInspector, AssetsOnly, FoldoutGroup("运行时资产配置"), LabelText("Hex地图数据")]
        private HexMapSO RuntimeHexMapData {
            get => MapRuntimeAssetsBuilder.GetAsset<HexMapSO>(MapRuntimeAssetRole.HexMapData);
            set => SetRuntimeHexMapData(value);
        }

        [ShowInInspector, AssetsOnly, FoldoutGroup("运行时资产配置"), LabelText("地形配置")]
        private TerrainSettingSO RuntimeTerrainSetting
        {
            get => MapRuntimeAssetsBuilder.GetAsset<TerrainSettingSO>(MapRuntimeAssetRole.TerrainSetting);
            set => MapRuntimeAssetsBuilder.SetAsset(MapRuntimeAssetRole.TerrainSetting, value);
        }

        [ShowInInspector, AssetsOnly, FoldoutGroup("运行时资产配置"), LabelText("Runtime配置")]
        private MapRuntimeSetting RuntimeMapSetting
        {
            get => MapRuntimeAssetsBuilder.GetAsset<MapRuntimeSetting>(MapRuntimeAssetRole.RuntimeSetting);
            set => MapRuntimeAssetsBuilder.SetAsset(MapRuntimeAssetRole.RuntimeSetting, value);
        }

        [ShowInInspector, AssetsOnly, FoldoutGroup("运行时资产配置"), LabelText("Hex配置")]
        private HexSettingSO RuntimeHexSetting
        {
            get => MapRuntimeAssetsBuilder.GetAsset<HexSettingSO>(MapRuntimeAssetRole.HexSetting);
            set => MapRuntimeAssetsBuilder.SetAsset(MapRuntimeAssetRole.HexSetting, value);
        }

        [ShowInInspector, AssetsOnly, FoldoutGroup("运行时资产配置"), LabelText("格子地貌数据")]
        private GridTerrainSO RuntimeGridTerrain
        {
            get => MapRuntimeAssetsBuilder.GetAsset<GridTerrainSO>(MapRuntimeAssetRole.GridTerrain);
            set => MapRuntimeAssetsBuilder.SetAsset(MapRuntimeAssetRole.GridTerrain, value);
        }

        [ShowInInspector, AssetsOnly, FoldoutGroup("运行时资产配置"), LabelText("区域数据")]
        private CountrySO RuntimeCountry
        {
            get => MapRuntimeAssetsBuilder.GetAsset<CountrySO>(MapRuntimeAssetRole.Country);
            set => MapRuntimeAssetsBuilder.SetAsset(MapRuntimeAssetRole.Country, value);
        }

        [ShowInInspector, AssetsOnly, FoldoutGroup("运行时资产配置"), LabelText("地貌材质")]
        private Material RuntimeLandformMaterial
        {
            get => MapRuntimeAssetsBuilder.GetAsset<Material>(MapRuntimeAssetRole.LandformMaterial);
            set => MapRuntimeAssetsBuilder.SetAsset(MapRuntimeAssetRole.LandformMaterial, value);
        }

        [ShowInInspector, AssetsOnly, FoldoutGroup("运行时资产配置"), LabelText("地貌颜色纹理数组")]
        private Texture2DArray RuntimeAlbedoArray
        {
            get => MapRuntimeAssetsBuilder.GetAsset<Texture2DArray>(MapRuntimeAssetRole.AlbedoArray);
            set => MapRuntimeAssetsBuilder.SetAsset(MapRuntimeAssetRole.AlbedoArray, value);
        }

        [ShowInInspector, AssetsOnly, FoldoutGroup("运行时资产配置"), LabelText("地貌法线纹理数组")]
        private Texture2DArray RuntimeNormalArray
        {
            get => MapRuntimeAssetsBuilder.GetAsset<Texture2DArray>(MapRuntimeAssetRole.NormalArray);
            set => MapRuntimeAssetsBuilder.SetAsset(MapRuntimeAssetRole.NormalArray, value);
        }

        [ShowInInspector, AssetsOnly, FoldoutGroup("运行时资产配置"), LabelText("区域纹理")]
        private Texture2D RuntimeRegionTexture
        {
            get => MapRuntimeAssetsBuilder.GetAsset<Texture2D>(MapRuntimeAssetRole.RegionTexture);
            set => MapRuntimeAssetsBuilder.SetAsset(MapRuntimeAssetRole.RegionTexture, value);
        }

        [ShowInInspector, AssetsOnly, FoldoutGroup("运行时资产配置"), LabelText("区域SDF计算着色器")]
        private ComputeShader RuntimeRegionSdfShader
        {
            get => MapRuntimeAssetsBuilder.GetAsset<ComputeShader>(MapRuntimeAssetRole.RegionSDFShader);
            set => MapRuntimeAssetsBuilder.SetAsset(MapRuntimeAssetRole.RegionSDFShader, value);
        }

        // 高度列表仅作界面缓存，实际配置保存在清单中。
        [NonSerialized, ShowInInspector, AssetsOnly, FoldoutGroup("运行时资产配置", 10)]
        [LabelText("高度数据")]
        [OnValueChanged(nameof(SaveRuntimeHeightAssets), IncludeChildren = true)]
        [OnCollectionChanged(null, nameof(SaveRuntimeHeightAssets))]
        private List<HeightDataModel> runtimeHeightAssets = new List<HeightDataModel>();

        // 切换到本编辑器时，在绘制前补齐高度数据并读取清单。
        private void InitializeRuntimeHeightAssets()
        {
            MapRuntimeAssetsBuilder.FillDefaultHeightAssets();
            ReloadRuntimeHeightAssets();
        }

        private void ReloadRuntimeHeightAssets()
        {
            runtimeHeightAssets = MapRuntimeAssetsBuilder.GetHeightAssets();
        }

        // 保存高度列表修改，不更新 Addressables 登记。
        private void SaveRuntimeHeightAssets()
        {
            MapRuntimeAssetsBuilder.SetHeightAssets(runtimeHeightAssets);
        }

        [ShowInInspector, FoldoutGroup("运行时资产配置"), LabelText("地块目录")]
        private string RuntimeTerrainDirectory
        {
            get => MapRuntimeAssetsBuilder.GetManifest().terrainDirectory;
            set => MapRuntimeAssetsBuilder.SetTerrainDirectory(value);
        }

        [ShowInInspector, ReadOnly, FoldoutGroup("运行时资产配置"), LabelText("上次登记地块数量")]
        private int RuntimeClusterCount => MapRuntimeAssetsBuilder.GetManifest().clusters.Count;

        [FoldoutGroup("运行时资产配置")]
        [Button("更新Addressables 资产", ButtonSizes.Medium)]
        private void UpdateRuntimeAddressables()
        {
            MapRuntimeAssetsBuilder.UpdateAddressables();
        }

        // 负责本窗口的清单准备与资源登记，不绘制 UI 或构建 AB。
        private static class MapRuntimeAssetsBuilder
        {
            private sealed class MapAssetField
            {
                public MapRuntimeAssetRole Role;
                public string Label, Path;
                public Type Type;
                public MapAssetField(MapRuntimeAssetRole role, string label, Type type, string path = null)
                {
                    Role = role;
                    Label = label;
                    Type = type;
                    Path = path;
                }
            }

            private static MapAssetField[] fields;

            private static MapAssetField[] Fields
            {
                get
                {
                    if (fields == null)
                    {
                        fields = CreateFields();
                    }
                    return fields;
                }
            }

            private static MapAssetField[] CreateFields()
            {
                string terrainPath = GetMapSettingPath<TerrainSettingSO>();
                string runtimePath = GetMapSettingPath<MapRuntimeSetting>();
                string hexPath = GetMapSettingPath<HexSettingSO>();
                string gridName = GridTerrainSO.GetDefaultAssetName();
                string gridPath = MapStoreEnum.GamePlayGridTerrainDataPath + "/" + gridName;
                string countryName = CountrySO.GetDefaultAssetName();
                string countryPath = MapStoreEnum.GamePlayCountryDataPath + "/" + countryName;
                const string landformMaterialPath = "Assets/Shader/Materials/CK3TerrainHeightMat.mat";
                string regionTexturePath = MapStoreEnum.GamePlayCountryTexDataPath + "/regionTex_256x256_59881319.png";
                const string regionSdfShaderPath = "Packages/com.lz-infrastructure.wargamemap/Assets/Shader/Hexmap/SDFCompute.compute";

                return new[]
                {
                    new MapAssetField(MapRuntimeAssetRole.TerrainSetting, "地形配置", typeof(TerrainSettingSO), terrainPath),
                    new MapAssetField(MapRuntimeAssetRole.RuntimeSetting, "Runtime配置", typeof(MapRuntimeSetting), runtimePath),
                    new MapAssetField(MapRuntimeAssetRole.HexSetting, "Hex配置", typeof(HexSettingSO), hexPath),
                    new MapAssetField(MapRuntimeAssetRole.GridTerrain, "格子地貌数据", typeof(GridTerrainSO), gridPath),
                    new MapAssetField(MapRuntimeAssetRole.HexMapData, "Hex地图数据", typeof(HexMapSO), MapStoreEnum.HexMapDataPath),
                    new MapAssetField(MapRuntimeAssetRole.Country, "区域数据", typeof(CountrySO), countryPath),
                    new MapAssetField(MapRuntimeAssetRole.LandformMaterial, "地貌材质", typeof(Material), landformMaterialPath),
                    new MapAssetField(MapRuntimeAssetRole.AlbedoArray, "地貌颜色纹理数组", typeof(Texture2DArray)),
                    new MapAssetField(MapRuntimeAssetRole.NormalArray, "地貌法线纹理数组", typeof(Texture2DArray)),
                    new MapAssetField(MapRuntimeAssetRole.RegionTexture, "区域纹理", typeof(Texture2D), regionTexturePath),
                    new MapAssetField(MapRuntimeAssetRole.RegionSDFShader, "区域SDF计算着色器", typeof(ComputeShader), regionSdfShaderPath)
                };
            }

            private static string GetMapSettingPath<T>() where T : MapSettingSO
            {
                // 读取现有实例名称接口后销毁临时对象，不创建资产。
                var setting = ScriptableObject.CreateInstance<T>();
                try
                {
                    string assetName = setting.MapSettingName;
                    return MapStoreEnum.WarGameMapSettingPath + "/" + assetName;
                }
                finally
                {
                    Object.DestroyImmediate(setting);
                }
            }

            private static MapRuntimeAssetsSO manifest;

            // 取得默认清单，仅在首次创建时填入默认资源。
            public static MapRuntimeAssetsSO GetManifest()
            {
                if (manifest != null) return manifest;
                manifest = AssetDatabase.LoadAssetAtPath<MapRuntimeAssetsSO>(MapStoreEnum.RuntimeManifestPath);
                if (manifest != null) return manifest;
                bool hasSettingFolder = AssetDatabase.IsValidFolder(MapStoreEnum.WarGameMapSettingPath);
                if (!hasSettingFolder)
                {
                    throw new InvalidOperationException("地图配置目录不存在");
                }
                bool manifestPathAvailable = AssetDatabase.LoadMainAssetAtPath(MapStoreEnum.RuntimeManifestPath) == null;
                if (!manifestPathAvailable)
                {
                    throw new InvalidOperationException("清单路径已被其他类型资产占用");
                }
                manifest = ScriptableObject.CreateInstance<MapRuntimeAssetsSO>();
                foreach (var field in Fields)
                {
                    Object asset = null;
                    if (field.Path != null)
                    {
                        asset = AssetDatabase.LoadAssetAtPath(field.Path, field.Type);
                        if (asset == null) Debug.LogError("[WarGameMap] 默认资源为空，请手动拖拽：" + field.Label + " / " + field.Path);
                    }
                    manifest.assets.Add(new MapRuntimeAssetEntry { role = field.Role, asset = Reference(asset) });
                }
                AssetDatabase.CreateAsset(manifest, MapStoreEnum.RuntimeManifestPath);
                AssetDatabase.SaveAssetIfDirty(manifest);
                return manifest;
            }

            private static Object Resolve(AssetReference reference)
            {
                if (reference == null || string.IsNullOrEmpty(reference.AssetGUID))
                {
                    return null;
                }
                string assetPath = AssetDatabase.GUIDToAssetPath(reference.AssetGUID);
                return AssetDatabase.LoadMainAssetAtPath(assetPath);
            }

            private static AssetReference Reference(Object asset)
            {
                if (asset == null) return new AssetReference("");
                bool isMainAsset = AssetDatabase.IsMainAsset(asset);
                if (!isMainAsset)
                {
                    throw new InvalidOperationException("只支持持久化主资产，请勿选择场景对象或子资产：" + asset.name);
                }
                string assetPath = AssetDatabase.GetAssetPath(asset);
                string assetGuid = AssetDatabase.AssetPathToGUID(assetPath);
                return new AssetReference(assetGuid);
            }

            public static T GetAsset<T>(MapRuntimeAssetRole role) where T : Object
            {
                var data = GetManifest();
                var entry = data.assets.SingleOrDefault(item => item.role == role);
                return entry == null ? null : Resolve(entry.asset) as T;
            }

            public static void SetAsset(MapRuntimeAssetRole role, Object asset)
            {
                var data = GetManifest();
                var reference = Reference(asset);
                var entry = data.assets.SingleOrDefault(item => item.role == role);
                if (entry == null)
                {
                    entry = new MapRuntimeAssetEntry { role = role };
                    data.assets.Add(entry);
                }
                entry.asset = reference;
                Save(data);
            }

            // 仅为空列表读取指定目录顶层的高度 SO，保留已有人工配置。
            public static void FillDefaultHeightAssets()
            {
                var data = GetManifest();
                if (data.heightDataModels.Count > 0)
                {
                    return;
                }

                string directory = MapStoreEnum.HeightMapScriptableObjPath;
                if (!AssetDatabase.IsValidFolder(directory))
                {
                    Debug.LogError("[WarGameMap] 高度数据目录不存在，请手动配置：" + directory);
                    return;
                }

                string[] files = Directory.GetFiles(directory, "*.asset", SearchOption.TopDirectoryOnly);
                Array.Sort(files, StringComparer.Ordinal);
                var heights = new List<HeightDataModel>();
                foreach (string file in files)
                {
                    string assetPath = file.Replace('\\', '/');
                    var height = AssetDatabase.LoadAssetAtPath<HeightDataModel>(assetPath);
                    if (height != null)
                    {
                        heights.Add(height);
                    }
                }

                if (heights.Count == 0)
                {
                    Debug.LogError("[WarGameMap] 目录顶层没有高度数据 SO，请手动配置：" + directory);
                    return;
                }

                SetHeightAssets(heights);
            }

            public static List<HeightDataModel> GetHeightAssets()
            {
                var data = GetManifest();
                return data.heightDataModels.Select(reference => Resolve(reference) as HeightDataModel).ToList();
            }

            public static void SetHeightAssets(List<HeightDataModel> heights)
            {
                var data = GetManifest();
                var references = heights.Select(height => Reference(height)).ToList();
                var oldGuids = data.heightDataModels.Select(reference => reference.AssetGUID);
                var newGuids = references.Select(reference => reference.AssetGUID);
                if (oldGuids.SequenceEqual(newGuids))
                {
                    return;
                }
                data.heightDataModels = references;
                Save(data);
            }

            public static void SetTerrainDirectory(string directory)
            {
                var data = GetManifest();
                data.terrainDirectory = directory;
                Save(data);
            }

            private static void Save(MapRuntimeAssetsSO data)
            {
                EditorUtility.SetDirty(data);
                AssetDatabase.SaveAssetIfDirty(data);
            }

            private sealed class Registration
            {
                public string Guid, Address, Group, Label;
                public bool LabelUsesFileName;
            }

            private static string GetLabel(Object asset, MapRuntimeAssetRole role)
            {
                switch (role)
                {
                    case MapRuntimeAssetRole.GridTerrain:
                        return GridTerrainSO.GetDefaultAssetName();
                    case MapRuntimeAssetRole.Country:
                        return CountrySO.GetDefaultAssetName();
                    default:
                        return Path.GetFileName(AssetDatabase.GetAssetPath(asset));
                }
            }

            public static void UpdateAddressables()
            {
                try { UpdateChecked(); }
                catch (Exception exception)
                {
                    Debug.LogError("[WarGameMap] 更新Addressables 资产失败：" + exception.Message);
                    throw;
                }
            }

            // 全部校验通过后才更新清单地块列表与 Addressables 登记。
            private static void UpdateChecked()
            {
                var watch = System.Diagnostics.Stopwatch.StartNew();
                var data = GetManifest();
                var settings = AddressableAssetSettingsDefaultObject.Settings;
                bool hasSettings = settings != null;
                if (!hasSettings)
                {
                    throw new InvalidOperationException("请先创建项目 Addressables Settings");
                }
                var registrations = new List<Registration>();
                foreach (var field in Fields)
                {
                    var entries = data.assets.Where(x => x.role == field.Role).ToList();
                    bool hasSingleEntry = entries.Count == 1;
                    if (!hasSingleEntry)
                    {
                        throw new InvalidOperationException("资源角色缺失或重复：" + field.Label);
                    }
                    var asset = Resolve(entries[0].asset);
                    bool hasExpectedType = asset != null && field.Type.IsInstanceOfType(asset);
                    if (!hasExpectedType)
                    {
                        throw new InvalidOperationException("必需资源为空或类型错误：" + field.Label);
                    }
                    string assetPath = AssetDatabase.GetAssetPath(asset);
                    bool isRenderAsset = field.Role >= MapRuntimeAssetRole.LandformMaterial
                        && field.Role <= MapRuntimeAssetRole.RegionSDFShader;
                    string groupName = isRenderAsset ? MapStoreEnum.RuntimeRenderGroup : MapStoreEnum.RuntimeConfigGroup;
                    bool labelUsesFileName = field.Role != MapRuntimeAssetRole.GridTerrain
                        && field.Role != MapRuntimeAssetRole.Country;
                    Add(registrations, asset, assetPath, groupName,
                        GetLabel(asset, field.Role), labelUsesFileName);
                }
                bool hasKnownRoles = data.assets.Count == Fields.Length;
                if (!hasKnownRoles)
                {
                    throw new InvalidOperationException("清单包含未知资源角色");
                }
                bool hasHeightData = data.heightDataModels.Count > 0;
                if (!hasHeightData)
                {
                    throw new InvalidOperationException("高度数据列表为空");
                }
                var heights = new HashSet<string>();
                foreach (var reference in data.heightDataModels)
                {
                    var height = Resolve(reference) as HeightDataModel;
                    bool hasValidHeight = height != null && height.singleHeightFileSize > 0;
                    if (!hasValidHeight)
                    {
                        throw new InvalidOperationException("高度数据为空或尺寸无效");
                    }
                    bool isUniqueHeight = heights.Add(reference.AssetGUID);
                    if (!isUniqueHeight)
                    {
                        throw new InvalidOperationException("高度数据重复：" + height.name);
                    }
                    string heightPath = AssetDatabase.GetAssetPath(height);
                    Add(registrations, height, heightPath, MapStoreEnum.RuntimeConfigGroup,
                        Path.GetFileName(heightPath), true);
                }

                var materialEntry = data.assets.Single(x => x.role == MapRuntimeAssetRole.LandformMaterial);
                var material = (Material)Resolve(materialEntry.asset);
                bool hasLandformShader = material.shader != null && material.shader.name == "WarGameMap/Terrain/TerrainLandform";
                if (!hasLandformShader)
                {
                    throw new InvalidOperationException("地貌材质必须使用 TerrainLandformShader");
                }
                var sdfEntry = data.assets.Single(x => x.role == MapRuntimeAssetRole.RegionSDFShader);
                var sdf = (ComputeShader)Resolve(sdfEntry.asset);
                var requiredKernels = new[] { "InitRegionBoundarySeeds", "RegionJumpFlood", "FinalizeRegionDistance" };
                foreach (var kernel in requiredKernels)
                {
                    bool hasKernel = sdf.HasKernel(kernel);
                    if (!hasKernel)
                    {
                        throw new InvalidOperationException("区域 ComputeShader 缺少 kernel：" + kernel);
                    }
                }
                var terrainEntry = data.assets.Single(x => x.role == MapRuntimeAssetRole.TerrainSetting);
                var terrain = (TerrainSettingSO)Resolve(terrainEntry.asset);
                bool hasValidTerrainSize = terrain.tileSize > 0 && terrain.clusterSize > 0 && terrain.clusterSize % terrain.tileSize == 0 && terrain.LODLevel > 0;
                if (!hasValidTerrainSize)
                {
                    throw new InvalidOperationException("地形配置尺寸或 LOD 无效");
                }
                var directory = (data.terrainDirectory ?? "").Replace('\\', '/').TrimEnd('/');
                bool isInsideAssets = directory.StartsWith("Assets/", StringComparison.Ordinal);
                string[] directorySegments = directory.Split('/');
                bool hasRelativeSegment = directorySegments.Any(segment => segment == ".." || segment == ".");
                bool hasValidDirectory = isInsideAssets && !hasRelativeSegment && AssetDatabase.IsValidFolder(directory);
                if (!hasValidDirectory)
                {
                    throw new InvalidOperationException("地块目录必须是 Assets 下明确的有效目录");
                }
                string clusterFilePattern = TerrainSettingSO.GetClusterFileSuffixName();
                var files = Directory.GetFiles(directory, clusterFilePattern, SearchOption.TopDirectoryOnly);
                Array.Sort(files, StringComparer.Ordinal);
                bool hasClusterFiles = files.Length > 0;
                if (!hasClusterFiles)
                {
                    throw new InvalidOperationException("地块目录为空：" + directory);
                }
                var clusters = new List<MapRuntimeClusterEntry>();
                var coordinates = new HashSet<Vector3Int>();
                var indices = new HashSet<Vector3Int>();
                var expectedTerrainSetting = terrain.GetTerrainSetting();
                foreach (var file in files)
                {
                    var path = file.Replace('\\', '/');
                    var asset = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
                    bool hasClusterAsset = asset != null;
                    if (!hasClusterAsset)
                    {
                        throw new InvalidOperationException("地块未导入为 TextAsset：" + path);
                    }
                    using (var reader = new BinaryReader(File.OpenRead(path)))
                    {
                        var stored = new TerrainSetting();
                        stored.ReadFromBinary(reader);
                        bool hasMatchingSettings = stored == expectedTerrainSetting;
                        if (!hasMatchingSettings)
                        {
                            throw new InvalidOperationException("地块与地形配置不一致：" + path);
                        }
                        int clusterCount = reader.ReadInt32();
                        bool hasSingleCluster = clusterCount == 1;
                        if (!hasSingleCluster)
                        {
                            throw new InvalidOperationException("每个地块文件必须恰好包含一个 cluster：" + path);
                        }
                        var item = new MapRuntimeClusterEntry {
                            idxX = reader.ReadInt32(), idxY = reader.ReadInt32(),
                            longitude = reader.ReadInt32(), latitude = reader.ReadInt32(), asset = Reference(asset)
                        };
                        item.lodLevel = reader.ReadInt32();
                        string fileName = Path.GetFileName(path);
                        bool parsedFileName = TerrainSettingSO.TryParseClusterFileName(fileName, out long lon, out long lat, out int lod);
                        bool hasMatchingFileName = parsedFileName && lon == item.longitude && lat == item.latitude && lod == item.lodLevel;
                        if (!hasMatchingFileName)
                        {
                            throw new InvalidOperationException("文件名和地块经纬度不一致：" + path);
                        }
                        bool indicesInBounds = item.idxX >= 0 && item.idxY >= 0
                            && item.idxX <= terrain.terrainSize.x && item.idxY <= terrain.terrainSize.z;
                        int expectedIndexX = item.longitude - terrain.startLL.x;
                        int expectedIndexY = item.latitude - terrain.startLL.y;
                        bool hasValidIndices = indicesInBounds && item.idxX == expectedIndexX && item.idxY == expectedIndexY;
                        if (!hasValidIndices)
                        {
                            throw new InvalidOperationException("地块索引与地图配置不一致：" + path);
                        }
                        var coordinate = new Vector3Int(item.longitude, item.latitude, item.lodLevel);
                        var index = new Vector3Int(item.idxX, item.idxY, item.lodLevel);
                        bool hasUniqueCoordinates = coordinates.Add(coordinate) && indices.Add(index);
                        if (!hasUniqueCoordinates)
                        {
                            throw new InvalidOperationException("地块坐标或索引重复：" + path);
                        }
                        int tiles = reader.ReadInt32();
                        int side = terrain.clusterSize / terrain.tileSize;
                        int expectedTileCount = checked(side * side);
                        bool hasExpectedTileCount = tiles == expectedTileCount;
                        if (!hasExpectedTileCount)
                        {
                            throw new InvalidOperationException("tile 数量与配置不一致：" + path);
                        }
                        long length = 0;
                        for (int i = 0; i < tiles; i++)
                        {
                            int size = reader.ReadInt32();
                            bool hasValidTileSize = size > 0;
                            if (!hasValidTileSize)
                            {
                                throw new InvalidOperationException("tile 长度无效：" + path);
                            }
                            length += size;
                        }
                        long remainingBytes = reader.BaseStream.Length - reader.BaseStream.Position;
                        bool hasExpectedPayloadLength = remainingBytes == length;
                        if (!hasExpectedPayloadLength)
                        {
                            throw new InvalidOperationException("地块 payload 长度不一致：" + path);
                        }
                        clusters.Add(item);
                        string clusterAddress = TerrainSettingSO.GetClusterFileName(lon, lat, lod);
                        Add(registrations, asset, clusterAddress, MapStoreEnum.TerrainMeshAssetGroupName,
                            clusterAddress, true);
                    }
                }
                Add(registrations, data, MapStoreEnum.RuntimeManifestAddress, MapStoreEnum.RuntimeConfigGroup,
                    Path.GetFileName(AssetDatabase.GetAssetPath(data)), false);
                // Complete all validation before changing any group or generated cluster list.
                ValidateRegistrations(settings, registrations);
                foreach (var registration in registrations)
                {
                    var group = settings.FindGroup(registration.Group);
                    if (group == null)
                        group = settings.CreateGroup(registration.Group, false, false, true, null,
                            typeof(BundledAssetGroupSchema), typeof(ContentUpdateGroupSchema));
                    var entry = settings.CreateOrMoveEntry(registration.Guid, group);
                    if (registration.LabelUsesFileName && entry.address != registration.Address)
                    {
                        string oldFileName = Path.GetFileName(entry.address);
                        if (!string.IsNullOrEmpty(oldFileName) && entry.labels.Contains(oldFileName))
                            entry.SetLabel(oldFileName, false);
                    }
                    entry.SetAddress(registration.Address);
                    entry.SetLabel(registration.Label, true, true);
                }
                data.clusters = clusters;
                Save(data);
                EditorUtility.SetDirty(settings);
                AssetDatabase.SaveAssets();
                Debug.Log("[WarGameMap] Addressables 登记完成：资源 " + registrations.Count + "，地块 " + clusters.Count +
                    "，耗时 " + watch.ElapsedMilliseconds + " ms。请通过 Addressables 构建入口打包。");
            }

            private static void Add(List<Registration> list, Object asset, string address,
                string group, string label, bool labelUsesFileName)
            {
                string assetPath = AssetDatabase.GetAssetPath(asset);
                string guid = AssetDatabase.AssetPathToGUID(assetPath);
                bool isPersistentMainAsset = !string.IsNullOrEmpty(guid) && AssetDatabase.IsMainAsset(asset);
                if (!isPersistentMainAsset)
                {
                    throw new InvalidOperationException("资源不是持久化主资产：" + asset.name);
                }
                if (string.IsNullOrWhiteSpace(label))
                {
                    throw new InvalidOperationException("资源 label 为空：" + assetPath);
                }
                var previous = list.Find(x => x.Guid == guid);
                if (previous != null)
                {
                    bool hasSameRegistration = previous.Address == address && previous.Group == group
                        && previous.Label == label && previous.LabelUsesFileName == labelUsesFileName;
                    if (!hasSameRegistration)
                    {
                        throw new InvalidOperationException("资源登记用途冲突：" + asset.name);
                    }
                    return;
                }
                list.Add(new Registration { Guid = guid, Address = address, Group = group,
                    Label = label, LabelUsesFileName = labelUsesFileName });
            }

            private static void ValidateRegistrations(AddressableAssetSettings settings, List<Registration> list)
            {
                var labelOwners = new Dictionary<string, Registration>(StringComparer.Ordinal);
                foreach (var item in list)
                {
                    if (labelOwners.TryGetValue(item.Label, out var other) && other.Guid != item.Guid)
                    {
                        throw new InvalidOperationException("自动 label 重复：" + item.Label + "；资源："
                            + AssetDatabase.GUIDToAssetPath(other.Guid) + "、"
                            + AssetDatabase.GUIDToAssetPath(item.Guid));
                    }
                    labelOwners[item.Label] = item;
                }
                var addresses = list.Select(registration => registration.Address);
                int uniqueAddressCount = addresses.Distinct(StringComparer.Ordinal).Count();
                bool hasUniqueAddresses = uniqueAddressCount == list.Count;
                if (!hasUniqueAddresses)
                {
                    throw new InvalidOperationException("生成地址重复");
                }
                foreach (var item in list)
                {
                    var existing = settings.FindAssetEntry(item.Guid);
                    bool hasExpectedGroup = existing == null || existing.parentGroup.Name == item.Group;
                    if (!hasExpectedGroup)
                    {
                        throw new InvalidOperationException("资源已在其他分组，禁止自动搬迁：" + item.Address);
                    }
                    var group = settings.FindGroup(item.Group);
                    bool hasBundleSchema = group == null || group.GetSchema<BundledAssetGroupSchema>() != null;
                    bool hasContentUpdateSchema = group == null || group.GetSchema<ContentUpdateGroupSchema>() != null;
                    bool hasBuildSchemas = hasBundleSchema && hasContentUpdateSchema;
                    if (!hasBuildSchemas)
                    {
                        throw new InvalidOperationException("已有分组缺少 AB 构建 schema：" + item.Group);
                    }
                    foreach (var otherGroup in settings.groups)
                    {
                        if (otherGroup == null) continue;
                        bool hasAvailableAddress = !otherGroup.entries.Any(x => x.address == item.Address && x.guid != item.Guid);
                        if (!hasAvailableAddress)
                        {
                            throw new InvalidOperationException("Addressables 地址被其他资产占用：" + item.Address);
                        }
                    }
                }
            }

        }

        #endregion

        #region 渲染设置

        [FoldoutGroup("渲染 设置")]
        [Button("test", ButtonSizes.Medium)]
        private void SetRenderTest()
        {
            // TODO : 要在这里集中地管理 Render 资产
        }

        #endregion

    }
}
