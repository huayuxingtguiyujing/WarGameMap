using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LZ.WarGameMap.Runtime
{
    public class LRUCache {

        private Dictionary<int, LinkedListNode<int>> m_Map = new Dictionary<int, LinkedListNode<int>>();

        private LinkedList<int> m_List = new LinkedList<int>(); // LinkedListNode<int>

        public int First { get { return m_List.First.Value; } }

        public void Add(int id) {
            if (m_Map.ContainsKey(id)) {
                return;
            }

            var node = new LinkedListNode<int>(id);
            m_Map.Add(id, node);
            m_List.AddLast(node);
        }

        public bool SetActive(int id) {
            if (!m_Map.ContainsKey(id)) {
                return false;
            }

            LinkedListNode<int> node = m_Map[id];
            m_List.Remove(node);
            m_List.AddLast(node);
            return true;
        }
    }
}
