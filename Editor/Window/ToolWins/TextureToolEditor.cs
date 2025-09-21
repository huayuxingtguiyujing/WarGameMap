using LZ.WarGameMap.Runtime;
using LZ.WarGameMap.Runtime.Enums;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using MeshUtility = LZ.WarGameMap.Runtime.MeshUtility;

namespace LZ.WarGameMap.MapEditor
{
    public class TextureToolEditor : BaseMapEditor {
        public override string EditorName => MapEditorEnum.TextureToolEditor;

        protected override void InitEditor() {
            base.InitEditor();
            TerrainCtor = EditorSceneManager.TerrainCtor;
        }

        #region 纹理图集生成

        [FoldoutGroup("地貌图集生成")]
        [LabelText("地貌纹理数目")]
        public int texListSize = 16;

        [FoldoutGroup("地貌图集生成")]
        [LabelText("纹理图集分辨率")]
        public int baseResolution = 2048;

        [FoldoutGroup("地貌图集生成")]
        [LabelText("地貌纹理集合")]
        public List<Texture2D> curTerrainTexList;

        [FoldoutGroup("地貌图集生成")]
        [LabelText("生成的纹理图集")]
        public Texture2D terrainTexSplat;

        [FoldoutGroup("地貌图集生成")]
        [LabelText("图集导出位置")]
        public string exportPath = MapStoreEnum.TerrainTexOutputPath;

        [FoldoutGroup("地貌图集生成")]
        [Button("纹理数据准备", ButtonSizes.Medium)]
        private void HandleTexData() {
            curTerrainTexList = new List<Texture2D>();
            for (int i = 0; i < texListSize; i++) {
                curTerrainTexList.Add(null);
            }
        }

        [FoldoutGroup("地貌图集生成")]
        [Button("生成地貌图集", ButtonSizes.Medium)]
        private void GenerateTexSplat() {
            if (terrainTexSplat != null) {
                Debug.LogError("already exist texture splat，please clean before generate");
                return;
            }
            
            int texSize = baseResolution / 4;
            terrainTexSplat = new Texture2D(baseResolution, baseResolution, TextureFormat.RGB24, false);

            // 4 * 4 的图集，可以以此取到每行/列的 index
            for (int i = 0; i < texListSize; i++) {
                int controlColumn = i % 4;
                int controlRow = (i % 16) / 4;
                int startWidth = controlColumn * texSize;
                int startHeight = controlRow * texSize;

                for (int j = 0; j < texSize; j++) {
                    for (int k = 0; k < texSize; k++) {
                        Color pixelColor = Color.blue;
                        if (curTerrainTexList[i] != null) {
                            pixelColor = GetPixelColor(j, k, curTerrainTexList[i]);
                        }
                        terrainTexSplat.SetPixel(j + startWidth, k + startHeight, pixelColor);
                        //Color normalColor = (terrainDataSplats[i].normalMap == null)?new Color(0.5f,0.5f,1): GetPixelColor(j, k, normalArray[i]);
                        //normalAtlas.SetPixel(j + startWidth, k + startHeight, normalColor);
                    }
                }
                terrainTexSplat.Apply();
            }
            Debug.LogError("texture splat generate successfully！");
        }

        private Color GetPixelColor(int rowNum, int columnNum, Texture2D oriTex) {
            //拉边几个像素
            Color oriColor;
            int minNum = baseResolution / 128 - 1;
            int maxNum = baseResolution / 4 - minNum + 1;

            //四个角
            if (rowNum <= minNum && columnNum <= minNum) {
                oriColor = oriTex.GetPixel(minNum, minNum);
            } else if (rowNum <= minNum && columnNum >= maxNum) {
                oriColor = oriTex.GetPixel(minNum, maxNum);
            } else if (rowNum >= maxNum && columnNum <= minNum) {
                oriColor = oriTex.GetPixel(maxNum, minNum);
            } else if (rowNum >= maxNum && columnNum >= maxNum) {
                oriColor = oriTex.GetPixel(maxNum, maxNum);
            }
            //四条边
            else if (rowNum <= minNum) {
                oriColor = oriTex.GetPixel(minNum, columnNum);
            } else if (rowNum >= maxNum) {
                oriColor = oriTex.GetPixel(maxNum, columnNum);
            } else if (columnNum <= minNum) {
                oriColor = oriTex.GetPixel(rowNum, minNum);
            } else if (columnNum >= maxNum) {
                oriColor = oriTex.GetPixel(rowNum, maxNum);
            }
            else {        //正常采样
                oriColor = oriTex.GetPixel(rowNum, columnNum);
            }
            return oriColor;
        }

