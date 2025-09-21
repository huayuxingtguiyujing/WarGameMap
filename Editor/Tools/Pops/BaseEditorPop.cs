using UnityEditor;
using UnityEngine;

namespace LZ.WarGameMap.MapEditor
{
    internal abstract class BaseEditorPop : EditorWindow
    {
        protected static readonly Vector2 popSize = new Vector2(300, 150);
        protected static readonly Vector2 buttonSize = new Vector2(100, 30);

        protected static GUIStyle normalButtonStyle = new GUIStyle(GUI.skin.button)
        {
            fixedHeight = buttonSize.y,
            stretchWidth = true,
            margin = new RectOffset(10, 10, 5, 5),
            alignment = TextAnchor.MiddleCenter
        };


        public bool IsValid;

        public static PopWindow GetPopInstance<PopWindow>(GUIContent popTitleTxt, Vector2 popSize) where PopWindow : BaseEditorPop
        {
            var window = GetWindow<PopWindow>();
            window.minSize = popSize;
            window.maxSize = popSize;
            window.titleContent = popTitleTxt;
            window.Show();
            return window;
        }

        private void OnDisable()
        {
            //OnGUIHided();
        }

        private void OnGUI()
        {
            OnGUIDraw();
        }

        protected virtual void OnGUIDraw() { }

        protected virtual void OnGUIHided() { IsValid = false; }

        protected void ShowCenterStatTxt(string confirmContent)
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(10);
            EditorGUILayout.LabelField(confirmContent, EditorStyles.boldLabel);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(10);
        }

        protected void ClosePop()
        {
            if (this)
            {
                this.Close();
            }
        }

        public virtual void ShowBasePop(params object[] args) { IsValid = true; }

        public virtual void HideBasePop() { IsValid = false; }

    }
}
