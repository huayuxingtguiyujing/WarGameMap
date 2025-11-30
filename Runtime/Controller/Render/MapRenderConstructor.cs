using LZ.WarGameMap.Runtime.Model;
using System.Collections.Generic;
using UnityEngine;

namespace LZ.WarGameMap.Runtime
{
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
        [SerializeField] Material RiverMaterial;

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

            MapModeList = new List<BaseMapMode>()
            {
                new CountryMapMode(), new TerrainMapMode(), new PoliticalMapMode()
            };
            foreach (var mapMode in MapModeList)
            {
                MapModeDict.Add(mapMode.GetMapModeName(), mapMode);
            }
        }

        public void InitMaterial(Material MainMaterial, Material RiverMaterial)
        {
            this.MainMaterial = MainMaterial;
            this.RiverMaterial = RiverMaterial;

            InitMainMaterial();
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

        private void InitRiverMaterial()
        {
            // TODO : river mat 的参数到外部配置
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
