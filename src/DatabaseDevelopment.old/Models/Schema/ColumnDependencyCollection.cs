using System;
using System.Collections;
using System.Collections.Generic;

namespace DatabaseDevelopment.Models.Schema
{
    public class ColumnDependencyCollection : IList<ColumnDependency>
    {
        private List<ColumnDependency> collection;

        public ColumnDependencyCollection()
        {
            collection = new List<ColumnDependency>();
        }

        public ColumnDependency this[int index] { get => collection[index]; set => collection[index] = value; }

        public int Count => collection.Count;

        public bool IsReadOnly => false;

        public void Add(ColumnDependency item)
        {
            collection.Add(item);
        }

        public void Clear()
        {
            collection.Clear();
        }

        public bool Contains(ColumnDependency item)
        {
            return collection.Contains(item);
        }

        public void CopyTo(ColumnDependency[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public IEnumerator<ColumnDependency> GetEnumerator()
        {
            return collection.GetEnumerator();
        }

        public int IndexOf(ColumnDependency item)
        {
            throw new NotImplementedException();
        }

        public void Insert(int index, ColumnDependency item)
        {
            throw new NotImplementedException();
        }

        public bool Remove(ColumnDependency item)
        {
            throw new NotImplementedException();
        }

        public void RemoveAt(int index)
        {
            throw new NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }

        public override Boolean Equals(Object obj)
        {
            if (ReferenceEquals(null, obj))
                return false;

            if (ReferenceEquals(this, obj))
                return true;

            if (obj.GetType() != GetType())
                return false;

            var cdc = obj as ColumnDependencyCollection;

            if (cdc.Count != collection.Count)
                return false;

            foreach (var item in cdc)
            {
                if (!collection.Contains(item))
                    return false;
            }

            return true;
        }
    }
}