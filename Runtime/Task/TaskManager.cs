
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;

namespace LZ.WarGameMap.Runtime
{
    [InitializeOnLoad]
    public class TaskManager : BaseManager
    {
        private static TaskManager _instance;

        static int TaskIDCount = 0;

        public static TaskManager GetInstance()
        {
            if (_instance == null)
            {
                _instance = GetInstance<TaskManager>();
            }
            if (!_instance.IsInit)
            {
                _instance.InitManager();
            }
            return _instance;
        }

        Dictionary<int, TaskTickLevel> TaskTickLevelDict = new Dictionary<int, TaskTickLevel>();

        Dictionary<int, BaseTask> FastLevelTaskDict = new Dictionary<int, BaseTask>();

        Dictionary<int, BaseTask> MediumLevelTaskDict = new Dictionary<int, BaseTask>();

        Dictionary<int, BaseTask> LowLevelTaskDict = new Dictionary<int, BaseTask>();

        public override void InitManager() {
#if UNITY_EDITOR
            EditorApplication.update += Update;
#endif
            IsInit = true; 
        }

        public int StartProgress(TaskTickLevel tickLevel, BaseTask task)
        {
            // TODO : 开启某事务，然后返回一个事务ID（唯一的）（ID会在程序运行期间用完吗？）
            TaskIDCount = (TaskIDCount + 1) / int.MaxValue;
            while (TaskTickLevelDict.ContainsKey(TaskIDCount))
            {
                TaskIDCount = (TaskIDCount + 1) / int.MaxValue;
            }

            TaskTickLevelDict.Add(TaskIDCount, tickLevel);
            if (tickLevel == TaskTickLevel.Fast)
            {
                FastLevelTaskDict.Add(TaskIDCount, task);
            }
            else if (tickLevel == TaskTickLevel.Medium)
            {
                MediumLevelTaskDict.Add(TaskIDCount, task);
            }
            else
            {
                LowLevelTaskDict.Add(TaskIDCount, task);
            }

            return TaskIDCount;
        }

        public void ProgressGoNextTask(int taskID)
        {
            BaseTask timer = GetTask(taskID);
            timer.GoNextChildTask();
            Debug.Log($"go next task : {taskID}, {timer.GetTaskName()}");
        }

        public float GetTaskProgress(int taskID, out string curStatTxt)
        {
            BaseTask task = GetTask(taskID);
            return task.GetProgress(out curStatTxt);
        }

        private BaseTask GetTask(int taskID)
        {
            TaskTickLevelDict.TryGetValue(taskID, out var level);
            BaseTask timer;
            if (level == TaskTickLevel.Fast)
            {
                FastLevelTaskDict.TryGetValue(taskID, out timer);
            }
            else if (level == TaskTickLevel.Medium)
            {
                MediumLevelTaskDict.TryGetValue(taskID, out timer);
            }
            else
            {
                LowLevelTaskDict.TryGetValue(taskID, out timer);
            }
            return timer;
        }

        public BaseTask EndTask(int taskID, bool abortFlag)
        {
            BaseTask task = GetTask(taskID);
            if (task == null)
            {
                Debug.Log($"end task, but the task is null : {taskID}, abort flag : {abortFlag}");
                return null;
            }
            if (task.TaskStat == TaskStatu.Completed)
            {
                Debug.Log($"end task, but the task is completed : {taskID}, abort flag : {abortFlag}");
                return task;
            }

            task.EndTask(abortFlag);
            Debug.Log($"remove timer : {taskID}, {task.GetTaskName()}");
            TaskTickLevelDict.TryGetValue(taskID, out var level);
            if (level == TaskTickLevel.Fast)
            {
                FastLevelTaskDict.Remove(taskID);
            }
            else if (level == TaskTickLevel.Medium)
            {
                MediumLevelTaskDict.Remove(taskID);
            }
            else
            {
                LowLevelTaskDict.Remove(taskID);
            }
            TaskTickLevelDict.Remove(taskID);
            return task;
        }

        public void EndAllProgress()
        {
            TaskTickLevelDict.Clear();
            FastLevelTaskDict.Clear();
            MediumLevelTaskDict.Clear();
            LowLevelTaskDict.Clear();
        }

        // If you do not set callback on node, you can use it sync progress external
        public void SyncProgress(int progressID, string taskName, float progress)
        {
            BaseTask timer = GetTask(progressID);
            timer.SyncTask(taskName, progress);
        }


        public int MediumTick = 100;
        private int lastMediumTick = 0;

        public int LowTick = 500;
        private int lastLowTick = 0;

        List<int> overProgressList = new List<int>();

        private void OnDisable()
        {
            RemoveUpdate();
        }

        private void OnDestroy()
        {
            RemoveUpdate();
        }

        private void RemoveUpdate()
        {
#if UNITY_EDITOR
            EditorApplication.update -= Update;
#endif
        }

        private void Update()
        {
            if (!IsInit)
            {
                return;
            }

            overProgressList.Clear();

            lastLowTick++;
            if (lastLowTick >= LowTick)
            {
                lastLowTick = 0;
                TickAndRemoveTask(ref LowLevelTaskDict);
            }

            lastMediumTick++;
            if (lastMediumTick >= MediumTick)
            {
                lastMediumTick = 0;
                TickAndRemoveTask(ref MediumLevelTaskDict);
            }

            TickAndRemoveTask(ref FastLevelTaskDict);

            for(int i = 0; i < overProgressList.Count; i++)
            {
                EndTask(overProgressList[i], false);
            }
        }

        private void TickAndRemoveTask(ref Dictionary<int, BaseTask> TimerDict)
        {
            foreach (var pair in TimerDict)
            {
                pair.Value.UpdateTask();
                bool completed = pair.Value.CheckAllLeafIsOver();;
                // can not use TaskStat == TaskStatu.Completed to judge...
                if (completed)
                {
                    Debug.Log($"detect task over : {pair.Value.GetTaskName()}, task id : {pair.Key}");  // TODO : 为什么从不触发?
                    overProgressList.Add(pair.Key);
                }
                //pair.Value.DebugTask();
                Debug.Log($"tick task : {pair.Value.GetTaskName()}, task id : {pair.Key}");
            }
        }

    }
}
