using LZ.WarGameMap.Runtime.Model;
using System.Collections.Generic;
using UnityEngine;

namespace LZ.WarGameMap.Runtime
{
    // 后续考虑移入 model 里面
    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public struct TerrainMaterialParams
    {
        public float roughness;
        public float metallic;
        public float detailFrequency;
        public float detailStrength;
    }

    // This class is used to manage map rendering
    // Features : 
    //      - Support change map mode, like politic mode, terrain mode etc
    //      - 
    public class MapRenderConstructor : MonoBehaviour
    {
        // All map setting and other assets
        TerrainSettingSO terSet;
        HexSettingSO hexSet;
        MapRuntimeSetting mapSet;

        GridTerrainSO gridTerrainSO;
        CountrySO countrySO;


        // Cur using map mode
        public BaseMapMode CurMapMode;

        // All enabled map mode, it means how to render game map
        public List<BaseMapMode> MapModeList = new List<BaseMapMode>();

        Dictionary<string, BaseMapMode> MapModeDict = new Dictionary<string, BaseMapMode>();


        #region Map render needed assets

        // TODO : 各个 Material, 后续要在外部配置资产，然后自动加载
        [Header("Render Material")]
        [SerializeField] Material MainMaterial;         // 旧版本的MainMaterial
        [SerializeField] Material TerLandformMaterial;      // 现在的CK3 material
        [SerializeField] Material RiverMaterial;

        [Header("Render Terrain Assets")]
        [SerializeField] ComputeBuffer terrainIDBuffer;
        [SerializeField] ComputeBuffer terrainMaterialParamsBuffer;
        [SerializeField] ComputeBuffer excludeOutlineLUT;
        [SerializeField] Texture2DArray TerrainAlbedoArray;
        [SerializeField] Texture2DArray TerrainNormalArray;

        //  TODO : CountryTexture HexGridTexture 资源，需要指定路径后，自动加载
        [Header("Render Assets")]
        [SerializeField] Texture2D GridTerrainTexture;

        [SerializeField] Texture2D RegionTexture;
        [SerializeField] Texture2D ProvinceTexture;
        [SerializeField] Texture2D PrefectureTexture;
        [SerializeField] Texture2D SubPrefectureTexture;

        [Header("Region Divide SDF")]
        [SerializeField] ComputeShader RegionSDFShader;    // 挂 SDFComputer.compute
        [SerializeField] Texture2D testResult;    // 挂 SDFComputer.compute
        [SerializeField] int RegionSDFResolution = 1024;
        // SDF 相关 RT（运行时创建）
        RenderTexture regionSeedA;        // RG32_SFloat
        RenderTexture regionSeedB;        // RG32_SFloat
        RenderTexture regionDistanceRT;   // R16_SFloat（最终距离场）
        // SDF 世界矩形与 texel 尺寸，注入到 material
        Vector4 regionSDFWorldRect;
        float regionSDFTexelWorldSize;



        #endregion


        // [Obosolete] SDF - Gen
        ComputeShader SDFGenShader;
        ComputeBuffer pixelDataBuffer;
        int threadGroupX;
        int threadGroupY;


        #region Init Map Render

        public void InitMapRenderCons(TerrainSettingSO terSet, HexSettingSO hexSet, MapRuntimeSetting mapSet, GridTerrainSO gridTerrainSO, CountrySO countrySO)
        {
            this.terSet = terSet;
            this.hexSet = hexSet;
            this.mapSet = mapSet;
            this.gridTerrainSO = gridTerrainSO;
            this.countrySO = countrySO;

            MapModeDict.Clear();
            MapModeList = new List<BaseMapMode>()
            {
                new CountryMapMode(), new TerrainMapMode(), new PoliticalMapMode()
            };
            foreach (var mapMode in MapModeList)
            {
                MapModeDict.Add(mapMode.GetMapModeName(), mapMode);
            }
        }

        #region Runtime landform initialization

