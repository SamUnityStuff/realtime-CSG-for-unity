using System;
using System.Collections.Generic;
using UnityEngine;
using RealtimeCSG.Components;
using System.Runtime.InteropServices;

namespace RealtimeCSG
{
	internal static class HierarchyItemExtension
	{
		internal static bool FindSiblingIndex(this HierarchyItem self, Transform searchTransform, int siblingIndex, int searchTransformID, out int index)
		{
			if (self.ChildNodes == null ||
				self.ChildNodes.Count == 0)
			{
				index = 0;
				return false;
			}
			Span<HierarchyItem> childNodesSpan = self.ChildNodes.AsSpanUnchecked();
			int self_ChildNodes_Count = childNodesSpan.Length;

			var checkIndex		 = siblingIndex;
			var last			 = self_ChildNodes_Count - 1;
			var currentLoopCount = HierarchyItem.CurrentLoopCount;
			
			HierarchyItem self_ChildNodes_Last = childNodesSpan[last];
			if (self_ChildNodes_Last.LastLoopCount != currentLoopCount)
			{
				if (self_ChildNodes_Last.Transform != null && self_ChildNodes_Last.Transform)
					self_ChildNodes_Last.CachedTransformSiblingIndex = self_ChildNodes_Last.Transform.GetSiblingIndex();
				else
					self_ChildNodes_Last.CachedTransformSiblingIndex = -1;
				self_ChildNodes_Last.LastLoopCount = currentLoopCount;
			}
			if (self_ChildNodes_Last.CachedTransformSiblingIndex < checkIndex)
			{
				index = self_ChildNodes_Count;
				return false;
			}

			// continue searching while [imin,imax] is not empty
			var imin = 0;
			var imax = last;
			while (imin <= imax)
			{
				// calculate the midpoint for roughly equal partition
				var imid = (imin + imax) / 2;

				HierarchyItem self_ChildNodes_imid = childNodesSpan[imid];
				if (self_ChildNodes_imid.LastLoopCount != currentLoopCount)
				{
					if (self_ChildNodes_imid.Transform != null && self_ChildNodes_imid.Transform)
						self_ChildNodes_imid.CachedTransformSiblingIndex = self_ChildNodes_imid.Transform.GetSiblingIndex();
					else
						self_ChildNodes_imid.CachedTransformSiblingIndex = -1;
					self_ChildNodes_imid.LastLoopCount = currentLoopCount;
				}
				var midKey2 = self_ChildNodes_imid.CachedTransformSiblingIndex;

				// determine which subarray to search
				if (midKey2 < checkIndex)
				{
					// change min index to search upper subarray
					imin = imid + 1;
				} else
				{
					if (midKey2 == checkIndex)
					{
						// key found at index imid

						index = imid;
						return (searchTransformID == self_ChildNodes_imid.TransformID);
					}
					if (imid > 0)
					{
						if (childNodesSpan[imid - 1].LastLoopCount != currentLoopCount)
						{
							if (childNodesSpan[imid - 1].Transform != null && childNodesSpan[imid - 1].Transform)
								childNodesSpan[imid - 1].CachedTransformSiblingIndex = childNodesSpan[imid - 1].Transform.GetSiblingIndex();
							else
                                childNodesSpan[imid - 1].CachedTransformSiblingIndex = -1;
                            childNodesSpan[imid - 1].LastLoopCount = currentLoopCount;
						}
						var midKey1 = childNodesSpan[imid - 1].CachedTransformSiblingIndex;

						if (midKey1 < checkIndex)
						{
							// key found at index imid
							index = imid;
							return (searchTransformID == self_ChildNodes_imid.TransformID);
						}
					}
					// change max index to search lower subarray
					imax = imid - 1;
				}
			}

			index = 0;
			return false;
		}

		internal static bool FindSiblingIndex(this HierarchyItem self, HierarchyItem item, out int index)
		{
			if (self.ChildNodes == null ||
				self.ChildNodes.Count == 0)
			{
				index = 0;
				return false;
			}

			for (var i = 0; i < self.ChildNodes.Count; i++)
			{
				if (item != self.ChildNodes[i])
					continue;

				index = i;
				return true;
			}

			index = 0;
			return false;
		}

		internal static bool AddChildItem(this HierarchyItem self, HierarchyItem item)
		{
			int index;
			if (self.FindSiblingIndex(item, out index))
				// The transform is already in the array?
				return false;

			var currentLoopCount = HierarchyItem.CurrentLoopCount;
			if (item.LastLoopCount != currentLoopCount)
			{
				item.CachedTransformSiblingIndex = item.Transform.GetSiblingIndex();
				item.LastLoopCount = currentLoopCount;
			}

			if (self.FindSiblingIndex(item.Transform, item.CachedTransformSiblingIndex, item.TransformID, out index))
			{
				return false;
			}

			// make sure item is added in the correct position within the array
			self.ChildNodes.Insert(index, item);
			item.SiblingIndex = index;
			/*
			bool childrenModified = false;
			for (int i = index + 1; i < ChildNodes.Length; i++)
			{
				if (ChildNodes[i].SiblingIndex != i)
				{
					ChildNodes[i].SiblingIndex = i;
					childrenModified = true;
				}
			}
			if (childrenModified)*/
			{
				var parentData = self as ParentNodeData;
				if (parentData != null)
					parentData.ChildrenModified = true;
			}
			
			Debug.Assert(self.ChildNodes[index] == item);
			return true;
		}

		internal static bool RemoveChildItem(this HierarchyItem self, HierarchyItem item)
		{
			int index;
			if (!self.FindSiblingIndex(item, out index))
				// The transform is not in the array?
				return false;

			// make sure item is removed from the array
			self.ChildNodes.RemoveAt(index);
			//item.siblingIndex = -1;
			return true;
		}

		public static IEnumerable<HierarchyItem> IterateChildrenDeep(this HierarchyItem self)
		{
			for (var i = 0; i < self.ChildNodes.Count; i++)
			{
				var childNode = self.ChildNodes[i];
				yield return childNode;

				if (childNode.ChildNodes.Count == 0)
					continue;

				foreach (var item in childNode.IterateChildrenDeep())
				{
					yield return item;
				}
			}
		}

		public static void Init(this HierarchyItem self, CSGNode node, Int32 nodeID)
		{
			self.Transform		= node.transform;
			self.TransformID	= node.transform.GetInstanceID();
			self.NodeID			= nodeID;
		}
	}
}
