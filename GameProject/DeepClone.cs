﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game
{
    public static class DeepClone
    {
        /// <summary>Deep clone instance.</summary>
        /// <returns>Set of shallow cloned instances.</returns>
        public static Dictionary<IDeepClone, IDeepClone> Clone(IDeepClone toClone)
        {
            List<IDeepClone> list = new List<IDeepClone>();
            list.Add(toClone);
            return Clone(list);
        }

        public static Dictionary<IDeepClone, IDeepClone> Clone(HashSet<IDeepClone> toClone)
        {
            return Clone(toClone.ToList());
        }

        /// <summary>Deep clone list of instances.</summary>
        /// <returns>Set of shallow cloned instances.</returns>
        public static Dictionary<IDeepClone, IDeepClone> Clone(List<IDeepClone> toClone)
        {
            HashSet<IDeepClone> cloneHash = new HashSet<IDeepClone>();
            GetReferences(toClone, cloneHash);

            Dictionary<IDeepClone, IDeepClone> cloneMap = new Dictionary<IDeepClone, IDeepClone>();
            foreach (IDeepClone original in cloneHash)
            {
                IDeepClone clone = original.ShallowClone();
                Debug.Assert(clone.GetType() == original.GetType(), "Type of cloned instance must match type of original instance.");
                cloneMap.Add(original, clone);
            }
            Debug.Assert(cloneMap.Count == cloneHash.Count);
            ReadOnlyDictionary<IDeepClone, IDeepClone> readOnlyCloneMap = new ReadOnlyDictionary<IDeepClone, IDeepClone>(cloneMap);
            foreach (IDeepClone clone in readOnlyCloneMap.Values)
            {
                clone.UpdateRefs(readOnlyCloneMap);
            }
            //return new HashSet<IDeepClone>(readOnlyCloneMap.Values);
            return cloneMap;
        }

        private static void GetReferences(List<IDeepClone> entities, HashSet<IDeepClone> cloneList)
        {
            foreach (IDeepClone e in entities)
            {
                GetReferences(e, cloneList);
            }
        }

        private static void GetReferences(IDeepClone entity, HashSet<IDeepClone> cloneList)
        {
            cloneList.Add(entity);
            foreach (IDeepClone cloneable in entity.GetCloneableRefs())
            {
                GetReferences(cloneable, cloneList);
            }
        }
    }
}