        // 注入已加载的渲染资产，不读取路径或创建替代资源。
        public void SetLandformResources(Texture2DArray albedoArray, Texture2DArray normalArray,
            Texture2D regionTexture, ComputeShader regionSdfShader)
        {
            if (albedoArray == null || normalArray == null || regionTexture == null || regionSdfShader == null)
            {
                throw new System.ArgumentException("Landform textures and region compute shader are required.");
            }
            TerrainAlbedoArray = albedoArray;
            TerrainNormalArray = normalArray;
            RegionTexture = regionTexture;
            RegionSDFShader = regionSdfShader;
        }

        // 使用调用方持有的材质实例，复用地貌与初始区域 SDF 构建。
        public void InitRuntimeLandformMaterial(Material material)
        {
            ValidateRuntimeLandformResources(material);
            TerLandformMaterial = material;
            InitTerLandformMaterial();
        }

        // 校验现有渲染算法的输入约束，避免缺失资源时跳过初始化。
        private void ValidateRuntimeLandformResources(Material material)
        {
            if (terSet == null || hexSet == null || mapSet == null || gridTerrainSO == null || countrySO == null)
            {
                throw new System.InvalidOperationException("Call InitMapRenderCons before initializing landform rendering.");
            }
            if (material == null || material.shader == null || material.shader.name != "WarGameMap/Terrain/TerrainLandform")
            {
                throw new System.ArgumentException("The material must use TerrainLandformShader.", nameof(material));
            }
            if (TerrainAlbedoArray == null || TerrainNormalArray == null || RegionTexture == null || RegionSDFShader == null)
            {
                throw new System.InvalidOperationException("Call SetLandformResources before initializing landform rendering.");
            }
            // 当前地貌索引布局使用方形网格，此入口不修改既有采样算法。
            if (hexSet.mapWidth <= 0 || hexSet.mapWidth != hexSet.mapHeight || hexSet.hexGridSize <= 0)
            {
                throw new System.InvalidOperationException("The existing landform layout requires a positive square Hex map.");
            }
            if (terSet.clusterSize <= 0 || terSet.terrainSize.x <= 0 || terSet.terrainSize.z <= 0 || RegionSDFResolution <= 0)
            {
                throw new System.InvalidOperationException("Terrain world size and region SDF resolution must be positive.");
            }
            var terrainTypes = gridTerrainSO.GridTerrainTypeList;
            var gridTypes = gridTerrainSO.HexmapGridTerTypeList;
            int gridCount = checked(hexSet.mapWidth * hexSet.mapHeight);
            if (terrainTypes == null || terrainTypes.Count < 5 || gridTypes == null || gridTypes.Count != gridCount)
            {
                throw new System.InvalidOperationException("Landform types or Hex grid data are incomplete.");
            }
            int terrainTypeCount = terrainTypes.Count;
            if (TerrainAlbedoArray.depth < terrainTypeCount || TerrainNormalArray.depth < terrainTypeCount)
            {
                throw new System.InvalidOperationException("Landform texture arrays do not contain all terrain types.");
            }
            foreach (var grid in gridTypes)
            {
                if (grid[0] >= terrainTypeCount)
                {
                    throw new System.InvalidOperationException("A Hex grid has an invalid base terrain type.");
                }
            }
            string[] kernels = { "InitRegionBoundarySeeds", "RegionJumpFlood", "FinalizeRegionDistance" };
            foreach (string kernel in kernels)
            {
                if (!RegionSDFShader.HasKernel(kernel))
                {
                    throw new System.InvalidOperationException("Missing region SDF kernel: " + kernel);
                }
            }
        }

        #endregion

        public void InitMaterial(Material MainMaterial, Material terLandformMat, Material RiverMaterial)
        {
            this.MainMaterial = MainMaterial;
            this.TerLandformMaterial = terLandformMat;
            this.RiverMaterial = RiverMaterial;

            InitMainMaterial();
            InitTerLandformMaterial();
            InitRiverMaterial();
        }

