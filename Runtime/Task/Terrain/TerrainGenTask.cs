using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEditor.Experimental.GraphView;
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

        // simplier list, keep the ref
        Dictionary<Vector2Int, int> SimplifyerClsIdxList = new Dictionary<Vector2Int, int>();
        List<TerrainSimplifier> SimplifierList;
        int TotalSimplifyTargetCnt = 0;

        // to cancel terrain gen process
        CancellationTokenSource tokenSrc;

        public TerrainGenTask(List<HeightDataModel> heightDataModels, TerrainSettingSO terSet, TerrainConstructor TerrainCtor, List<Vector2Int> clusterIdxList, bool shouldGenRiver, bool shouldGenLODBySimplify) : base(null, -1, true)
        {
            this.buildClusterNum = clusterIdxList.Count;
            this.heightDataModels = heightDataModels;
            this.terSet = terSet;
            this.TerrainCtor = TerrainCtor;
            this.clusterIdxList = clusterIdxList;
            this.shouldGenRiver = shouldGenRiver;
            this.shouldGenLODBySimplify = shouldGenLODBySimplify;

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
                foreach (var model in heightDataModels)
                {
                    if (!model.ExistHeightData(longitude, latitude))
                    {
                        throw new Exception($"unable to find heightdata, longitude : {longitude}, latitude : {latitude}, so build not valid");
                    }
                }
            }
        }

        // TODO : 有bug！
        private void InitTerGenChildTask()
        {
            float buildMeshRiverWeight = 0.2f / buildClusterNum;
            TaskNode terGenMeshRiverNode = new TaskNode(TerGenClsTaskName, buildMeshRiverWeight, null, "");
            for (int i = 0; i < buildClusterNum; i++)
            {
                terGenMeshRiverNode.AddChildTask($"{TerGenClsTaskName}_{i}_{TerGenClsGenMeshName}", buildMeshRiverWeight / 2, null, "生成地形Mesh中");
                terGenMeshRiverNode.AddChildTask($"{TerGenClsTaskName}_{i}_{TerGenClsGenRiverName}", buildMeshRiverWeight / 2, null, "生成河流中");
                
            }
            AddChildTask(terGenMeshRiverNode);

            float simplifyWeight = 0.8f / buildClusterNum;
            TaskNode simplifyNode = new TaskNode(TerSimplifyTaskName, simplifyWeight, SetSimplifyProgressCall, "");
            for (int i = 0; i < buildClusterNum; i++)
            {
                simplifyNode.AddChildTask($"{TerGenClsTaskName}_{i}_{TerGenClsSimplifyName}", simplifyWeight, SetSimplifyProgressCall, $"LOD减面中");
            }
            AddChildTask(simplifyNode);
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
            int curVertNum = 0;
            int targetVertNum = 0;
            foreach (var idx in clusterIdxList)
            {
                int i = idx.x; int j = idx.y;
                curVertNum += TerrainCtor.GetClusterCurVertNum(i, j);
                targetVertNum += TerrainCtor.GetTargetSimplifyVertNum(i, j);
            }
            TotalSimplifyTargetCnt = curVertNum - targetVertNum;
            //Debug.Log($"target : {TotalSimplifyTargetCnt}");
        }

        private float SetSimplifyProgressCall()
        {
            int reducedVertNum = GetSimplifedVertNum();
            float progress = (float)reducedVertNum / (float)TotalSimplifyTargetCnt;
            if (progress > 0.8f)
            {
                // avoid dead circle
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
                await TerrainCtor.BuildCluster_TerMesh(i, j, shouldGenLODBySimplify, tokenSrc.Token);
                GoNextChildTask(2);
                await TerrainCtor.BuildCluster_River(i, j, tokenSrc.Token);
                GoNextChildTask(2);
            }

            InitSimplifyList();
            SetSimplifyTargetCnt();

            foreach (var idx in clusterIdxList)
            {
                int i = idx.x; int j = idx.y;
                await TerrainCtor.ExeSimplify_MT(i, j, GetSimplifyer, tokenSrc.Token);

                // TODO : 多个 cluster 的时候也许会有问题！

                GoNextChildTask(2);
                Debug.Log($"simplify cluster over : {i}, {j}");
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

            if (abortFlag)
            {
                TerrainCtor.ClearClusterObj();
                DebugUtility.LogError($"build canceled, build {clusterIdxList.Count} clusters, cost {costTime} ms");
            }
            else
            {
                TerrainCtor.UpdateTerrain();
                Debug.Log($"build over, build {clusterIdxList.Count} clusters, cost {costTime} ms");
            }
        }

        public override void SyncTask(string taskName, float progress)
        {
            base.SyncTask(taskName, progress);
        }

    }
}
