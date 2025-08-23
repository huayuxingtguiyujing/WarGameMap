using System;
using System.IO;
using Unity.Collections;
using Unity.Jobs;
using UnityEditor;
using UnityEngine;

namespace LZ.WarGameMap.MapEditor {

    internal static class EditorMapEnum {
        public const string EditorRoot = "editorRoot";

        public const string EditorTexs = "textures";

        public const string EdtiorOthers = "others";

    }

    internal struct FillPixelJob : IJobParallelFor {
        [WriteOnly]
        public NativeArray<Color> Pixels;

        public Color brushColor;

        public void Execute(int index) {
            Pixels[index] = brushColor;
        }
    }

    internal struct BrushPixelJob : IJobParallelFor {

        public Vector2 brushCenter;

        public int brushScope;

        public int textureWidth;

        public int textureHeight;

        public Color brushColor;

        [WriteOnly]
        public NativeArray<Color> Pixels;

        public void Execute(int index) {
            // TODO: 实现完它；可不可以只处理局部的纹理？
        }
    }

    [InitializeOnLoad]
    public class MapEditorBase : EditorWindow
    {

        private static MapEditorBase Instance;

        //[MenuItem("GameMap/ShowEditor")]
        static void ShowWindow() {
            MapEditorBase window = GetWindow<MapEditorBase>("MapEditor");
            window.minSize = new Vector2(720, 320);
            window.Show();
            Instance = window;
        }

        private GameObject editorRoot;
        private GameObject editorTexts;
        private GameObject editorOthers;

        public string saveTargetPath = "Assets/Texture/";
        public string textureName = "testTexture.asset";

        public GameObject textureObj;
        public SpriteRenderer textureRender;
        public Texture2D texture;

        private GridMatCtrl gridMatCtrl;

        private int textureWidth = 500;
        private int textureHeight = 500;

        private float lineWidth = 0.1f;
        private Color lineColor = Color.black;
        private bool enableBrush = false;
        private Color brushColor = Color.white;

        private void OnEnable() {
            InitEditorCfg();

            if (textureObj == null) {
                textureObj = new GameObject(textureName);
                textureObj.transform.parent = editorTexts.transform;
                textureRender = textureObj.AddComponent<SpriteRenderer>();
            }
            if(texture == null) {
                //texture = CreateTexture();
            }

            SceneView.duringSceneGui += OnSceneGUI;
        }

        private void InitEditorCfg() {
            // init edit in hierachy (for placing some objs)
            editorRoot = GameObject.Find(EditorMapEnum.EditorRoot);
            if (editorRoot == null) {
                editorRoot = new GameObject(EditorMapEnum.EditorRoot);
            }

            editorTexts = GameObject.Find(EditorMapEnum.EditorTexs);
            if (editorTexts == null) {
                editorTexts = new GameObject(EditorMapEnum.EditorTexs);
            }
            editorTexts.transform.parent = editorRoot.transform;

            editorOthers = GameObject.Find(EditorMapEnum.EdtiorOthers);
            if (editorOthers == null) {
                editorOthers = new GameObject(EditorMapEnum.EdtiorOthers);
            }
            editorOthers.transform.parent = editorRoot.transform;

        }

        private void OnDisable() {
            SceneView.duringSceneGui -= OnSceneGUI;

            DestroyImmediate(textureObj);
            DestroyImmediate(editorRoot);
        }