        [FoldoutGroup("地貌图集生成")]
        [Button("保存地貌图集", ButtonSizes.Medium)]
        private void SaveSplatTexture() {
            if (terrainTexSplat == null) {
                Debug.LogError("terrainTexSplat is null!");
                return;
            }

            byte[] bytes = terrainTexSplat.EncodeToPNG();
            TextureUtility.GetInstance().SaveTextureAsAsset(exportPath, "TerrainSplat_AAA.png", terrainTexSplat);

        }

        #endregion

        #region 纹理混合测试

        [FoldoutGroup("纹理混合测试")]
        [LabelText("混合权重纹理")]
        public Texture2D blenderTex;

        [FoldoutGroup("纹理混合测试")]
        [LabelText("混合纹理图集")]
        public Texture2D blenderTexSplat;

        [FoldoutGroup("纹理混合测试")]
        [LabelText("生成的混合地貌纹理")]
        public Texture2D targetTerrainTex;

        [FoldoutGroup("纹理混合测试")]
        [Button("生成混合地貌纹理", ButtonSizes.Medium)]
        private void GenerateBlendTerrain() {
            // 生成多大的尺寸？ 和 混合纹理一样大吗？


        }

        [FoldoutGroup("地貌图集生成")]
        [Button("保存混合地貌纹理", ButtonSizes.Medium)]
        private void SaveBlendTerrainTexture() {
            if (targetTerrainTex == null) {
                Debug.Log("you do not generate blend terrain texture");
                return;
            }
            // TODO : save to asset path
        }

        #endregion

        // TODO : 这个功能还要吗？要不要保留呢
        #region 地形mesh处理

        TerrainConstructor TerrainCtor;

        [FoldoutGroup("地形网格持久化")]
        [LabelText("导出文件路径")]
        public string exportTerMeshPath = MapStoreEnum.TerrainMeshAssetPath;

        [FoldoutGroup("地形网格持久化")]
        [Button("存储地块0x0-0x0的mesh文件", ButtonSizes.Medium)]
        private void SaveMesh0_0() {
            if (TerrainCtor == null) {
                Debug.LogError("terrian ctor is null!");
                return;
            }

            Mesh mesh = TerrainCtor.GetTerTileMesh(5, 0, 0, 0, 0);
            if (mesh == null) {
                Debug.LogError("can not create, mesh is null!");
                return;
            }

            string meshName = "mesh_lod5_0x0_0x0.asset";
            AssetDatabase.CreateAsset(mesh, $"{exportTerMeshPath}/{meshName}");

            string txtName = "mesh_lod5_0x0_0x0.obj";
            string fullPath = AssetsUtility.AssetToFullPath($"{exportTerMeshPath}/{txtName}");
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
            File.WriteAllText(fullPath, MeshUtility.MeshToString(mesh, meshName));

            AssetDatabase.Refresh();
            Debug.Log($"create mesh and obj txt over!");
        }

        #endregion

        #region SDF纹理处理

        [FoldoutGroup("SDF纹理处理")]
        [LabelText("SDF提示")]
        [ReadOnly]
        public string sdfNote = "请到路径 : Utils/SDFGenSample.cs，该文件为 Mono，挂接到场景并配置即可生成 SDF 纹理";

        [FoldoutGroup("SDF纹理处理")]
        [LabelText("源纹理")]
        public Texture2D originTexture;

        //[FoldoutGroup("SDF纹理处理")]
        //[LabelText("SDF纹理")]
        //public Texture2D sdfTexture;

        //[FoldoutGroup("SDF纹理处理")]
        //[Button("生成SDF纹理", ButtonSizes.Medium)]
        //private void GetSDFTexture()
        //{
        //}

        //[FoldoutGroup("SDF纹理处理")]
        //[Button("保存SDF纹理", ButtonSizes.Medium)]
        //private void SaveSDFTexture()
        //{
        //}

        #endregion

    }
}