        private void InitMainMaterial()
        {
            // Set TerrainType color to Material
            MainMaterial.SetColor("_PlainColor", BaseGridTerrain.PlainType.terrainEditColor);
            MainMaterial.SetColor("_HillColor", BaseGridTerrain.HillType.terrainEditColor);
            MainMaterial.SetColor("_MountainColor", BaseGridTerrain.MountainType.terrainEditColor);
            MainMaterial.SetColor("_PlateauColor", BaseGridTerrain.PlateauType.terrainEditColor);
            MainMaterial.SetColor("_SnowColor", BaseGridTerrain.SnowType.terrainEditColor);

            // 设置各项地图字段
            MainMaterial.SetInt("_HexmapWidth", hexSet.mapWidth);
            MainMaterial.SetInt("_HexmapHeight", hexSet.mapHeight);
            MainMaterial.SetInt("_HexGridSize", hexSet.hexGridSize);
            MainMaterial.SetFloat("_EdgeRatio", hexSet.hexEdgeRatio);
            // Hex setting
            //_HexGridScale("Hex Grid Scale", Float) = 2
            //_HexGridSize("Hex Grid Size", Range(1, 300)) = 20
            //_HexGridEdgeRatio("Hex Grid Edge Ratio", Range(0.001, 1)) = 0.1
            //// 描边-边界相关
            //_EdgeRatio("Edge Ratio", Float) = 0.8

            // 设置纹理资产
            MainMaterial.SetTexture("_GridTerrainTypeTexture", GridTerrainTexture);

            MainMaterial.SetTexture("_RegionTexture", RegionTexture);
            MainMaterial.SetTexture("_ProvinceTexture", ProvinceTexture);
            MainMaterial.SetTexture("_PrefectureTexture", PrefectureTexture);
            MainMaterial.SetTexture("_SubPrefectureTexture", SubPrefectureTexture);
        
        }

        private void InitTerLandformMaterial()
        {
            if (terrainIDBuffer != null) {
                terrainIDBuffer.Release();
            }
            if (terrainMaterialParamsBuffer != null) {
                terrainMaterialParamsBuffer.Release();
            }
            if (excludeOutlineLUT != null) {
                excludeOutlineLUT.Release();
            }

            // 创建 TerrainID Map (R8, 单通道 byte, 直接从 HexmapGridTerTypeList 构建)
            FillTerrainIDMap();

            // 创建 MaterialParams Buffer
            int terrainTypeCount = gridTerrainSO.GridTerrainTypeList.Count;
            terrainMaterialParamsBuffer = new ComputeBuffer(terrainTypeCount, sizeof(float) * 4);

            TerrainMaterialParams[] paramsArray = new TerrainMaterialParams[terrainTypeCount];
            for (int i = 0; i < terrainTypeCount; i++)
            {
                // TODO ： 目前阶段先给默认值，后续从配置加载
                paramsArray[i] = new TerrainMaterialParams
                {
                    roughness = 0.5f,
                    metallic = 0.0f,
                    detailFrequency = 2.0f,
                    detailStrength = 0.1f,
                };
            }
            terrainMaterialParamsBuffer.SetData(paramsArray);

            // 绑定到 TerrainMaterial
            TerLandformMaterial.SetBuffer("_TerrainIDBuffer", terrainIDBuffer);
            TerLandformMaterial.SetTexture("_TerrainAlbedoArray", TerrainAlbedoArray);
            TerLandformMaterial.SetTexture("_TerrainNormalArray", TerrainNormalArray);

            TerLandformMaterial.SetBuffer("_TerrainMaterialParamsBuffer", terrainMaterialParamsBuffer);
            TerLandformMaterial.SetInt("_HexmapWidth", hexSet.mapWidth);
            TerLandformMaterial.SetInt("_HexmapHeight", hexSet.mapHeight);

            // 区域划分所需纹理（ApplyRegionDivide 依赖 _RegionTexture）
            TerLandformMaterial.SetTexture("_RegionTexture", RegionTexture);

            // ===== 六边形边框参数 =====
            TerLandformMaterial.SetFloat("_HexGridEdgeRatio", 0.04f);
            TerLandformMaterial.SetFloat("_HexGridEdgeStartLerp", 0.92f);
            TerLandformMaterial.SetColor("_HexGridEdgeColor", new Color(0.3f, 0.3f, 0.3f, 1f));

            // 排除列表：浅海(0)、深海(1)、山脉(4) → 不显示边框
            uint[] excludeLUT = new uint[terrainTypeCount];
            excludeLUT[0] = 1; // ShallowSea
            excludeLUT[1] = 1; // DeepSea
            excludeLUT[4] = 1; // Mountain
            excludeOutlineLUT = new ComputeBuffer(terrainTypeCount, sizeof(uint));
            excludeOutlineLUT.SetData(excludeLUT);
            TerLandformMaterial.SetBuffer("_ExcludeOutlineLUT", excludeOutlineLUT);

            // 区域划分：
            // TODO ：这玩意是动态变化的，要放到 update 里面，后续再变
            UpdateCountryDivideTexture();

            Debug.Log($"Terrain landform inited over!, terrainTypeCount : {terrainTypeCount}");
        }

