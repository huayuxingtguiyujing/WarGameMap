
using UnityEngine;
using UnityEditor;
using System.IO;

namespace LZ.WarGameMap.Runtime
{

    // 来源：https://zhuanlan.zhihu.com/p/625411409
    // 本文以Unity引擎为例，介绍一下对地表混合方案的探索
    public class ExportTerrainTexture_Export : EditorWindow {

        private Terrain _terrain;

        int _firstTexIndex = 0;
        int _secondTexIndex = 0;
        int _thirdTexIndex = 0;
        int _firstTexIndexTemp = 0;

        float _firstTexBlend = 0f;
        float _secondTexBlend = 0f;
        float _thirdTexBlend = 0f;

        int first = 1;
        int secondFrom = 2;
        int secondTo = 9;
        int thirdFrom = 10;
        int thirdTo = 16;

        int planeSize = 10;
        int planeUV = 10;

        Texture2D diffuseAtlas;
        Texture2D indexTex;
        Texture2D indexTexOriginal;
        Texture2D blendTex;


        const string DIRECTORYNAME = "Assets/WGame/HotFixResources/Textures/MapHighLevel/TerrainAtlas/";
        string _directoryName = "";

        const int baseResolution = 2048;

        //[MenuItem("GameMap/地形贴图图集/正常导出")]
        public static void ShowWindow() {
            EditorWindow.GetWindowWithRect((typeof(ExportTerrainTexture_Export)), new Rect(0, 0, 600, 500));
        }


        void OnGUI() {
            GenericMenu genericMenu = new GenericMenu();
            _terrain = (Terrain)EditorGUILayout.ObjectField("选择地形", _terrain, typeof(Terrain), true);
            GUILayout.Space(20);

            if (_terrain != null) {
                GUILayout.Label("文件保存路径:" + DIRECTORYNAME);
                _directoryName = GUILayout.TextField(_directoryName);
                GUILayout.Space(10);

                GUILayout.Label("第一层打底地表图集选择", EditorStyles.boldLabel);
                first = EditorGUILayout.IntField(first);
                GUILayout.Space(10);

                GUILayout.Label("第二层地表图集选择区间", EditorStyles.boldLabel);
                secondFrom = EditorGUILayout.IntField(secondFrom);
                secondTo = EditorGUILayout.IntField(secondTo);
                GUILayout.Space(10);

                GUILayout.Label("第三层地表图集选择区间", EditorStyles.boldLabel);
                thirdFrom = EditorGUILayout.IntField(thirdFrom);
                thirdTo = EditorGUILayout.IntField(thirdTo);
                GUILayout.Space(10);

                GUILayout.Label("检查尺寸UV值", EditorStyles.boldLabel);
                planeUV = EditorGUILayout.IntField(planeUV);
                GUILayout.Space(10);

                if (GUILayout.Button("检查")) {
                    CheckEachSplatLevel();
                }
                GUILayout.Space(10);
                if (GUILayout.Button("导出Diffuse和Normal")) {
                    ExportBaseTexture();
                }
                GUILayout.Space(10);
                if (GUILayout.Button("导出索引和权重图")) {
                    ExportIndexAndBlendTexture();
                }
            }
        }

