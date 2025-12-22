using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;


namespace LZ.WarGameCommon {
    using ActionList = List<Action>;
    public class EventManager
    {
        public static EventManager instance;
        public static EventManager Instance {
            get {
                if (instance == null)
                {
                    instance = new EventManager();
                }

                return instance;
            }
        }
        
        // 基于 UnityEvent, 使用 UnityEvent 的提供的 add, remove 机制(因为好用)
        public class RegisteredEvent : UnityEvent { }
        
        // 存储所有注册的事件
        private Dictionary<int, RegisteredEvent> eventDic = new Dictionary<int, RegisteredEvent>();

        public void SubScribe(int eventID, UnityAction registerEvent)
        {
            if (eventDic.TryGetValue(eventID, out RegisteredEvent registrableEvent))
            {
                eventDic[eventID].AddListener(registerEvent);
            }
            else
            {
                RegisteredEvent eventRec = new RegisteredEvent();
                eventRec.AddListener(registerEvent);
                eventDic.Add(eventID, eventRec);
            }
        }

        public void Unsubscribe(int eventID, UnityAction unRegisterEvent)
        {
            if (eventDic.TryGetValue(eventID, out RegisteredEvent registrableEvent))
            {
                eventDic[eventID].RemoveListener(unRegisterEvent);
            }
        }

        // TODO：还是要支持传入参数！！！
        public void Publish(int eventID)
        {
            if (eventDic.TryGetValue(eventID, out RegisteredEvent registrableEvent))
            {
                registrableEvent?.Invoke();
            }
        }
        
    }

}
