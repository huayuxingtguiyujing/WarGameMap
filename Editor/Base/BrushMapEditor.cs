using LZ.WarGameMap.Runtime;
using Sirenix.OdinInspector;
using UnityEngine;

namespace LZ.WarGameMap.MapEditor
{

    // ��Ҫ��scene����brush������Ҫ�鿴scene�е�ͼ��Ϣ�ģ��̳и���
    public abstract class BrushMapEditor : BaseMapEditor {

        [FoldoutGroup("����scene", -10)]
        [LabelText("չʾTerrain")]
        [OnValueChanged("ShowSceneValueChanged")]
        public bool showTerrainScene;

        [FoldoutGroup("����scene", -10)]
        [LabelText("չʾHex")]
        [OnValueChanged("ShowSceneValueChanged")]
        public bool showHexScene;

        [FoldoutGroup("����scene", -10)]
        [LabelText("�������ɷ�ʽ")]
        [OnValueChanged("TerMeshGenValueChanged")]
        public TerMeshGenMethod genMethod;

        private void ShowSceneValueChanged() {
            sceneManager.UpdateSceneView(showTerrainScene, showHexScene);
        }

        private void TerMeshGenValueChanged() {
            // TODO : �� EditorSceneManager �ı����ɷ�ʽ
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