        private void OnGUI() {
            GUILayout.Label("存储设置", EditorStyles.boldLabel);
            saveTargetPath = EditorGUILayout.TextField("Save Path:", saveTargetPath);
            textureName = EditorGUILayout.TextField("Texture Name:", textureName);

            GUILayout.Label("纹理设置", EditorStyles.boldLabel);
            textureObj = (GameObject)EditorGUILayout.ObjectField("TextureObj: ", textureObj, typeof(GameObject), true);
            texture = (Texture2D)EditorGUILayout.ObjectField("Texture: ", texture, typeof(Texture2D), false);
            textureWidth = EditorGUILayout.IntField("Texture Width:", textureWidth);
            textureHeight = EditorGUILayout.IntField("Texture Height:", textureHeight);

            // 分组布局框
            EditorGUILayout.BeginVertical("box");
            GUILayout.Label("笔刷设置", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("NOTE: 请手动设置material和shader到spriteRender上", EditorStyles.boldLabel);
            lineWidth = EditorGUILayout.FloatField("Line Width:", lineWidth);
            lineColor = EditorGUILayout.ColorField("Line Color:", lineColor);
            enableBrush = EditorGUILayout.Toggle("Enable Brush:", enableBrush);
            brushColor = EditorGUILayout.ColorField("Brush Color:", brushColor);
            if (GUILayout.Button("refresh")) {
                RefreshGridView();
            }
            EditorGUILayout.EndVertical();

            // 设置栅格化的material
            //EditorGUILayout.BeginVertical("box");
            //GUILayout.Label("Grid View Settings", EditorStyles.boldLabel);
            //lineWidth = EditorGUILayout.FloatField("Brush Line Width:", lineWidth);
            //enableBrush = EditorGUILayout.Toggle("Enable Brush:", enableBrush);
            //brushColor = EditorGUILayout.ColorField("Brush Color:", brushColor);
            //EditorGUILayout.EndVertical();


            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Save Texture")) {
                SaveTexture();
            }
            if (GUILayout.Button("Load Texture")) {
                LoadTexture();
                textureRender.sprite = Sprite.Create(
                    texture,
                    new Rect(0, 0, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f), 
                    100f, 0u, SpriteMeshType.FullRect
                );
                textureRender.drawMode = SpriteDrawMode.Sliced;
                textureRender.size = new Vector2(texture.width, texture.height);
                gridMatCtrl = new GridMatCtrl(texture);
            }
            EditorGUILayout.EndHorizontal();


            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Create Texture")) {
                texture = CreateTexture(textureWidth, textureHeight);
                textureRender.sprite = Sprite.Create(
                    texture,
                    new Rect(0, 0, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f),
                    100f, 0u, SpriteMeshType.FullRect
                );
                textureRender.drawMode = SpriteDrawMode.Sliced;
                textureRender.size = new Vector2(texture.width, texture.height);
                gridMatCtrl = new GridMatCtrl(texture);
            }
            if (GUILayout.Button("Destroy Texture")) {
                DestroyTexture();
            }
            EditorGUILayout.EndHorizontal();

        }

        private static void OnSceneGUI(SceneView sceneView) {
            Event e = Event.current;
            var eventType = Event.current.type;
            if (e.button == 0) {
                var controlID = GUIUtility.GetControlID(FocusType.Passive);
                GUIUtility.hotControl = controlID;
                //var eventType = Event.current.GetTypeForControl(controlID);
                switch (eventType) {
                    case EventType.MouseDrag:
                        Instance.OnMouseDrag(e);
                        break;
                    case EventType.MouseUp:
                        Instance.OnMouseUp();
                        break;
                    case EventType.MouseDown:
                        Instance.OnMouseDown();
                        break;
                    case EventType.MouseMove:
                        Instance.OnMouseMove(e);
                        break;
                }

                GUIUtility.hotControl = 0;
            }

        }

        protected virtual void OnMouseUp() {
        }

        protected virtual void OnMouseDown() {
        }

        protected virtual void OnMouseMove(Event e) {
            UpdateMousePos(e);
        }

        protected virtual void OnMouseDrag(Event e) {
            if (gridMatCtrl == null || !gridMatCtrl.initFlag) {
                return;
            }
            Vector2 mousePosition = e.mousePosition;


        }

        private void UpdateMousePos(Event e) {
            if (gridMatCtrl == null || !gridMatCtrl.initFlag) {
                return;
            }

            Vector2 mousePosition = e.mousePosition;
            Ray ray = HandleUtility.GUIPointToWorldRay(mousePosition);
            if (ray.direction.z != 0) {
                float t = -ray.origin.z / ray.direction.z;
                Vector3 worldPosition = ray.origin + t * ray.direction;
                gridMatCtrl.SetMousePos(worldPosition);
            }
        }

        #region texture handle

        private Texture2D CreateTexture(int width, int height, TextureFormat format = TextureFormat.RGBA32) {
            if (width == 0 || height == 0) {
                Debug.LogError("you can not create texture with 0 w/h");
                return null;
            }
            Texture2D texture2D = new Texture2D(width, height, format, false);


            NativeArray<Color> pixels = new NativeArray<Color>(width * height, Allocator.TempJob);
            FillPixelJob fillPixelsJob = new FillPixelJob {
                Pixels = pixels,
                brushColor = Color.white
            };
            JobHandle jobHandle = fillPixelsJob.Schedule(pixels.Length, 64);
            jobHandle.Complete();

            texture2D.SetPixels(pixels.ToArray());
            texture2D.Apply();

            return texture2D;
        }

        private void DestroyTexture() {
            DestroyImmediate(texture);
        }

        private void SaveTexture() {
            if (texture == null) {
                return;
            }

            SaveAssets<Texture2D>(textureName, texture);
        }

