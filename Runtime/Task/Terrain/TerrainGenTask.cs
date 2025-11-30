using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace LZ.WarGameMap.Runtime
{
    public class TerrainGenTask : BaseTask
    {
        public override string GetTaskName() { return "TerrainGenProgressTimer"; }
        public override TaskTickLevel GetTickLevel() { return TaskTickLevel.Medium; }


        public static string TerGenClsTaskName = "地块Mesh与河流生成";
        public static string TerSimplifyTaskName = "地块减面";

        public static string TerGenClsGenMeshName = "生成Mesh";
        public static string TerGenClsGenRiverName = "生成河流";
        public static string TerGenClsSimplifyName = "LOD减面";


        int buildClusterNum;
        List<HeightDataModel> heightDataModels; 
        TerrainSettingSO terSet; 
        TerrainConstructor TerrainCtor; 
        List<Vector2Int> clusterIdxList; 
        bool shouldGenRiver;
        bool shouldGenLODBySimplify;
        bool useForRuntime;

        // Simplier list, keep the ref
        Dictionary<Vector2Int, int> SimplifyerClsIdxList = new Dictionary<Vector2Int, int>();
        List<TerrainSimplifier> SimplifierList = new List<TerrainSimplifier>();
        int TotalSimplifyTargetCnt = 1;

        // To cancel terrain gen process
        CancellationTokenSource tokenSrc;

        public TerrainGenTask(List<HeightDataModel> heightDataModels, TerrainSettingSO terSet, TerrainConstructor TerrainCtor, List<Vector2Int> clusterIdxList, bool shouldGenRiver, bool shouldGenLODBySimplify, bool useForRuntime) : base(null, -1, true)
        {
            this.buildClusterNum = clusterIdxList.Count;
            this.heightDataModels = heightDataModels;
            this.terSet = terSet;
            this.TerrainCtor = TerrainCtor;
            this.clusterIdxList = clusterIdxList;
            this.shouldGenRiver = shouldGenRiver;                   // TODO : 未来河流的机制可能会大改
            this.shouldGenLODBySimplify = shouldGenLODBySimplify;
            this.useForRuntime = useForRuntime;

            tokenSrc = new CancellationTokenSource();

            CheckBuildValid();
            InitTerGenChildTask();
            ProgressOverCall = BuildOverCallBack;
        }

        private void CheckBuildValid() 
        {
            foreach (var clusterIdx in clusterIdxList)
            {
                int longitude = terSet.startLL.x + clusterIdx.x;
                int latitude = terSet.startLL.y + clusterIdx.y;
                bool existInHeightDataModel = false;
                foreach (var model in heightDataModels)
                {
                    if (model.ExistHeightData(longitude, latitude))
                    {
                        existInHeightDataModel = true;
                        break;
                    }
                }

                if (!existInHeightDataModel)
                {
                    throw new Exception($"unable to find heightdata, longitude : {longitude}, latitude : {latitude}, so build not valid");
                }
            }
        }

        private void InitTerGenChildTask()
        {
            float buildMeshRiverWeight = 0.2f / buildClusterNum;
            TaskNode terGenMeshRiverNode = new TaskNode(TerGenClsTaskName, 0.2f, null, "");
            for (int i = 0; i < buildClusterNum; i++)
            {
                Vector2Int clsIdx = clusterIdxList[i];
                terGenMeshRiverNode.AddChildTask($"{TerGenClsTaskName}_{clsIdx}_{TerGenClsGenMeshName}", buildMeshRiverWeight / 2, null, $"地块{clsIdx} 生成地形Mesh中");
                terGenMeshRiverNode.AddChildTask($"{TerGenClsTaskName}_{clsIdx}_{TerGenClsGenRiverName}", buildMeshRiverWeight / 2, null, $"地块{clsIdx} 生成河流中");
            }
            AddChildTask(terGenMeshRiverNode);

            if (shouldGenLODBySimplify)
            {
                float simplifyWeight = 0.8f / buildClusterNum;
                TaskNode simplifyNode = new TaskNode(TerSimplifyTaskName, 0.8f, null, "");
                for (int i = 0; i < buildClusterNum; i++)
                {
                    Vector2Int clsIdx = clusterIdxList[i];
                    simplifyNode.AddChildTask($"{TerSimplifyTaskName}_{clsIdx}_{TerGenClsSimplifyName}", simplifyWeight, SetSimplifyProgressCall, $"地块{clsIdx} LOD减面中");
                }
                AddChildTask(simplifyNode);
            }
        }
        
        private void InitSimplifyList()
        {
            int tileNum = terSet.GetTileNumClsPerLine();
            SimplifierList = new List<TerrainSimplifier>(tileNum * tileNum * buildClusterNum * terSet.LODLevel);
            int maxLodLevel = terSet.LODLevel - 1;
            for (int i = 0; i < buildClusterNum; i++)
            {
                SimplifyerClsIdxList.Add(clusterIdxList[i], i);
                for (int j = 0; j < tileNum * tileNum; j++)
                {
                    // max lod level do not need simplify
                    for (int lodLevel = 0; lodLevel < maxLodLevel; lodLevel++)
                    {
                        SimplifierList.Add(new TerrainSimplifier());
                    }
                }
            }
            //Debug.Log($"simplier list count : {SimplifierList.Count}");
        }

        private void SetSimplifyTargetCnt()
        {
            // Reset reduced cnt firstly
            foreach (var simplifier in SimplifierList)
            {
                simplifier.ResetRecorder();
            }

            int curVertNum = 0;
            int targetVertNum = 0;
            foreach (var idx in clusterIdxList)
            {
                int i = idx.x, j = idx.y;
                curVertNum += TerrainCtor.GetClusterCurVertNum(i, j);
                targetVertNum += TerrainCtor.GetTargetSimplifyVertNum(i, j);
            }
            TotalSimplifyTargetCnt = curVertNum - targetVertNum;
        }

        private float SetSimplifyProgressCall()
        {
            int reducedVertNum = GetSimplifedVertNum();
            float progress = (float)reducedVertNum / (float)TotalSimplifyTargetCnt;
            if (progress > 0.9f)
            {
                // Handle dead circle
                if (IsAllSimplifedOver())
                {

                    return TaskNode.TerminalProgress;
                }
            }
            //Debug.Log($"call simplify progress : {progress}, reduced : {reducedVertNum}, total target : {TotalSimplifyTargetCnt}");
            return progress;
        }

        private int GetSimplifedVertNum()
        {
            int reducedVertNum = 0;
            foreach (var simplifier in SimplifierList)
            {
                reducedVertNum += simplifier.GetReducedCnt();
            }
            return reducedVertNum;
        }

        private bool IsAllSimplifedOver()
        {
            foreach (var simplifier in SimplifierList)
            {
                if (!simplifier.GetSimplifyOver())
                {
                    return false;
                }
            }
            return true;
        }
        
        private TerrainSimplifier GetSimplifyer(int clsX, int clsY, int tileX, int tileY, int lodLevel)
        {
            Vector2Int clusterIdx = new Vector2Int(clsX, clsY);
            int tileNum = terSet.GetTileNumClsPerLine();
            int startIdx = SimplifyerClsIdxList[clusterIdx] * tileNum * tileNum;
            int offset = (tileX * tileNum + tileY) * (terSet.LODLevel - 1) + lodLevel;

            //DebugUtility.Log($"call GetSimplifyer, clsX : {clsX}, clsY : {clsY}, tileX : {tileX}, tileY : {tileY}, lodLevel : {lodLevel}, startIdx : {startIdx}, offset : {offset}, idx : {startIdx+offset}", DebugPriority.High);

            return SimplifierList[startIdx + offset];
        }

        public override async void StartTask(int taskID)
        {
            base.StartTask(taskID);
            foreach (var idx in clusterIdxList)
            {
                int i = idx.x; int j = idx.y;
                if (useForRuntime)
                {
                    await TerrainCtor.BuildCluster_TerMesh(i, j, tokenSrc.Token);
                }
                else
                {
                    TerrainCtor.BuildCluster_OnlyMaxLOD(i, j);
                }
                GoNextChildTask();
                await TerrainCtor.BuildCluster_River(i, j, tokenSrc.Token);
                GoNextChildTask();
            }

            if (shouldGenLODBySimplify)
            {
                InitSimplifyList();
                SetSimplifyTargetCnt();

                foreach (var idx in clusterIdxList)
                {
                    int i = idx.x; int j = idx.y;
                    await TerrainCtor.ExeSimplify_MT(i, j, GetSimplifyer, tokenSrc.Token);
                    GoNextChildTask();

                    Debug.Log($"simplify cluster over : {i}, {j}");
                }
            }
        }

        public override void EndTask(bool abortFlag)
        {
            base.EndTask(abortFlag);
            tokenSrc.Cancel();
        }

        private void BuildOverCallBack(long costTime, bool abortFlag)
        {
            foreach (var idx in clusterIdxList)
            {
                int i = idx.x; int j = idx.y;
                TerrainCtor.BuildOriginMesh(i, j);
            }

            TerrainCtor.SetTerrainGened();

            if (abortFlag)
            {
                TerrainCtor.ClearClusterObj();
                DebugUtility.LogError($"build canceled, build {clusterIdxList.Count} clusters, cost {costTime} ms");
            }
            else
            {
                Debug.Log($"build over, build {clusterIdxList.Count} clusters, cost {costTime} ms");
            }
        }

        public override void SyncTask(string taskName, float progress)
        {
            base.SyncTask(taskName, progress);
        }

    }
}
