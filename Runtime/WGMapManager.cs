using System.Collections.Generic;
using UnityEngine;
using LZ.WarGameMap.Runtime.Model;

namespace LZ.WarGameMap.Runtime
{
    public class WGMapManager : MonoBehaviour
    {
        
        // TODO ： 这些组件全部需要动态加载，但是AB加载功能还没有封装完成，需要考虑怎么处理
        // 现阶段先直接挂接（8.3日），属性是public，后续改为private
        #region 初始化地图需要的功能组件

        public TerrainConstructor TerrainCtor;
        public HexmapConstructor HexCtor;
        public MapRenderConstructor RenderCtor;

        public HexSettingSO hexSet;
        public TerrainSettingSO terSet;
        public MapRuntimeSetting mapSet;

        public GridTerrainSO gridTerrainSO;
        public CountrySO countrySO;

        [Header("地图主材质")]
        public Material mainMaterial;
        [Header("地貌材质")]
        public Material terrainLandformMat;
        [Header("河流材质")]
        public Material riverMaterial;

        [Header("高度数据模型列表")]
        public List<HeightDataModel> heightDataModels;
        [Header("河流数据")]
        public MapRiverData mapRvData;

        [Header("生成选项")]
        public bool shouldGenRiver = false;
        public bool shouldGenLODBySimplify = false;
        public bool genRuntimeClusterMesh = false;

        #endregion

        // ***
        // 调用此函数一键完成war game map的初始化
        // ***
        public void InitWarGameMap()
        {
            InitTerrain();
            InitLandform();
            InitRiverAndWater();
            InitRegion();
            InitGamePlay();
        }

        private void InitTerrain(){
            int tileNumARow = terSet.clusterSize / terSet.tileSize;

            DebugUtility.Log($"the map size is : {terSet.terrainSize}");
            DebugUtility.Log($"the cluster size : {terSet.clusterSize}, the tile size : {terSet.tileSize}, there are {tileNumARow} tiles per line");

            if (TerrainCtor == null) {
                Debug.LogError("terrian ctor is null!");
                return;
            }

            if (heightDataModels == null) {
                Debug.LogError("you do not set the heightDataModel");
                return;
            }

            // 初始化地块
            TerrainCtor.InitTerrainCons(mapSet, terSet, hexSet, heightDataModels, null, mainMaterial, mapRvData);
        
            List<Vector2Int> clusterIdxList = GetBuildClusterTargets();

            // 实际构建地块Mesh
            TerrainGenTask terrainGenTask = new TerrainGenTask(heightDataModels, terSet, TerrainCtor, 
                clusterIdxList, shouldGenRiver, shouldGenLODBySimplify, genRuntimeClusterMesh);
            int taskID = TaskManager.GetInstance().StartProgress(TaskTickLevel.Medium, terrainGenTask);
            // TODO: TerGenTaskPop 是 Editor 代码，Runtime 中不可用，后续需要用 Runtime 的进度UI
            // TerGenTaskPop.GetPopInstance().ShowBasePop(terrainGenTask);
            terrainGenTask.StartTask(taskID);
        }

        /// 默认构建所有 cluster
        private List<Vector2Int> GetBuildClusterTargets()
        {
            List<Vector2Int> result = new List<Vector2Int>();
            int clusterHeight = terSet.terrainSize.x/ terSet.clusterSize;
            int clusterWidth = terSet.terrainSize.z / terSet.clusterSize;
            for (int x = 0; x < clusterHeight; x++)
            {
                for (int y = 0; y < clusterWidth; y++)
                {
                    result.Add(new Vector2Int(x, y));
                }
            }
            return result;
        }

        private void InitLandform(){
            RenderCtor.InitMapRenderCons(terSet, hexSet, mapSet, gridTerrainSO, countrySO);
            RenderCtor.InitMaterial(mainMaterial, terrainLandformMat, riverMaterial);
        }

        private void InitRiverAndWater(){

        }

        private void InitRegion(){

        }

        private void InitGamePlay(){

        }

    }
}