        // 从 HexmapGridTerTypeList 构建 BaseLayer terrainID 纹理 (R8)
        private void FillTerrainIDMap()
        {
            int mapWidth = hexSet.mapWidth;
            int mapHeight = hexSet.mapHeight;
            int totalCount = mapWidth * mapHeight;
            
            uint[] data = new uint[totalCount];
            var typeList = gridTerrainSO.HexmapGridTerTypeList;

            int cnt1 = 0;
            int cnt2 = 0;
            int cnt3 = 0;
            int cnt4 = 0;
            int cnt5 = 0;
            for (int col = 0; col < mapWidth; col++)
            {
                for (int row = 0; row < mapHeight; row++)
                {
                    int dstIdx = col * mapWidth + row;   // 输出位置：始终原始行列

                    // 0°（当前）
                    // int idx = col * mapWidth + row;
                    int idx = row * mapWidth + col;
                    // 90° 顺时针：col 变 row，row 变 (mapWidth - 1 - col)
                    // int idx = row * mapHeight + (mapWidth - 1 - col);
                    // 注意：旋转后宽高互换，行数变 mapWidth，列数变 mapHeight
                    // 180°：col 和 row 都反向
                    // int idx = (mapWidth - 1 - col) * mapWidth + (mapHeight - 1 - row);
                    // 270° 顺时针：col 变 (mapHeight - 1 - row)，row 变 col
                    // int idx = (mapHeight - 1 - row) * mapHeight + col;
                    // int idx = (mapWidth - 1 - col) * mapWidth + row;  // 水平翻转

                    uint terrainTypeID = (uint)(idx < typeList.Count ? typeList[idx][0] : 0);

                    if (terrainTypeID == 0)
                    {
                        cnt1 ++;
                    }else if (terrainTypeID == 1)
                    {
                        cnt2++;
                    }
                    else if (terrainTypeID == 2)
                    {
                        cnt3++;
                    }
                    else if (terrainTypeID == 3)
                    {
                        cnt4++;
                    }
                    else if (terrainTypeID == 4)
                    {
                        cnt5++;
                    }
                    data[dstIdx] = terrainTypeID;
                }
            }
            Debug.Log($" idx0 : {cnt1},  idx1 : {cnt2},  idx2 : {cnt3},  idx3 : {cnt4},  idx4 : {cnt5}, ");
            
            terrainIDBuffer = new ComputeBuffer(totalCount, sizeof(uint));
            terrainIDBuffer.SetData(data);
        }

        private void InitRiverMaterial()
        {
            // TODO : river mat 的参数到外部配置
        }

        private void OnDestroy()
        {
            terrainIDBuffer?.Release();
            terrainMaterialParamsBuffer?.Release();
            excludeOutlineLUT?.Release();

            // 区域划分相关资产
            regionSeedA?.Release();
            regionSeedB?.Release();
            regionDistanceRT?.Release();
        }

