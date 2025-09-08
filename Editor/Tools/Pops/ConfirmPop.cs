using System;
using UnityEditor;
using UnityEngine;

namespace LZ.WarGameMap.MapEditor
{
    internal class ConfirmPop : BaseEditorPop
    {
        static GUIContent confirmPopTitleTxt = new GUIContent("��ʾ����");

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
                if (GUILayout.Button("ȷ��", normalButtonStyle))
                {
                    ConfirmEvent?.Invoke();
                }

                if (GUILayout.Button("ȡ��", normalButtonStyle))
                {
                    CancelEvent?.Invoke();
                }
            }
            EditorGUILayout.EndHorizontal();
        }

    }
}