        // 此函数应该是组成地形纹理的图集
        void ExportBaseTexture() {
            TerrainData terrainData = _terrain.terrainData;
            // 获取到 Terrain 的混合贴图，但我不太了解这个结构到底是啥
            SplatPrototype[] terrainDataSplats = terrainData.splatPrototypes;

            // NOTE : gpt告诉我 renderTexture 的操作都在 GPU上完成，所以效率会更高些
            RenderTexture[] rtArray = new RenderTexture[terrainDataSplats.Length];
            int texSize = baseResolution / 4;
            Texture2D[] diffuseArray = new Texture2D[terrainDataSplats.Length];
            Texture2D[] normalArray = new Texture2D[terrainDataSplats.Length];

            // 拿 shader 和 material 的资源
            Shader defaultShader = Shader.Find("Unlit/Texture");
            Material defaultMaterial = new Material(defaultShader);

            string directoryPath = DIRECTORYNAME + _directoryName + "/";

            for (int i = 0; i < terrainDataSplats.Length; i++) {
                // https://docs.unity.cn/cn/2019.4/ScriptReference/RenderTexture.GetTemporary.html
                // 分配临时的 renderTexture
                rtArray[i] = RenderTexture.GetTemporary(texSize, texSize, 24);
                diffuseArray[i] = new Texture2D(texSize, texSize, TextureFormat.RGB24, false);
                normalArray[i] = new Texture2D(texSize, texSize, TextureFormat.RGB24, false);

                // 使用着色器将源纹理复制到目标渲染纹理 （此处 dst 是 rtArray[i]）
                Graphics.Blit(terrainDataSplats[i].texture, rtArray[i], defaultMaterial, 0);
                RenderTexture.active = rtArray[i];
                diffuseArray[i].ReadPixels(new Rect(0f, 0f, (float)texSize, (float)texSize), 0, 0);

                // 为什么要再取一次 normal map 的数据
                if (terrainDataSplats[i].normalMap != null) {
                    Graphics.Blit(terrainDataSplats[i].normalMap, rtArray[i], defaultMaterial, 0);
                    RenderTexture.active = rtArray[i];
                    normalArray[i].ReadPixels(new Rect(0f, 0f, (float)texSize, (float)texSize), 0, 0);
                }
            }

            // 把图集信息读取过来形成的 大号的纹理图集
            diffuseAtlas = new Texture2D(baseResolution, baseResolution, TextureFormat.RGB24, false);
            //Texture2D normalAtlas = new Texture2D(BasemapResolution, BasemapResolution, TextureFormat.RGB24, false);

            // 4 * 4 的图集，可以以此取到每行/列的 index
            for (int i = 0; i < terrainDataSplats.Length; i++) {
                int controlColumn = i % 4;
                int controlRow = (i % 16) / 4;
                int startWidth = controlColumn * texSize;
                int startHeight = controlRow * texSize;

                for (int j = 0; j < texSize; j++) {
                    for (int k = 0; k < texSize; k++) {
                        Color diffuseColor = GetPixelColor(j, k, diffuseArray[i]);
                        diffuseAtlas.SetPixel(j + startWidth, k + startHeight, diffuseColor);

                        //Color normalColor = (terrainDataSplats[i].normalMap == null)?new Color(0.5f,0.5f,1): GetPixelColor(j, k, normalArray[i]);
                        //normalAtlas.SetPixel(j + startWidth, k + startHeight, normalColor);
                    }
                }

                // diffuseAtlas 才是最终存储地形纹理图集的地方
                diffuseAtlas.Apply();
                byte[] bytes = diffuseAtlas.EncodeToPNG();

                if (!Directory.Exists(directoryPath)) {
                    Directory.CreateDirectory(directoryPath);
                }
                File.WriteAllBytes(directoryPath + "TerrainDiffuse_D.png", bytes);
                //normalAtlas.Apply();
                //byte[] normalBytes = normalAtlas.EncodeToPNG();
                //if (!Directory.Exists(directoryPath))
                //{
                // Directory.CreateDirectory(directoryPath);
                //}
                //File.WriteAllBytes(directoryPath + "TerrainNormal.png", normalBytes);
            }

            // 导出作为 unity 资产
            AssetDatabase.ImportAsset(directoryPath + "TerrainDiffuse_D.png");
            Debug.Log("导出diffuseTexture,normalTexture");
        }

