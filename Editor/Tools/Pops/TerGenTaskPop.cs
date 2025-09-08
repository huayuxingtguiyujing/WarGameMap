using LZ.WarGameMap.Runtime;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace LZ.WarGameMap.MapEditor
{
    internal class TerGenTaskPop : BaseEditorPop
    {
        static GUIContent popTitleTxt = new GUIContent("地形生成进度");
        static string cancelGenStatuTxt = "中止生成";
        static string overGenStatuTxt = "生成完毕!";

        static string statuTxt;
        static float progress = 0f;

        TerrainGenTask task;

        public static TerGenTaskPop GetPopInstance()
        {
            return GetPopInstance<TerGenTaskPop>(popTitleTxt, popSize);
        }

        protected override void OnGUIHided()
        {
            if (!IsValid)
            {
                return;
            }
            bool IsGenning = task.TaskStat == TaskStatu.Running;
            EndTerGenTaskEvent(IsGenning);
            base.OnGUIHided();
        }

        public override void ShowBasePop(params object[] args)
        {
            task = (TerrainGenTask)args[0];
            progress = 0f;
            statuTxt = "";
            //UnityEditorManager.RegisterUpdate(OnGUIDraw);
            base.ShowBasePop(args);
        }

        public override void HideBasePop()
        {
            progress = 0f;
            base.HideBasePop();
        }

        protected override void OnGUIDraw()
        {
            if (!IsValid)
            {
                return;
            }

            if(task == null)
            {
                return;
            }

            if(task.TaskStat == TaskStatu.Initing)
            {
                return;
            }

            bool IsGenning = task.TaskStat == TaskStatu.Running;

            ShowStatAndProgress();
            ShowTerGenTaskBtns(IsGenning);
            UpdateProgress(IsGenning);
        }

        private void ShowStatAndProgress()
        {
            ShowCenterStatTxt(statuTxt);

            float padding = 20f;
            Rect progressRect = EditorGUILayout.GetControlRect(false, 20);
            Rect fixedProgressRect = new Rect(
                progressRect.x + padding, progressRect.y,
                progressRect.width - 2 * padding, progressRect.height
            );
            EditorGUI.ProgressBar(fixedProgressRect, progress, $"{progress * 100:F1}%");
            EditorGUILayout.Space(20);
        }

        private void ShowTerGenTaskBtns(bool IsGenning)
        {
            EditorGUILayout.BeginHorizontal();
            {
                EditorGUI.BeginDisabledGroup(!IsGenning);
                if (GUILayout.Button("中止", normalButtonStyle))
                {
                    EndTerGenTaskEvent(true);
                }
                EditorGUI.EndDisabledGroup();

                EditorGUI.BeginDisabledGroup(!ShouldDisableConfirm(IsGenning));
                if (GUILayout.Button("确认", normalButtonStyle))
                {
                    EndTerGenTaskEvent(false);
                }
                EditorGUI.EndDisabledGroup();
            }
            EditorGUILayout.EndHorizontal();
        }

        private bool ShouldDisableConfirm(bool IsGenning)
        {
            return (progress >= 1f) || !IsGenning;
        }

        private void EndTerGenTaskEvent(bool abortFlag)
        {
            TaskManager.GetInstance().EndTask(task.TaskID, abortFlag);
            if (abortFlag)
            {
                statuTxt = string.Format("{0} 花费时间: {1}", cancelGenStatuTxt, task.GetCostTime());
            }
            else
            {
                statuTxt = string.Format("{0} 花费时间: {1}", overGenStatuTxt, task.GetCostTime());
                if (this)
                {
                    this.Close();
                }
            }
        }


        int tickTime = 0;

        List<string> tickFrameStr = new List<string>() { ".", "..", "...", "....", "....." };

        private void UpdateProgress(bool IsGenning)
        {
            if (IsGenning)
            {
                tickTime++;
                tickTime %= tickFrameStr.Count;

                progress = task.GetProgress(out statuTxt);
                if (progress >= 1f)
                {
                    statuTxt = string.Format(overGenStatuTxt, tickFrameStr[tickTime]);
                }
                else
                {
                    statuTxt = string.Format(statuTxt, tickFrameStr[tickTime]);
                }
                this.Repaint();
            }
        }
    }
}