        private void LoadTexture() {
            texture = LoadAssets<Texture2D>(textureName);
        }

        // apply grid view to this tetxure (use material)
        private void RefreshGridView() {
            if(gridMatCtrl == null) {
                Debug.LogError("未创建栅格化控制器!");
                return;
            }
            // NOTE: 这里使用的是sharedMaterial, 修改后会影响到全局使用到mat的对象
            gridMatCtrl.SetCtrl(textureRender.sharedMaterial, textureRender.sharedMaterial.shader, textureRender);
            gridMatCtrl.SetGridCfg(lineWidth, lineColor);

            gridMatCtrl.SetHoverEffect(enableBrush);
            gridMatCtrl.ApplyCfg();

            SceneView.RepaintAll();
            EditorUtility.SetDirty(textureObj);
        }

        #endregion

        #region asset handle

        private void SaveAssets<T>(string assetName, T asset) where T : UnityEngine.Object {
            if (assetName == null) {
                return;
            }

            string path = saveTargetPath;
            string objPath = path + assetName;
            string dir = Path.GetDirectoryName(path);
            if (!Directory.Exists(dir)) {
                Directory.CreateDirectory(dir);
            }

            UnityEngine.Object existingAsset = AssetDatabase.LoadAssetAtPath<T>(objPath);
            if (existingAsset != null) {
                EditorUtility.SetDirty(asset);
            } else {
                AssetDatabase.CreateAsset(asset, objPath);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private T LoadAssets<T>(string assetName) where T : UnityEngine.Object {
            if (assetName == null) {
                return null;
            }

            string path = saveTargetPath;
            string objPath = path + assetName;
            string dir = Path.GetDirectoryName(path);
            if (!Directory.Exists(dir)) {
                return null;
            }

            return AssetDatabase.LoadAssetAtPath<T>(objPath);
        }


        #endregion
    }

    // Material Controller 基类
    [Serializable]
    public abstract class BaseMaterialCtrl {

        protected SpriteRenderer spriteRenderer;
        protected Material material;
        protected Shader shader;

        protected MaterialPropertyBlock propertyBlock;

        public BaseMaterialCtrl() {
            propertyBlock = new MaterialPropertyBlock();
        }

        public abstract void SetCtrl(Material material, Shader shader, SpriteRenderer spriteRenderer);

        public abstract void ApplyCfg();

    }

    // 用于控制 偏移四边形栅格化的 Material Controller
    [Serializable]
    public class GridMatCtrl : BaseMaterialCtrl {
        public Texture2D texture;
        public float lineWidth;
        public Color32 lineColor;
        public int width;
        public int height;

        public bool initFlag = false;

        public GridMatCtrl(Texture2D texture) {
            this.texture = texture;
            width = texture.width;
            height = texture.height;
        }

        public override void SetCtrl(Material material, Shader shader, SpriteRenderer spriteRenderer) {
            this.material = material;
            this.shader = shader;
            this.spriteRenderer = spriteRenderer;
            initFlag = true;
        }
        
        public void SetGridCfg(float lineWidth, Color32 lineColor) {
            this.lineWidth = lineWidth;
            this.lineColor = lineColor;
        }

        public void SetHoverEffect(bool enableFlag) {
            material.SetFloat("_EnableHover", enableFlag ? 1.0f : 0.0f);
        }

        public void SetMousePos(Vector3 worldMousePos) {
            // NOTE: 此处可以实现鼠标悬停时让shader指定范围变色
            Vector2 leftDown = new Vector2(-(float)width/2, -(float)height/2);
            Vector2 uv = new Vector2( 
                (worldMousePos.x - leftDown.x) / width,
                (worldMousePos.y - leftDown.y) / height
            );
            material.SetVector("_MousePos", new Vector4(uv.x, uv.y, 0, 0));
            Debug.Log(worldMousePos);
        }

        // call this to apply change to shader!
        public override void ApplyCfg() {
            if(texture == null) {
                Debug.LogError("texture is null");
            }
            if (spriteRenderer == null) {
                Debug.LogError("spriteRenderer is null");
            }
            spriteRenderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetTexture("_MainTex", texture);
            propertyBlock.SetFloat("_LineWidth", lineWidth);
            propertyBlock.SetColor("_LineColor", lineColor);

            if (texture != null && material != null) {
                spriteRenderer.sharedMaterial.SetVector("_TexSize", new Vector2(width, height));
            }

            spriteRenderer.SetPropertyBlock(propertyBlock);
        }
    }

}