        // 输出索引和混合因子贴图
        void ExportIndexAndBlendTexture() {
            TerrainData terrainData = _terrain.terrainData;
            SplatPrototype[] terrainDataSplats = terrainData.splatPrototypes;
            int slpatNums = terrainDataSplats.Length;//slpats数目

            Texture2D[] alphaTextures = terrainData.alphamapTextures;//alphamaps
            int alphaWidth = alphaTextures[0].width;//alphawidth
            int alphaHeight = alphaTextures[0].height;//alphaheight

            // 存储索引的 texture
            indexTex = new Texture2D(alphaWidth, alphaHeight, TextureFormat.RGB24, false, true);
            indexTex.filterMode = FilterMode.Point;
            Color indexColor = new Color(0, 0, 0, 0);

            indexTexOriginal = new Texture2D(alphaWidth, alphaHeight, TextureFormat.RGB24, false, true);
            indexTex.filterMode = FilterMode.Point;

            // 存储 混合权重的texture
            blendTex = new Texture2D(alphaWidth, alphaHeight, TextureFormat.RGB24, false, true);
            blendTex.filterMode = FilterMode.Bilinear;
            Color blendColor = new Color(0, 0, 0, 0);

            for (int j = 0; j < alphaWidth; j++) {
                for (int k = 0; k < alphaHeight; k++) {
                    ResetNumAndBlend();
                    for (int i = 0; i < slpatNums; i++) {
                        int controlRow = (i % 16) / 4;
                        int controlColumn = i % 4;
                        Color alphaColor = alphaTextures[controlRow].GetPixel(j, k);
                        switch (controlColumn) {
                            case 0:
                                CalculateIndex(i, alphaColor.r);
                                break;
                            case 1:
                                CalculateIndex(i, alphaColor.g);
                                break;
                            case 2:
                                CalculateIndex(i, alphaColor.b);
                                break;
                            case 3:
                                CalculateIndex(i, alphaColor.a);
                                break;
                            default:
                                break;
                        }
                    }

                    indexColor.r = _firstTexIndex / 15f;
                    indexColor.g = _secondTexIndex / 15f;
                    indexColor.b = _thirdTexIndex / 15f;
                    indexTex.SetPixel(j, k, indexColor);
                    indexTexOriginal.SetPixel(j, k, indexColor);

                    blendColor.r = _firstTexBlend;
                    blendColor.g = _secondTexBlend;
                    blendColor.b = _thirdTexBlend;
                    blendTex.SetPixel(j, k, blendColor);
                }
            }
            
            string directoryPath = DIRECTORYNAME + _directoryName + "/";
            if (!Directory.Exists(directoryPath)) {
                Directory.CreateDirectory(directoryPath);
            }

            // 不知道这个函数有什么用
            FillIndexEdge(alphaWidth, alphaHeight);
            indexTex.Apply();
            indexTex.filterMode = FilterMode.Point;

            // write index textrue to texture file 
            byte[] indexBytes = indexTex.EncodeToPNG();
            File.WriteAllBytes(directoryPath + "TerrainAtlasIndexTex_I.png", indexBytes);
            AssetDatabase.ImportAsset(directoryPath + "TerrainAtlasIndexTex_I.png");

            blendTex.Apply();
            byte[] blendBytes = blendTex.EncodeToPNG();
            File.WriteAllBytes(directoryPath + "TerrainAtlasBlendTex_B.png", blendBytes);
            AssetDatabase.ImportAsset(directoryPath + "TerrainAtlasBlendTex_B.png");

            Debug.Log("导出IndexTexture,BlendTexture");
        }

        private void ResetNumAndBlend() {
            _firstTexIndex = 0;
            _secondTexIndex = 0;
            _thirdTexIndex = 0;
            _firstTexBlend = 0f;
            _secondTexBlend = 0f;
            _thirdTexBlend = 0f;
        }

        private void CalculateIndex(int currentIndex, float currentBlend) {
            //存打底第一张图
            if (currentIndex == first - 1) {
                _firstTexBlend = currentBlend;
                _firstTexIndex = currentIndex;
                _firstTexIndexTemp = currentIndex;
            } else {
                if (currentBlend > 0f && currentIndex >= (secondFrom - 1) && currentIndex <= (secondTo - 1)) {
                    _secondTexBlend = currentBlend;
                    _secondTexIndex = currentIndex;
                }
                if (currentBlend > 0f && currentIndex >= (thirdFrom - 1) && currentIndex <= (thirdTo - 1)) {
                    _thirdTexBlend = currentBlend;
                    _thirdTexIndex = currentIndex;
                }
            }
        }



        Color GetPixelColor(int rowNum, int columnNum, Texture2D oriTex)//拉边几个像素
        {
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

              //正常采样
              else {
                oriColor = oriTex.GetPixel(rowNum, columnNum);
            }
            return oriColor;
        }
        
        void CheckEachSplatLevel() {
            ExportBaseTexture();
            ExportIndexAndBlendTexture();
            initSetting();
        }
        
