using LZ.WarGameMap.Runtime.Model;
using System;
using UnityEditor;
using UnityEngine;

namespace LZ.WarGameMap.MapEditor
{
    internal class CountryDeletePop : BaseEditorPop
    {
        static GUIContent confirmPopTitleTxt = new GUIContent("��ʾ����");

        static string countryDelContent = "��ʾ��������ɾ����������";

        public static CountryDeletePop GetPopInstance()
        {
            return GetPopInstance<CountryDeletePop>(confirmPopTitleTxt, popSize);
        }

        bool enableMoveToggle = true;
        CountryData countryData;
        Action<bool> ConfirmEvent;
        Action CancelEvent;

        public override void ShowBasePop(params object[] args)
        {
            countryData  = (CountryData)args[0];
            ConfirmEvent = (Action<bool>)args[1];
            CancelEvent  = (Action)args[2];
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
            ShowCenterStatTxt(countryDelContent);
            ShowEnableToggle();
            ShowConfirmBtns();
        }

        private void ShowEnableToggle()
        {
            EditorGUILayout.Space(15);
            enableMoveToggle = EditorGUILayout.Toggle("������ת��������", enableMoveToggle);
            EditorGUILayout.Space(15);
        }

        private void ShowConfirmBtns()
        {
            EditorGUILayout.Space(15);
            EditorGUILayout.BeginHorizontal();
            {
                if (GUILayout.Button("ȷ��", normalButtonStyle))
                {
                    ConfirmEvent?.Invoke(enableMoveToggle);
                    ClosePop();
                }

                if (GUILayout.Button("ȡ��", normalButtonStyle))
                {
                    CancelEvent?.Invoke();
                    ClosePop();
                }
            }
            EditorGUILayout.EndHorizontal();
        }

    }
}
