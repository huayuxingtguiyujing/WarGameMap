using LZ.WarGameMap.Runtime;
using Sirenix.OdinInspector;
using UnityEngine;

namespace LZ.WarGameMap.MapEditor
{

    // 需要对scene进行brush，或需要查看scene中地图信息的，继承该类
    public abstract class BrushMapEditor : BaseMapEditor {

        [FoldoutGroup("配置scene", -10)]
        [LabelText("展示Terrain")]
        [OnValueChanged("ShowSceneValueChanged")]
        public bool showTerrainScene;

        [FoldoutGroup("配置scene", -10)]
        [LabelText("展示Hex")]
        [OnValueChanged("ShowSceneValueChanged")]
        public bool showHexScene;

        [FoldoutGroup("配置scene", -10)]
        [LabelText("地形生成方式")]
        [OnValueChanged("TerMeshGenValueChanged")]
        public TerMeshGenMethod genMethod;

        private void ShowSceneValueChanged() {
            sceneManager.UpdateSceneView(showTerrainScene, showHexScene);
        }

        private void TerMeshGenValueChanged() {
            // TODO : 让 EditorSceneManager 改变生成方式
        }

        public override void Enable() {
            base.Enable();
            sceneManager.UpdateSceneView(showTerrainScene, showHexScene);
        }

        public override void Disable() { 
            base.Disable();
            sceneManager.UpdateSceneView(false, false);
        }

    }
}
