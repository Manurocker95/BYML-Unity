using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VirtualPhenix.Nintendo64
{
    public static class VP_BYMLUtils
    {
        public static void Assert(bool condition, string message = "")
        {
            if (!condition)
            {
                throw new Exception("Assert fail: " + message);
            }
        }

        public static T AssertExists<T>(T value, string name = "")
        {
            if (value == null)
                throw new Exception("Missing object " + name);
            return value;
        }

        public static T Nullify<T>(T value) where T : class
        {
            return value == null ? null : value;
        }

        public static bool IsAligned(long n, long m)
        {
            return (n & (m - 1)) == 0;
        }

        public static int AlignNonPowerOfTwo(int n, int multiple)
        {
            return ((n + multiple - 1) / multiple) * multiple;
        }
        public static string LeftPad(string input, int totalWidth, char paddingChar = '0')
        {
            return input.PadLeft(totalWidth, paddingChar);
        }

        public static string HexZero(long n, int digits)
        {
            return LeftPad(n.ToString("x"), digits);
        }

        public static string HexZero(uint n, int digits)
        {
            return LeftPad(n.ToString("x"), digits);
        }

        public static string HexZero(int n, int digits)
        {
            return LeftPad(n.ToString("x"), digits);
        }

        public static string HexZero0x(int n, int digits = 8)
        {
            if (n < 0)
                return "-0x" + HexZero((uint)(-n), digits);
            else
                return "0x" + HexZero((uint)n, digits);
        }

        public static List<T> Flatten<T>(List<List<T>> list)
        {
            var result = new List<T>();
            foreach (var sublist in list)
                result.AddRange(sublist);
            return result;
        }

        public static int GetBytesPerElement(TypedArrayKind kind)
        {
            switch (kind)
            {
                case TypedArrayKind.Int8:
                case TypedArrayKind.Uint8:
                    return 1;
                case TypedArrayKind.Int16:
                case TypedArrayKind.Uint16:
                    return 2;
                case TypedArrayKind.Int32:
                case TypedArrayKind.Uint32:
                case TypedArrayKind.Float32:
                    return 4;
                case TypedArrayKind.Float64:
                    return 8;
                default:
                    return 1;
            }
        }

        public static int BisectRight<T>(List<T> list, T item, Comparison<T> comparer)
        {
            int lo = 0, hi = list.Count;
            while (lo < hi)
            {
                int mid = lo + ((hi - lo) >> 1);
                if (comparer(item, list[mid]) < 0)
                    hi = mid;
                else
                    lo = mid + 1;
            }
            return lo;
        }

        public static void SpliceBisectRight<T>(List<T> list, T item, Comparison<T> comparer)
        {
            int idx = BisectRight(list, item, comparer);
            list.Insert(idx, item);
        }

        public static int SetBitFlagEnabled(int value, int mask, bool enabled)
        {
            if (enabled)
                return value | mask;
            else
                return value & ~mask;
        }
        public static int ArrayRemove<T>(List<T> list, T item)
        {
            int idx = list.IndexOf(item);
            Assert(idx >= 0, "Item not found in array");
            list.RemoveAt(idx);
            return idx;
        }

        public static int ArrayRemoveIfExists<T>(List<T> list, T item)
        {
            int idx = list.IndexOf(item);
            if (idx >= 0)
                list.RemoveAt(idx);
            return idx;
        }
        public static int Mod(int a, int b)
        {
            return (a % b + b) % b;
        }

        public static void EnsureInList<T>(List<T> list, T value)
        {
            if (!list.Contains(value))
                list.Add(value);
        }
    }
}
