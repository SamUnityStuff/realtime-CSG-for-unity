using System;
using System.Collections.Generic;
using UnityEngine;
using InternalRealtimeCSG;
using UnityEngine.Rendering;

namespace RealtimeCSG
{
#if UNITY_EDITOR
	[Serializable]
	public class HierarchyItem
	{
        public bool             TransformInitialized = false;
		public Transform        Transform;
		public int              TransformID;
		public HierarchyItem    Parent;
		public int              PrevSiblingIndex    = -1;
		public int              SiblingIndex        = -1;
		public Int32            NodeID              = Components.CSGNode.InvalidNodeID;
		public List<HierarchyItem>  ChildNodes          = new ();


		public int LastLoopCount = -1;
		public int CachedTransformSiblingIndex;

		public static int CurrentLoopCount { get; set; }
		
		public virtual void Reset()
		{
			Transform			= null;
			TransformID			= 0;
			Parent				= null;
			PrevSiblingIndex	= -1;
			SiblingIndex		= -1;
			NodeID				= Components.CSGNode.InvalidNodeID;
			if (ChildNodes == null) { ChildNodes = new(); }
			ChildNodes.Clear();

			LastLoopCount		= -1;
			CachedTransformSiblingIndex = 0;
		}
	}
#endif
}