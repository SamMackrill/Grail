using System;
using System.Collections.Generic;

namespace Grail.Model
{
    public interface IKeyed : IComparable<IKeyed>, IComparable, IEqualityComparer<IKeyed>, IEquatable<IKeyed>
    {
        string Key { get; }
    }

    public abstract class Keyed : IKeyed
    {
        public abstract string Key { get; }

        public bool Equals(IKeyed other)
        {
            return other?.Key == Key;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as IKeyed);
        }


        public bool Equals(IKeyed x, IKeyed y)
        {
            return (x != null && y != null) && x.Key == y.Key;
        }

        public int GetHashCode(IKeyed obj)
        {
            return obj.Key.GetHashCode();
        }

        public override int GetHashCode()
        {
            return Key.GetHashCode();
        }

        public int CompareTo(object obj)
        {
            return CompareTo(obj as IKeyed);
        }

        public int CompareTo(IKeyed other)
        {
            return string.CompareOrdinal(Key, other?.Key);
        }
    }
}