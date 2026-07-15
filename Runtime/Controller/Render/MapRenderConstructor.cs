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
        [SerializeField] Material MainMaterial;
        [SerializeField] Material TerLandformMaterial;
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

            // ===== 六边形边框参数 =====
            TerLandformMaterial.SetFloat("_HexGridEdgeRatio", 0.075f);
            TerLandformMaterial.SetFloat("_HexGridEdgeStartLerp", 0.75f);
            TerLandformMaterial.SetColor("_HexGridEdgeColor", new Color(0.3f, 0.3f, 0.3f, 1f));

            // 排除列表：浅海(0)、深海(1)、山脉(4) → 不显示边框
            uint[] excludeLUT = new uint[terrainTypeCount];
            excludeLUT[0] = 1; // ShallowSea
            excludeLUT[1] = 1; // DeepSea
            excludeLUT[4] = 1; // Mountain
            excludeOutlineLUT = new ComputeBuffer(terrainTypeCount, sizeof(uint));
            excludeOutlineLUT.SetData(excludeLUT);
            TerLandformMaterial.SetBuffer("_ExcludeOutlineLUT", excludeOutlineLUT);

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
                    // 0°（当前）
                    // int idx = col * mapWidth + row;
                    // 90° 顺时针：col 变 row，row 变 (mapWidth - 1 - col)
                    // int idx = row * mapHeight + (mapWidth - 1 - col);
                    // // 注意：旋转后宽高互换，行数变 mapWidth，列数变 mapHeight
                    // // 180°：col 和 row 都反向
                    // int idx = (mapWidth - 1 - col) * mapWidth + (mapHeight - 1 - row);
                    // // 270° 顺时针：col 变 (mapHeight - 1 - row)，row 变 col
                    int idx = (mapHeight - 1 - row) * mapHeight + col;
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
                    data[idx] = terrainTypeID;
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


        // TODO : 思考一下有没有必要做这个 IOA
        private void InitSDFResource(int width, int height, int clusterSize)
        {
            int bufferSize = clusterSize * clusterSize;
            pixelDataBuffer = new ComputeBuffer(bufferSize, sizeof(float) * 4);
            threadGroupX = Mathf.CeilToInt(width / 16.0f);
            threadGroupY = Mathf.CeilToInt(height / 16.0f);

            // 应当缓存 512 * 512 大小的纹理 * 9 以便于重算 JFA

            // TODO : 继续设置数据
            SDFGenShader.SetInt("_ClusterWidth", terSet.clusterSize);
            SDFGenShader.SetInt("_ClusterHeight", terSet.clusterSize);
            SDFGenShader.SetInt("_HexGridSize", hexSet.hexGridSize);
        }

        public void UpdateCountryBorderData(Vector2Int longitudeAndLatitude)
        {
            SDFGenShader.SetInt("_StartLongitude", longitudeAndLatitude.x);
            SDFGenShader.SetInt("_StartLatitude", longitudeAndLatitude.y);


            int initJFAKernelIndex = SDFGenShader.FindKernel("InitJFA");
            int JFAIterKernelIndex = SDFGenShader.FindKernel("JFAIter");
            int genSDFKernelIndex = SDFGenShader.FindKernel("GenSDF");
            // TODO : 继续设置数据
            SDFGenShader.SetTexture(initJFAKernelIndex, "CountryTexture", RegionTexture);
            SDFGenShader.SetBuffer(initJFAKernelIndex, "PixelDataBuffer", pixelDataBuffer);
            SDFGenShader.Dispatch(initJFAKernelIndex, threadGroupX, threadGroupY, 1);

            //SDFGenShader.SetVectorArray("_CountryBorderColors", countrySO.countryBorderColors);
            // TODO : 继续设置数据
            SDFGenShader.Dispatch(JFAIterKernelIndex, threadGroupX, threadGroupY, 1);

            // TODO : 继续设置数据
            SDFGenShader.Dispatch(genSDFKernelIndex, threadGroupX, threadGroupY, 1);
        }
    
    }
}
