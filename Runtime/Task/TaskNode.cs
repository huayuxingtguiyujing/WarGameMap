using System;
using System.Collections.Generic;
using System.Text;

namespace LZ.WarGameMap.Runtime
{
    public sealed class TaskNode
    {
        public readonly static float TerminalProgress = 1.0f;

        public string TaskName;
        public float Progress;
        public float Weight;
        public string StatusTxt;

        private Func<float> CountLogicCall;

        public List<TaskNode> ChildTaskList = new List<TaskNode>();

        TaskNode curChildTask;
        int curChildTaskIdx = -1;

        public bool IsNodeOver;

        public bool IsLeaf() => (ChildTaskList.Count == 0);

        public TaskNode() { Progress = 0f; }

        public TaskNode(string taskName, float weight, Func<float> countLogicCall, string statusTxt)
        {
            TaskName = taskName;
            Progress = 0f;
            Weight = weight;
            StatusTxt = statusTxt;

            CountLogicCall = countLogicCall;
            IsNodeOver = false;
        }

        public void AddChildTask(string taskName, float weight, Func<float> countLogicCall, string statuTxt = "")
        {
            TaskNode newTask = new TaskNode(taskName, weight, countLogicCall, statuTxt);
            AddChildTask(newTask);
        }

        public void AddChildTask(TaskNode newTask)
        {
            ChildTaskList.Add(newTask);

            // init curChildTask to idx 0
            curChildTask = ChildTaskList[0];
            curChildTaskIdx = 0;
        }

        public float GetCurWeight()
        {
            if (IsLeaf())
            {
                return Progress * Weight;
            }
            else
            {
                float weight = 0f;
                foreach (var childTask in ChildTaskList)
                {
                    weight += childTask.GetCurWeight();
                }
                return weight;
            }
        }

        public float GetTotalWeight()
        {
            if (IsLeaf())
            {
                return Weight;
            }
            else
            {
                float totalWeight = 0f;
                foreach (var childTask in ChildTaskList)
                {
                    totalWeight += childTask.GetTotalWeight();
                }
                return totalWeight;
            }
        }

        public string GetCurStatTxt()
        {
            if (IsLeaf())
            {
                return StatusTxt;
            }
            else
            {
                if (curChildTask != null)
                {
                    return string.Format("{0} {1}", StatusTxt, curChildTask.GetCurStatTxt());
                }
                else
                {
                    return StatusTxt;
                }
            }
        }

        public bool UpdateTask()
        {
            if (IsLeaf())
            {
                if (CountLogicCall != null)
                {
                    Progress = CountLogicCall();
                }
                bool taskOver = (Progress >= TerminalProgress);
                return taskOver;
            }
            else
            {
                // find uncomplete task, and cache it
                if (curChildTask == null || curChildTask.IsNodeOver)
                {
                    for (int i = 0; i < ChildTaskList.Count; i++)
                    {
                        if (ChildTaskList[i].Progress < TerminalProgress)
                        {
                            curChildTask = ChildTaskList[i];
                            curChildTaskIdx = i;
                            break;
                        }
                    }
                }
                if (curChildTask == null)
                {
                    EndTask();
                    return false;       // no more task, so its over
                }

                curChildTask.UpdateTask();
                return true;
            }
        }

        // NOTE : taskNode is a tree, level is depth
        public void GoNextChildTask(int level = 1)
        {
            if (!CheckNodeValid())
            {
                DebugUtility.LogError($"node is not valid : {IsNodeOver}, curChildTask : {curChildTask != null}");
            }

            if (IsLeaf() || IsNodeOver)
            {
                //DebugTask();
                DebugUtility.LogError($"node is leaf or over, cannot go next child Task, isLeaf : {IsLeaf()}, nodeOver : {IsNodeOver}");
                return;
            }

            if (level > 1)
            {
                level--;
                curChildTask.GoNextChildTask(level);
                return;
            }

            // go next child task, if no child, do nothing, will not influence child's child
            // find first running, and over it
            curChildTask.EndTask();
            int nextChildTaskIdx = curChildTaskIdx + 1;
            if (nextChildTaskIdx < ChildTaskList.Count)
            {
                curChildTask = ChildTaskList[nextChildTaskIdx];
                curChildTaskIdx = nextChildTaskIdx;
            }
            else
            {
                curChildTask = null;
                curChildTaskIdx = -1;
            }

            if (curChildTask == null)
            {
                EndTask();
            }
            UpdateTask();
        }

        public bool CheckAllLeafIsOver()
        {
            bool ans = true;
            if(IsLeaf())
            {
                ans = ans && IsNodeOver;
            }
            else
            {
                foreach (var childTask in ChildTaskList)
                {
                    ans = ans && childTask.CheckAllLeafIsOver();
                }
            }
            return ans;
        }

        public void SyncTask(string taskName, float progress)
        {
            TaskNode targetTask = FindTask(taskName);
            targetTask.SyncProgress(progress);
        }

        private void SyncProgress(float progress)
        {
            Progress = progress;
        }

        public void EndTask()
        {
            // end this, and its children
            float childrenWeight = GetTotalWeight();
            foreach (var child in ChildTaskList)
            {
                child.EndTask();
            }
            Progress = TerminalProgress;
            Weight = childrenWeight;        // when end tasknode, record all children's weight
            ChildTaskList.Clear();

            IsNodeOver = true;
        }

        public TaskNode FindTask(string taskName)
        {
            if (this.TaskName == taskName)
            {
                return this;
            }
            TaskNode ans;
            foreach (var task in ChildTaskList)
            {
                ans = task.FindTask(taskName);
                if (ans != null)
                {
                    return ans;
                }
            }
            return null;
        }

        public bool GetNodeOver() => IsNodeOver;

        public bool CheckNodeValid()
        {
            return IsNodeOver && (curChildTask != null);
        }
        
        public string DebugTask(int level, TaskNode curTickingTask)
        {

            StringBuilder nodeInfo = new StringBuilder();
            if(curTickingTask == this)
            {
                nodeInfo.Append("*Cur*, ");
            }
            
            nodeInfo.Append($"Level:{level}");
            nodeInfo.Append($", TaskName:{TaskName}");
            nodeInfo.Append($", StatusTxt:{StatusTxt}");
            nodeInfo.Append($", Weight:{GetCurWeight()}");
            nodeInfo.Append($", IsNodeOver:{IsNodeOver}");

            nodeInfo.Append(" -> ");
            foreach (var childTaskNode in ChildTaskList)
            {
                nodeInfo.Append(childTaskNode.DebugTask(level + 1, curChildTask));
            }
            nodeInfo.Append(" -> ");
            return nodeInfo.ToString();
        }

    }
}