        void initSetting() {
            GameObject pl = GameObject.Find("TestPlane");
            // 貌似不是自带的？
            Shader atlasShader = Shader.Find("Modules/TerrainSystem/TerrainBakedBlendAtlas");
            Material plMat = new Material(atlasShader);

            string diffusepath = DIRECTORYNAME + _directoryName + "/" + "TerrainDiffuse_D.png";
            string indexPath = DIRECTORYNAME + _directoryName + "/" + "TerrainAtlasIndexTex_I.png";
            string blendPath = DIRECTORYNAME + _directoryName + "/" + "TerrainAtlasBlendTex_B.png";
            Texture2D d = (Texture2D)AssetDatabase.LoadAssetAtPath(diffusepath, typeof(Texture2D));
            Texture2D i = (Texture2D)AssetDatabase.LoadAssetAtPath(indexPath, typeof(Texture2D));
            Texture2D b = (Texture2D)AssetDatabase.LoadAssetAtPath(blendPath, typeof(Texture2D));

            if (pl == null) {
                pl = GameObject.CreatePrimitive(PrimitiveType.Plane);
                pl.name = "TestPlane";
                pl.transform.position = new Vector3(0, 1, 0);
                pl.transform.localScale = new Vector3(planeSize, 1, planeSize);
                pl.transform.rotation = new Quaternion(0, 180, 0, 0);

                pl.GetComponent<MeshRenderer>().material = plMat;
                plMat.SetFloat("useObjectUV", 1f);
                plMat.SetFloat("_UVTiling", planeUV);
                plMat.SetTexture("_MainTex", d);
                plMat.SetTexture("_IndexTex", i);
                plMat.SetTexture("_BlendTex", b);
            } else {
                plMat = pl.GetComponent<MeshRenderer>().sharedMaterial;
                plMat.SetFloat("_UVTiling", planeUV);
                plMat.SetFloat("useObjectUV", 1f);
                plMat.SetTexture("_MainTex", d);
                plMat.SetTexture("_IndexTex", i);
                plMat.SetTexture("_BlendTex", b);
                pl.GetComponent<MeshRenderer>().material = plMat;
            }
        }

        void FillIndexEdge(int alphaWidth, int alphaHeight) {
            float firstIndex = _firstTexIndexTemp / 15f;
            Color indexColorTemp = new Color(0, 0, 0, 0);
            indexColorTemp.r = firstIndex;

            for (int j = 0; j < alphaWidth; j++) {
                for (int k = 0; k < alphaHeight; k++) {
                    Color alphaColor = indexTexOriginal.GetPixel(j, k);
                    float g = alphaColor.g;
                    float b = alphaColor.b;
                    indexColorTemp.g = g;
                    indexColorTemp.b = b;

                    if (j - 1 >= 0) {
                        float g1 = indexTexOriginal.GetPixel(j - 1, k).g;
                        float b1 = indexTexOriginal.GetPixel(j - 1, k).b;
                        if (g != firstIndex && g1 == firstIndex) {
                            indexTex.SetPixel(j - 1, k, indexColorTemp);
                        }
                        if (b != firstIndex && b1 == firstIndex) {
                            indexTex.SetPixel(j - 1, k, indexColorTemp);
                        }
                    }

                    if (k - 1 >= 0) {
                        float g2 = indexTexOriginal.GetPixel(j, k - 1).g;
                        float b2 = indexTexOriginal.GetPixel(j, k - 1).b;
                        if (g != firstIndex && g2 == firstIndex) {
                            indexTex.SetPixel(j, k - 1, indexColorTemp);
                        }
                        if (b != firstIndex && b2 == firstIndex) {
                            indexTex.SetPixel(j, k - 1, indexColorTemp);
                        }
                    }

                    if (j + 1 < alphaWidth) {
                        float g3 = indexTexOriginal.GetPixel(j + 1, k).g;
                        float b3 = indexTexOriginal.GetPixel(j + 1, k).b;
                        if (g != firstIndex && g3 == firstIndex) {
                            indexTex.SetPixel(j + 1, k, indexColorTemp);
                        }
                        if (b != firstIndex && b3 == firstIndex) {
                            indexTex.SetPixel(j + 1, k, indexColorTemp);
                        }
                    }

                    if (k + 1 < alphaHeight) {
                        float g4 = indexTexOriginal.GetPixel(j, k + 1).g;
                        float b4 = indexTexOriginal.GetPixel(j, k + 1).b;
                        if (g != firstIndex && g4 == firstIndex) {
                            indexTex.SetPixel(j, k + 1, indexColorTemp);
                        }
                        if (b != firstIndex && b4 == firstIndex) {
                            indexTex.SetPixel(j, k + 1, indexColorTemp);
                        }
                    }
                }
            }
        }

    }

}
