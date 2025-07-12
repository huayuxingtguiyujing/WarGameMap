using NUnit.Framework.Constraints;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LZ.WarGameMap.Runtime
{

    // only num value
    public class SimpleHeapStruct<Element> where Element : IComparable<Element> {

        private List<Element> heap = new List<Element>();

        public int Count => heap.Count;

        public bool IsMinHeap { get; private set; }

        public SimpleHeapStruct(bool isMinHeap) {
            IsMinHeap = isMinHeap;
        }

        public void Push(Element item) {
            heap.Add(item);
            HeapifyUp(heap.Count - 1);
        }

        public bool Empty() {
            return heap.Count <= 0;
        }

        public Element Pop() {
            if (heap.Count == 0)
                throw new InvalidOperationException("Heap is empty");

            Element root = heap[0];
            Element last = heap[heap.Count - 1];
            heap.RemoveAt(heap.Count - 1);

            if (heap.Count > 0) {
                heap[0] = last;
                HeapifyDown(0);
            }

            return root;
        }

        public Element Peek() {
            if (heap.Count == 0)
                throw new InvalidOperationException("Heap is empty");
            return heap[0];
        }

        private void HeapifyUp(int index) {
            while (index > 0) {
                int parent = (index - 1) / 2;

                if (IsMinHeap) {
                    if (heap[index].CompareTo(heap[parent]) >= 0) {
                        break;
                    }
                } else {
                    if (heap[index].CompareTo(heap[parent]) <= 0) {
                        break;
                    }
                }

                Swap(index, parent);
                index = parent;
            }
        }

        private void HeapifyDown(int index) {
            int count = heap.Count;
            while (true) {
                int left = 2 * index + 1;
                int right = 2 * index + 2;
                int smallest = index;

                if (IsMinHeap) {
                    if (left < count && heap[left].CompareTo(heap[smallest]) < 0)
                        smallest = left;
                    if (right < count && heap[right].CompareTo(heap[smallest]) < 0)
                        smallest = right;
                } else {
                    if (left < count && heap[left].CompareTo(heap[smallest]) > 0)
                        smallest = left;
                    if (right < count && heap[right].CompareTo(heap[smallest]) > 0)
                        smallest = right;
                }
                
                if (smallest == index)
                    break;

                Swap(index, smallest);
                index = smallest;
            }
        }

        private void Swap(int i, int j) {
            Element tmp = heap[i];
            heap[i] = heap[j];
            heap[j] = tmp;
        }
    
    }
}
