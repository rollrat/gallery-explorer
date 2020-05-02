// This source code is a part of DCInside Gallery Explorer Project.
// Copyright (C) 2020. rollrat. Licensed under the MIT Licence.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GalleryExplorer.Core
{
    /*public class IVPTreeNode : IComparable<IVPTreeNode>
    {
        public bool IsLeaf { get; set; }
        public int Index { get; set; }
        public int Distance { get; set; }
        public int DMin { get; set; }
        public int DMax { get; set; }
        public int Myu { get; set; }
        public IVPTreeNode Left { get; set; }
        public IVPTreeNode Right { get; set; }

        public int CompareTo(IVPTreeNode node) => Distance.CompareTo(node.Distance);
    }

    public class VPTreeNode : IVPTreeNode
    {
    }

    public class VPTreeLeafNode<T> : IVPTreeNode where T : IComparable<T>
    {
        public T Data { get; set; }
    }

    /// <summary>
    /// Implemention of Vantage Point Tree Data Structure
    /// 
    /// https://github.com/fpirsch/vptree.js
    /// https://github.com/szalaigj/VPTree/tree/master/VPTreeApp/Distance
    /// </summary>
    public class VPTree<T> where T : IComparable<T>
    {
        /// <summary>
        /// Partition list by pivot
        /// </summary>
        /// <param name="list"></param>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <param name="pivot"></param>
        /// <returns></returns>
        private int partition<F>(List<F> list, int left, int right, int pivot) where F : IComparable<F>
        {
            var pv = list[pivot];
            var swap = list[pivot];
            list[pivot] = list[right];
            list[right] = swap;
            var store = left;
            for (int i = left; i < right; i++)
            {
                if (list[i].CompareTo(pv) < 0)
                {
                    swap = list[store];
                    list[store] = list[i];
                    list[i] = swap;
                    store++;
                }
            }
            swap = list[right];
            list[right] = list[store];
            list[store] = swap;
            return store;
        }

        /// <summary>
        /// Get the index of the median of three items.
        /// </summary>
        /// <param name="list"></param>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="c"></param>
        /// <returns></returns>
        private int mid<F>(List<F> list, int a, int b, int c) where F : IComparable<F>
        {
            F A = list[a], B = list[b], C = list[c];
            if (A.CompareTo(B) < 0)
            {
                if (B.CompareTo(C) < 0)
                    return b;
                else if (A.CompareTo(C) < 0)
                    return c;
                else
                    return a;
            }
            else if (A.CompareTo(C) < 0)
                return a;
            else if (B.CompareTo(C) < 0)
                return c;
            return b;
        }

        /// <summary>
        /// Relocate the left side to be smaller than the nth element 
        /// aligned with respect to the nth element, and relocate the 
        /// right side to things larger than the aligned nth element, 
        /// then get the corresponding nth element.
        /// </summary>
        /// <param name="list"></param>
        /// <param name="left"></param>
        /// <param name="nth"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        private F nth_element<F>(List<F> list, int left, int nth, int right) where F : IComparable<F>
        {
            while (true)
            {
                var pivot = mid(list, left, right, (left + right) >> 1);
                var newpv = partition(list, left, right, pivot);
                var dist = newpv - left + 1;
                if (dist == nth)
                    return list[pivot];
                else if (nth < dist)
                    right = newpv - 1;
                else
                {
                    nth -= dist;
                    left = newpv + 1;
                }
            }
        }

        private F select<F>(List<F> list, int k) where F : IComparable<F>
            => nth_element(list, 0, k + 1, list.Count - 1);

        private int select_vp_index(List<T> list)
            => new Random().Next(list.Count);

        private void build(List<T> list, Func<T, T, int> distance/*, int bucket_size*)
        {
            var ll = Enumerable.Range(0, list.Count).Select(x => new IVPTreeNode { Index = x }).ToList();
            var tree = recurse(list, ll, distance/*, bucket_size*);
        }

        private IVPTreeNode recurse(List<T> S, List<IVPTreeNode> list, Func<T, T, int> distance/*, int bucket_size*)
        {
            if (S.Count == 0) return null;
            if (S.Count == 1)
            {
                return new VPTreeLeafNode<T> { Data = S[0], IsLeaf = true };
            }

            int pivot = select_vp_index(S);
            var node = list[pivot];
            list.RemoveAt(pivot);
            if (list.Count == 0)
                return node;

            var vp = S[node.Index];
            var dmin = int.MaxValue;
            var dmax = 0;
            for (int i = 0; i < list.Count; i++)
            {
                var item = list[i];
                var dist = distance(vp, S[item.Index]);
                item.Distance = dist;
                if (dmin > dist) dmin = dist;
                if (dmax < dist) dmax = dist;
            }

            node.DMax = dmax;
            node.DMin = dmin;

            var mid = list.Count >> 1;
            var med = select(list, mid);

            var left = list.Take(mid + 1).ToList();
            var right = list.Skip(mid + 1).ToList();

            node.Myu = med.Distance;
            node.Left = recurse(S, left, distance);
            node.Right = recurse(S, right, distance);
            return node;
        }
    }*/

    public delegate double CalculateDistance<T>(T item1, T item2);

	/// <summary>
	/// https://github.com/mcraiha/CSharp-vptree/blob/master/VpTree.cs
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public class VpTree<T>
	{

		public VpTree()
		{
			this.rand = new Random(); // Used in BuildFromPoints
		}

		public void Create(T[] newItems, CalculateDistance<T> distanceCalculator)
		{
			this.items = newItems;
			this.calculateDistance = distanceCalculator;
			this.root = this.BuildFromPoints(0, newItems.Length);
		}

		public void Search(T target, int numberOfResults, out T[] results, out double[] distances)
		{
			List<HeapItem> closestHits = new List<HeapItem>();

			// Reset tau to longest possible distance
			this.tau = double.MaxValue;

			// Start search
			Search(root, target, numberOfResults, closestHits);

			// Temp arrays for return values
			List<T> returnResults = new List<T>();
			List<double> returnDistance = new List<double>();

			// We have to reverse the order since we want the nearest object to be first in the array
			for (int i = numberOfResults - 1; i > -1; i--)
			{
				returnResults.Add(this.items[closestHits[i].index]);
				returnDistance.Add(closestHits[i].dist);
			}

			results = returnResults.ToArray();
			distances = returnDistance.ToArray();
		}

		private T[] items;
		private double tau;
		private Node root;
		private Random rand; // Used in BuildFromPoints

		private CalculateDistance<T> calculateDistance;

		private class Node // This cannot be struct because Node referring to Node causes error CS0523
		{
			public int index;
			public double threshold;
			public Node left;
			public Node right;

			public Node()
			{
				this.index = 0;
				this.threshold = 0.0;
				this.left = null;
				this.right = null;
			}
		}

		private class HeapItem
		{
			public int index;
			public double dist;

			public HeapItem(int index, double dist)
			{
				this.index = index;
				this.dist = dist;
			}

			public static bool operator <(HeapItem h1, HeapItem h2)
			{
				return h1.dist < h2.dist;
			}

			public static bool operator >(HeapItem h1, HeapItem h2)
			{
				return h1.dist > h2.dist;
			}
		}

		private Node BuildFromPoints(int lowerIndex, int upperIndex)
		{
			if (upperIndex == lowerIndex)
			{
				return null;
			}

			Node node = new Node();
			node.index = lowerIndex;

			if (upperIndex - lowerIndex > 1)
			{
				Swap(items, lowerIndex, this.rand.Next(lowerIndex + 1, upperIndex));

				int medianIndex = (upperIndex + lowerIndex) / 2;

				nth_element(items, lowerIndex + 1, medianIndex, upperIndex - 1,
							(i1, i2) => System.Collections.Generic.Comparer<double>.Default.Compare(calculateDistance(items[lowerIndex], i1), calculateDistance(items[lowerIndex], i2)));

				node.threshold = this.calculateDistance(this.items[lowerIndex], this.items[medianIndex]);

				node.left = BuildFromPoints(lowerIndex + 1, medianIndex);
				node.right = BuildFromPoints(medianIndex, upperIndex);
			}

			return node;
		}

		private void Search(Node node, T target, int numberOfResults, List<HeapItem> closestHits)
		{
			if (node == null)
			{
				return;
			}

			double dist = this.calculateDistance(this.items[node.index], target);

			/// We found entry with shorter distance
			if (dist < this.tau)
			{
				if (closestHits.Count == numberOfResults)
				{
					// Too many results, remove the first one which has the longest distance
					closestHits.RemoveAt(0);
				}

				// Add new hit
				closestHits.Add(new HeapItem(node.index, dist));

				// Reorder if we have numberOfResults, and set new tau
				if (closestHits.Count == numberOfResults)
				{
					closestHits.Sort((a, b) => b.dist.CompareTo(a.dist));
					this.tau = closestHits[0].dist;
				}
			}

			if (node.left == null && node.right == null)
			{
				return;
			}

			if (dist < node.threshold)
			{
				if (dist - this.tau <= node.threshold)
				{
					this.Search(node.left, target, numberOfResults, closestHits);
				}

				if (dist + this.tau >= node.threshold)
				{
					this.Search(node.right, target, numberOfResults, closestHits);
				}
			}
			else
			{
				if (dist + this.tau >= node.threshold)
				{
					this.Search(node.right, target, numberOfResults, closestHits);
				}

				if (dist - this.tau <= node.threshold)
				{
					this.Search(node.left, target, numberOfResults, closestHits);
				}
			}
		}

		private static void Swap(T[] arr, int index1, int index2)
		{
			T temp = arr[index1];
			arr[index1] = arr[index2];
			arr[index2] = temp;
		}

		private static void nth_element<T>(T[] array, int startIndex, int nthToSeek, int endIndex, Comparison<T> comparison)
		{
			int from = startIndex;
			int to = endIndex;

			// if from == to we reached the kth element
			while (from < to)
			{
				int r = from, w = to;
				T mid = array[(r + w) / 2];

				// stop if the reader and writer meets
				while (r < w)
				{
					if (comparison(array[r], mid) > -1)
					{ // put the large values at the end
						T tmp = array[w];
						array[w] = array[r];
						array[r] = tmp;
						w--;
					}
					else
					{ // the value is smaller than the pivot, skip
						r++;
					}
				}

				// if we stepped up (r++) we need to step one down
				if (comparison(array[r], mid) > 0)
				{
					r--;
				}

				// the r pointer is on the end of the first k elements
				if (nthToSeek <= r)
				{
					to = r;
				}
				else
				{
					from = r + 1;
				}
			}

			return;
		}
	}
}
