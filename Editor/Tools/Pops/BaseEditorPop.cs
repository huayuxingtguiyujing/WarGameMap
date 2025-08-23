using UnityEditor;
using UnityEngine;

namespace LZ.WarGameMap.MapEditor
{
    internal abstract class BaseEditorPop : EditorWindow
    {

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

        protected virtual void OnGUIHided() { IsValid = false; }

        private void OnGUI()
        {
            OnGUIDraw();
        }

        public virtual void ShowBasePop(params object[] args) { IsValid = true; }

        public virtual void HideBasePop() { IsValid = false; }

        protected virtual void OnGUIDraw() { }

    }
}