        #endregion


        #region Map Mode

        public void EnterMapMode(string modeName)
        {
            BaseMapMode mapMode = MapModeDict[modeName];
            mapMode.EnterMapMode();
        }

        public void UpdateMapMode()
        {
            if(CurMapMode == null)
            {
                return;
            }

            CurMapMode.UpdateMapMode();
        }

        public void ExitMapMode()
        {
            CurMapMode.ExitMapMode();
        }

        #endregion

        #region 区域划分相关

        // 每次 区域信息有更新的时候，都要调用该函数来生成 新区域的 SDF
        private void UpdateCountryDivideTexture()
        {
            if (RegionSDFShader == null || RegionTexture == null)
            {
                Debug.LogError("RegionSDFShader or RegionTexture is null, skip SDF generation.");
                return;
            }

            int w = hexSet.mapWidth;
            int h = hexSet.mapHeight;
            float s = hexSet.hexGridSize;

            // _RegionSDFWorldRect 必须与 worldPos.xz 的真实世界范围一致。
            // 地形 mesh 从 (0,0) 起，每个 cluster 占据 clusterSize × clusterSize 世界空间，
            // 总范围 = clusterSize * terrainSize（terrainSize.x/z 表示 cluster 数量）。
            float minX = 0f;
            float minZ = 0f;
            float widthWorld = terSet.clusterSize * terSet.terrainSize.x;
            float heightWorld = terSet.clusterSize * terSet.terrainSize.z;
            regionSDFWorldRect = new Vector4(minX, minZ, widthWorld, heightWorld);

            int res = RegionSDFResolution;
            regionSDFTexelWorldSize = Mathf.Max(
                regionSDFWorldRect.z / res,
                regionSDFWorldRect.w / res);

            EnsureRegionSDFRTs(res);

            // 无归属色（与 CountrySO 保持一致）
            Color invalid = BaseCountryDatas.NotValidCountryColor;

            int initKernel  = RegionSDFShader.FindKernel("InitRegionBoundarySeeds");
            int jfaKernel   = RegionSDFShader.FindKernel("RegionJumpFlood");
            int finalKernel = RegionSDFShader.FindKernel("FinalizeRegionDistance");

            // 对三个 kernel 统一绑定公共参数
            int[] kernels = { initKernel, jfaKernel, finalKernel };
            foreach (int k in kernels)
            {
                // 所有参数用全局两参版本（对所有 kernel 生效，无需指定 kernel 索引）
                // RegionSDFShader.SetTexture(initKernel, "_RegionTexture", RegionTexture);
                RegionSDFShader.SetTexture(k, "_RegionTexture", RegionTexture);
                RegionSDFShader.SetInt("_HexmapWidth", w);
                RegionSDFShader.SetInt("_HexmapHeight", h);
                RegionSDFShader.SetVector("_RegionSDFWorldRect", regionSDFWorldRect);
                RegionSDFShader.SetVector("_RegionInvalidColor", new Vector4(invalid.r, invalid.g, invalid.b, invalid.a));
                RegionSDFShader.SetInt("_RegionTreatInvalidAsBoundary", 1);
                RegionSDFShader.SetInt("_RegionTreatMapEdgeAsBoundary", 1);
                RegionSDFShader.SetFloat("_HexGridSize", s);
            }
            // SetFloat 无三参重载，用全局两参版本（对所有 kernel 生效）
            RegionSDFShader.SetFloat("_HexGridSize", s);
            RegionSDFShader.SetFloat("_RegionSDFTexelWorldSize", regionSDFTexelWorldSize);
            RegionSDFShader.SetFloat("_RegionColorEpsilon", 0.01f);
            RegionSDFShader.SetFloat("_RegionSDFMaxDistanceWorld", hexSet.hexGridSize * 16.0f);

            int groups = Mathf.CeilToInt(res / 8.0f);

            // 1) 初始化种子：写入 regionSeedA
            
            RegionSDFShader.SetTexture(initKernel, "_RegionSeedWrite", regionSeedA);
            RegionSDFShader.Dispatch(initKernel, groups, groups, 1);

            // 2) JFA 迭代，ping-pong
            int maxStep = 1;
            while (maxStep * 2 < res) maxStep *= 2;
            RenderTexture readRT = regionSeedA;
            RenderTexture writeRT = regionSeedB;
            for (int step = maxStep; step >= 1; step /= 2)
            {
                RegionSDFShader.SetInt("_RegionJfaStep", step);
                RegionSDFShader.SetTexture(jfaKernel, "_RegionSeedRead", readRT);
                RegionSDFShader.SetTexture(jfaKernel, "_RegionSeedWrite", writeRT);
                RegionSDFShader.Dispatch(jfaKernel, groups, groups, 1);

                // 交换 read/write
                RenderTexture tmp = readRT;
                readRT = writeRT;
                writeRT = tmp;
            }

            // 3) 最终距离场：readRT 是最后一次写入的 seed
            RegionSDFShader.SetTexture(finalKernel, "_RegionSeedRead", readRT);
            RegionSDFShader.SetTexture(finalKernel, "_RegionDistanceTexture", regionDistanceRT);
            RegionSDFShader.Dispatch(finalKernel, groups, groups, 1);

            // 4) 注入到 TerLandformMaterial（TerrainLandformShader）（MainMat 是过时的）
            TerLandformMaterial.SetTexture("_RegionDistanceTexture", regionDistanceRT);
            TerLandformMaterial.SetVector("_RegionSDFWorldRect", regionSDFWorldRect);
            TerLandformMaterial.SetFloat("_RegionSDFTexelWorldSize", regionSDFTexelWorldSize);

            // terrain 真实世界范围（渲染采样 UV 用）
            Vector4 terrainWorldRect = new Vector4(
                0f,
                0f,
                terSet.clusterSize * terSet.terrainSize.x,
                terSet.clusterSize * terSet.terrainSize.z
            );
            TerLandformMaterial.SetVector("_RegionTerrainWorldRect", terrainWorldRect);

            // SDF 布局
            Vector4 regionSDFUVRect = new Vector4(0, 0, hexSet.hexGridSize, hexSet.hexGridSize);
            TerLandformMaterial.SetVector("_RegionSDFUVRect", regionSDFUVRect);
        
            Debug.Log("区域划分 SDF 生成完毕！");

            // Debug：把距离场回读到 testResult 以便在 Inspector 查看
            if (testResult == null || testResult.width != res || testResult.height != res)
            {
                testResult = new Texture2D(res, res, TextureFormat.RFloat, false);
            }
            RenderTexture.active = regionDistanceRT;
            testResult.ReadPixels(new Rect(0, 0, res, res), 0, 0);
            testResult.Apply();
            RenderTexture.active = null;
        }

        private void EnsureRegionSDFRTs(int res)
        {
            if (regionSeedA == null)
            {
                regionSeedA = new RenderTexture(res, res, 0, UnityEngine.Experimental.Rendering.GraphicsFormat.R32G32_SFloat);
                regionSeedA.enableRandomWrite = true;
                regionSeedA.Create();
            }
            if (regionSeedB == null)
            {
                regionSeedB = new RenderTexture(res, res, 0, UnityEngine.Experimental.Rendering.GraphicsFormat.R32G32_SFloat);
                regionSeedB.enableRandomWrite = true;
                regionSeedB.Create();
            }
            if (regionDistanceRT == null)
            {
                regionDistanceRT = new RenderTexture(res, res, 0, UnityEngine.Experimental.Rendering.GraphicsFormat.R16_SFloat);
                regionDistanceRT.enableRandomWrite = true;
                regionDistanceRT.filterMode = FilterMode.Bilinear;
                regionDistanceRT.wrapMode = TextureWrapMode.Clamp;
                regionDistanceRT.Create();
            }
        }

        #endregion
    
    }
}
