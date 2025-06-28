using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace LZ.WarGameMap.Runtime
{
    public class LightThreadPool
    {
        // NOTE : it cant work!
        public static void ScheduleTask(int threadNum, int length, Action<int> exeAction, Action callback) {
            if (length <= 0 || threadNum <= 0 || exeAction == null) {
                callback?.Invoke();
                return;
            }

            int completedThreadCount = 0;
            int batchSize = length / threadNum;
            int remaining = length % threadNum;

            for (int t = 0; t < threadNum; t++) {
                int start = t * batchSize;
                int count = (t == threadNum - 1) ? batchSize + remaining : batchSize;

                ThreadPool.QueueUserWorkItem(_ =>
                {
                    int end = start + count;
                    for (int i = start; i < end; i++) {
                        // you should assure the safe in this action
                        exeAction(i);
                    }

                    if (Interlocked.Increment(ref completedThreadCount) >= threadNum) {
                        callback?.Invoke();
                    }
                });
            }
        }

    }
}
