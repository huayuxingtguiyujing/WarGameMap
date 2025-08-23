using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;


namespace LZ.WarGameMap.Runtime
{

    public class TaskInfo : IDisposable
    {
        public CancellationTokenSource CancelToken;

        public TaskInfo(CancellationTokenSource cancelToken)
        {
            CancelToken = cancelToken;
        }

        public void CancelTask()
        {
            CancelToken.Cancel();
        }

        public void Dispose()
        {
            CancelToken.Dispose();
        }
    }

    [ExecuteAlways]
    public class ThreadManager : MonoBehaviour
    {
        public static ThreadManager Instance { get; private set; }

        ConcurrentQueue<Action> mainThreadQueue = new ConcurrentQueue<Action>();                    // main thread call

        ConcurrentDictionary<int, TaskInfo> ActiveTasks = new ConcurrentDictionary<int, TaskInfo>();    // running task


        private int taskCounter = 0;    // task id

        private int completedTaskCounter = 0;


        public int GetActiveTaskCount()
        {
            return ActiveTasks.Count;
        }

        public int GetCompletedTaskCount()
        {
            return completedTaskCounter;
        }

        public void DebugTaskStatus()
        {
            foreach (var kv in ActiveTasks)
            {
                Debug.Log($"[Task {kv.Key}], NameID: {kv.Value}");
            }
        }

        public static ThreadManager GetInstance()
        {
            if (Instance == null)
            {
                GameObject otherRootObj = MapEnum.GetOtherRootObj();
                ThreadManager taskManager = otherRootObj.GetComponent<ThreadManager>();
                if (taskManager == null)
                {
                    taskManager = otherRootObj.AddComponent<ThreadManager>();
                    InitThreadPool();
                }
                Instance = taskManager;
            }
            return Instance;
        }

        private static void InitThreadPool()
        {
            int cpuCount = Environment.ProcessorCount;
            ThreadPool.SetMinThreads(cpuCount, cpuCount);
            ThreadPool.SetMaxThreads(cpuCount * 4, cpuCount * 4);
            DebugUtility.Log($"[TaskManager] ThreadPool initialized. MinThreads={cpuCount}, MaxThreads={cpuCount * 4}");
        }

        // TODO : 这个机制讲道理真的好吗？
        void Update()
        {
            while (mainThreadQueue.TryDequeue(out var action))
            {
                try 
                { 
                    action?.Invoke(); 
                }
                catch (Exception e) 
                { 
                    Debug.LogError(e); 
                }
            }
            //DebugUtility.Log(SimplifyProcess.GetSimplifyProcess(), DebugPriority.Medium);
        }



        public Task RunTaskAsync(Action<CancellationToken> func, Action<int> onCompleted, CancellationToken externalToken = default, int timeoutMs = Timeout.Infinite)
        {
            var cancelSrc = CancellationTokenSource.CreateLinkedTokenSource(externalToken);
            int taskId = GetInCrementTaskId();
            int taskIdRec = taskId;
            TaskInfo taskInfo = new TaskInfo(cancelSrc);
            if (timeoutMs != Timeout.Infinite)
            {
                cancelSrc.CancelAfter(timeoutMs);
            }

            Task task = Task.Run(() => {
                try
                {
                    func(cancelSrc.Token);
                }
                catch (Exception e)
                {
                    EndTask(taskIdRec);
                    throw new Exception(e.ToString());
                }
                finally
                {
                    onCompleted?.Invoke(taskIdRec);
                    EndTask(taskIdRec);
                }
            }, cancelSrc.Token);

            ActiveTasks[taskId] = taskInfo;
            return task;
        }

        public void CancelTask(int taskId)
        {
            if (ActiveTasks.TryGetValue(taskId, out var taskInfo))
            {
                taskInfo.CancelTask();
            }
        }

        public void EndTask(int taskId)
        {
            if (ActiveTasks.TryGetValue(taskId, out var taskInfo))
            {
                taskInfo.Dispose();
                ActiveTasks.TryRemove(taskId, out _);
            }
        }

        // TODO : 需要改进，现在的 Task 只能运行，不能暂停/中止
        [Obsolete]
        public int RunAsync<T>(Func<CancellationToken, T> backgroundTask, Action<T> onComplete = null, int timeLimitMS = 0, string taskName = "")
        {
            int taskId = GetInCrementTaskId();
            var cts = new CancellationTokenSource();
            CancellationToken token = cts.Token;

            Task threadTask = Task.Run(() =>
            {
                try
                {
                    T result = default;
                    result = backgroundTask(token);
                    mainThreadQueue.Enqueue(() =>
                    {
                        if (token.IsCancellationRequested)
                        {
                            DebugUtility.Log($"[TaskManager] Task {taskId} was cancelled", DebugPriority.Medium);
                        }
                        onComplete?.Invoke(result);
                        ActiveTasks.TryRemove(taskId, out _);
                    });
                }
                catch (Exception e)
                {
                    DebugUtility.LogError(e.ToString(), DebugPriority.High);
                }

            }, token);

            if (timeLimitMS > 0)
            {
                Task.Delay(timeLimitMS, token).ContinueWith(_ =>
                {
                    if (!threadTask.IsCompleted)
                    {
                        cts.Cancel();
                        DebugUtility.Log($"[TaskManager] Task {taskId} timed out after {timeLimitMS}ms");
                    }
                });
            }
            return taskId;
        }
        
        [Obsolete]
        public int RunAsync(Action backgroundTask, Action onComplete = null, string taskName = "")
        {
            int taskId = GetInCrementTaskId();
            Task.Run(() =>
            {
                try
                {
                    backgroundTask();
                    if (onComplete != null)
                    {
                        mainThreadQueue.Enqueue(() => {
                            onComplete();
                            ActiveTasks.TryRemove(taskId, out _);
                        });
                    }
                    else
                    {
                        ActiveTasks.TryRemove(taskId, out _);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError(e);
                }
            });
            return taskId;
        }

        private int GetInCrementTaskId()
        {
            int taskId = Interlocked.Increment(ref taskCounter);
            return taskId;
        }

    }
}
