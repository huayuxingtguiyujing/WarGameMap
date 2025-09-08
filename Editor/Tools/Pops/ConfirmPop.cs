using System;
using UnityEditor;
using UnityEngine;

namespace LZ.WarGameMap.MapEditor
{
    internal class ConfirmPop : BaseEditorPop
    {
        static GUIContent confirmPopTitleTxt = new GUIContent("提示窗口");

        public static TerGenTaskPop GetPopInstance()
        {
            return GetPopInstance<TerGenTaskPop>(confirmPopTitleTxt, popSize);
        }

        string confirmContent;
        Action ConfirmEvent;
        Action CancelEvent;

        public override void ShowBasePop(params object[] args)
        {
            confirmContent = (string)args[0];
            ConfirmEvent = (Action)args[1];
            CancelEvent = (Action)args[2];
            base.ShowBasePop(args);
        }

        public override void HideBasePop()
        {
            ConfirmEvent = null;
            CancelEvent = null;
            base.HideBasePop();
        }

        protected override void OnGUIDraw()
        {
            if (!IsValid)
            {
                return;
            }
            ShowCenterStatTxt(confirmContent);
            ShowConfirmBtns();
        }

        private void ShowConfirmBtns()
        {
            EditorGUILayout.BeginHorizontal();
            {
                if (GUILayout.Button("确认", normalButtonStyle))
                {
                    ConfirmEvent?.Invoke();
                }

                if (GUILayout.Button("取消", normalButtonStyle))
                {
                    CancelEvent?.Invoke();
                }
            }
            EditorGUILayout.EndHorizontal();
        }

    }
}
