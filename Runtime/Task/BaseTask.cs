using log4net.Core;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using UnityEditor.VersionControl;

namespace LZ.WarGameMap.Runtime
{
    public enum TaskTickLevel
    {
        Fast,           // one frame one tick
        Medium,         // 0.25 second one tick
        Low             // 1 second one tick
    }

    public enum TaskStatu
    {
        Initing,
        Running,
        Completed
    }

    // A BaseTask is a task, for all task in program : load/network/etc
    // You should use Task to control the logic of these process
    // And Task function : 
    // 1. apply progress trace for every phase
    // 2. represent a whole process so you can manage it easliy
    public abstract class BaseTask : IDisposable
    {
        public abstract string GetTaskName();
        public abstract TaskTickLevel GetTickLevel();

        public int TaskID { get; private set; }

        public TaskStatu TaskStat { get; private set; }


        TaskNode RootTask = new TaskNode("", 0, null, "");

        public readonly long TimeOutMs;
        public readonly long StartTimeMs;

        protected Stopwatch stopwatch;
        protected Action<long, bool> ProgressOverCall;

        TaskNode curChildTask;
        int curChildTaskIdx = -1;

        protected BaseTask(Action<long, bool> ProgressOverCall = null, int timeOutMs = -1, bool useTimeWatch = false)
        {
            TaskStat = TaskStatu.Initing;
            curChildTask = null;
            this.ProgressOverCall = ProgressOverCall;
            this.TimeOutMs = timeOutMs;
            this.StartTimeMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            if (useTimeWatch)
            {
                stopwatch = new Stopwatch();
                stopwatch.Start();
            }
        }

        public virtual void StartTask(int taskID)
        {
            TaskID = taskID;
            TaskStat = TaskStatu.Running;
        }

        public virtual bool UpdateTask()
        {
            // time out
            long curTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            if (TimeOutMs > 0 && curTime - StartTimeMs >= TimeOutMs)
            {
                return false;
            }

            return RootTask.UpdateTask();
        }

        public virtual void EndTask(bool abortFlag)
        {
            long costTime = -1;
            if (stopwatch != null)
            {
                stopwatch.Stop();
                costTime = stopwatch.ElapsedMilliseconds;
            }
            ProgressOverCall?.Invoke(costTime, abortFlag);
            ProgressOverCall = null;

            DebugUtility.Log($"end task, ...... BaseTask, cost time : {costTime}, abortFlag : {abortFlag}", DebugPriority.High);

            RootTask.EndTask();

            TaskStat = TaskStatu.Completed;
        }

        public virtual float GetProgress(out string curStatuTxt)
        {
            float weight = RootTask.GetCurWeight();
            float totalWeight = RootTask.GetTotalWeight();
            curStatuTxt = RootTask.GetCurStatTxt();
            //DebugUtility.Log($"weight : {weight}, totalWeight : {totalWeight}, progress : {weight / totalWeight}, status txt : {curStatuTxt}", DebugPriority.High);
            return weight / totalWeight;
        }


        public void AddChildTask(TaskNode taskNode)
        {
            RootTask.AddChildTask(taskNode);
        }

        public virtual void GoNextChildTask(int level)
        {
            // find first running, and over it
            //DebugUtility.Log($"child cur task : {GetTaskName()}, progress : {GetProgress(out _)} : ", DebugPriority.High);
            //DebugTask();
            RootTask.GoNextChildTask(level);
            DebugUtility.Log($"child go next task : {GetTaskName()}, progress : {GetProgress(out _)} : ", DebugPriority.High);
            //DebugTask();
        }

        public virtual bool CheckAllLeafIsOver()
        {
            return RootTask.CheckAllLeafIsOver();
        }

        // Do not recommend call this
        public virtual void SyncTask(string taskName, float progress)
        {
            RootTask.SyncTask(taskName, progress);
        }

        public void DebugTask()
        {
            string rec = RootTask.DebugTask(0, null);
            DebugUtility.Log(rec, DebugPriority.High);
        }

        public float GetCostTime()
        {
            if (stopwatch != null && TaskStat != TaskStatu.Completed)
            {
                return stopwatch.ElapsedMilliseconds;
            }
            return -1;
            
        }

        public virtual void Dispose()
        {
            
        }
    }
}
