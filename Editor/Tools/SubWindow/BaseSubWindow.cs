using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LZ.WarGameMap.MapEditor
{
    public class BaseSubWindow : OdinEditorWindow
    {

        protected static readonly Vector2 verticalWindowSize = new Vector2(256, 512);

        [HideInInspector]
        public bool IsValid;

        public static SubWindow GetPopInstance<SubWindow>(GUIContent popTitleTxt, Vector2 popSize) where SubWindow : BaseSubWindow
        {
            var window = GetWindow<SubWindow>();
            window.minSize = popSize;
            window.maxSize = popSize;
            window.titleContent = popTitleTxt;
            window.ShowUtility();
            return window;
        }

        protected override void OnDestroy()
        {
            HideSubWindow();
        }

        public virtual void ShowSubWindow(params object[] args) { IsValid = true; }

        public virtual void HideSubWindow() { IsValid = false; }

        protected void CloseWindow()
        {
            if (this)
            {
                this.Close();
            }
        }
    }
}
