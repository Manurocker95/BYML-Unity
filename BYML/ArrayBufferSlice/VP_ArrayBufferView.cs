using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VirtualPhenix.Nintendo64
{
    public abstract class VP_ArrayBufferView<T> : IArrayBufferLike, ISpeciesTypedArray where T : IArrayBufferLike
    {
        public virtual T BufferSource { get; protected set; }

        public virtual byte[] Buffer
        {
            get { return BufferSource.Buffer; }
        }

        public virtual long ByteOffset { get; protected set; }
        public virtual long ByteLength { get; protected set; }
        public virtual long Length { get { return ByteLength / GetBytesPerElement(); } }
        public virtual long LongLength { get { return ByteLength; } }

        protected VP_ArrayBufferView(T buffer, long byteOffset, long byteLength)
        {
            if (buffer == null)
                throw new System.ArgumentNullException("buffer");

            if (byteOffset < 0 || byteLength < 0 || (byteOffset + byteLength) > buffer.ByteLength)
                throw new System.ArgumentOutOfRangeException("Invalid byteOffset or byteLength");

            this.BufferSource = buffer;
            this.ByteOffset = byteOffset;
            this.ByteLength = byteLength;
        }

        public abstract string Species { get; }
        public abstract TypedArrayKind Kind { get; }
        // Este indexador debe ser implementado por cada subclase
        public abstract object this[long index] { get; set; }

        public virtual VP_ArrayBufferView<T> CopyWithin(long target, long start, long? end = null)
        {
            long length = ByteLength / GetBytesPerElement();
            long to = target < 0 ? length + target : target;
            long from = start < 0 ? length + start : start;
            long final = end.HasValue ? (end.Value < 0 ? length + end.Value : end.Value) : length;
            long count = System.Math.Min(final - from, length - to);

            if (count <= 0)
                return this;

            // Copia temporal
            var temp = new object[count];
            for (long i = 0; i < count; i++)
                temp[i] = GetElement(from + i);

            for (long i = 0; i < count; i++)
                SetElement(to + i, temp[i]);

            return this;
        }
      
        protected abstract int GetBytesPerElement();
        protected abstract object GetElement(long index);
        protected abstract void SetElement(long index, object value);

        public virtual bool Every(System.Func<object, long, VP_ArrayBufferView<T>, bool> predicate)
        {
            long length = ByteLength / GetBytesPerElement();

            for (long i = 0; i < length; i++)
            {
                object element = GetElement(i);
                if (!predicate(element, i, this))
                    return false;
            }

            return true;
        }

        public virtual VP_ArrayBufferView<T> Fill(object value, long? start = null, long? end = null)
        {
            long length = ByteLength / GetBytesPerElement();
            long from = start.HasValue ? (start.Value < 0 ? length + start.Value : start.Value) : 0;
            long to = end.HasValue ? (end.Value < 0 ? length + end.Value : end.Value) : length;

            from = System.Math.Max(0, from);
            to = System.Math.Min(length, to);

            for (long i = from; i < to; i++)
                SetElement(i, value);

            return this;
        }

        public virtual VP_ArrayBufferView<T> Filter(System.Func<object, long, VP_ArrayBufferView<T>, bool> predicate)
        {
            long length = ByteLength / GetBytesPerElement();
            var results = new System.Collections.Generic.List<object>();

            for (long i = 0; i < length; i++)
            {
                var value = GetElement(i);
                if (predicate(value, i, this))
                    results.Add(value);
            }

            var output = CreateInstance(results.Count);

            for (long i = 0; i < results.Count; i++)
                output.SetElement(i, results[(int)i]);

            return output;
        }

        protected abstract VP_ArrayBufferView<T> CreateInstance(long length);

        public virtual object Find(System.Func<object, long, VP_ArrayBufferView<T>, bool> predicate)
        {
            long length = ByteLength / GetBytesPerElement();

            for (long i = 0; i < length; i++)
            {
                var value = GetElement(i);
                if (predicate(value, i, this))
                    return value;
            }

            return null; // En lugar de `undefined`, en C# usamos `null`
        }

        public virtual long FindIndex(System.Func<object, long, VP_ArrayBufferView<T>, bool> predicate)
        {
            long length = ByteLength / GetBytesPerElement();

            for (long i = 0; i < length; i++)
            {
                var value = GetElement(i);
                if (predicate(value, i, this))
                    return i;
            }

            return -1;
        }

        public virtual void ForEach(System.Action<object, long, VP_ArrayBufferView<T>> callback)
        {
            long length = ByteLength / GetBytesPerElement();

            for (long i = 0; i < length; i++)
            {
                var value = GetElement(i);
                callback(value, i, this);
            }
        }

        public virtual long IndexOf(object searchElement, long fromIndex = 0)
        {
            long length = ByteLength / GetBytesPerElement();
            if (fromIndex < 0) fromIndex = 0;

            for (long i = fromIndex; i < length; i++)
            {
                var value = GetElement(i);
                if (Equals(value, searchElement))
                    return i;
            }

            return -1;
        }

        public virtual string Join(string separator = ",")
        {
            long length = ByteLength / GetBytesPerElement();
            if (length == 0) return string.Empty;

            var builder = new System.Text.StringBuilder();

            for (long i = 0; i < length; i++)
            {
                if (i > 0)
                    builder.Append(separator);
                builder.Append(GetElement(i)?.ToString());
            }

            return builder.ToString();
        }

        public virtual long LastIndexOf(object searchElement, long? fromIndex = null)
        {
            long length = ByteLength / GetBytesPerElement();
            long start = fromIndex.HasValue ? fromIndex.Value : length - 1;

            if (start >= length) start = length - 1;
            if (start < 0) return -1;

            for (long i = start; i >= 0; i--)
            {
                var value = GetElement(i);
                if (Equals(value, searchElement))
                    return i;
            }

            return -1;
        }

        public virtual VP_ArrayBufferView<T> Map(System.Func<object, long, VP_ArrayBufferView<T>, object> callback)
        {
            long length = Length;
            var result = CreateInstance(length);

            for (long i = 0; i < length; i++)
            {
                var input = GetElement(i);
                var output = callback(input, i, this);
                result.SetElement(i, output);
            }

            return result;
        }

        public virtual object Reduce(System.Func<object, object, long, VP_ArrayBufferView<T>, object> callback)
        {
            long length = Length;
            if (length == 0)
                throw new System.InvalidOperationException("Reduce of empty array with no initial value");

            object accumulator = GetElement(0);

            for (long i = 1; i < length; i++)
            {
                accumulator = callback(accumulator, GetElement(i), i, this);
            }

            return accumulator;
        }

        public virtual object Reduce(System.Func<object, object, long, VP_ArrayBufferView<T>, object> callback, object initialValue)
        {
            long length = Length;
            object accumulator = initialValue;

            for (long i = 0; i < length; i++)
            {
                var current = GetElement(i);
                accumulator = callback(accumulator, current, i, this);
            }

            return accumulator;
        }

        public virtual U Reduce<U>(System.Func<U, object, long, VP_ArrayBufferView<T>, U> callback, U initialValue)
        {
            long length = Length;
            U accumulator = initialValue;

            for (long i = 0; i < length; i++)
            {
                accumulator = callback(accumulator, GetElement(i), i, this);
            }

            return accumulator;
        }

        public virtual object ReduceRight(System.Func<object, object, long, VP_ArrayBufferView<T>, object> callback)
        {
            long length = Length;
            if (length == 0)
                throw new System.InvalidOperationException("ReduceRight of empty array with no initial value");

            object accumulator = GetElement(length - 1);

            for (long i = length - 2; i >= 0; i--)
            {
                var current = GetElement(i);
                accumulator = callback(accumulator, current, i, this);
            }

            return accumulator;
        }

        public virtual object ReduceRight(System.Func<object, object, long, VP_ArrayBufferView<T>, object> callback, object initialValue)
        {
            long length = Length;
            object accumulator = initialValue;

            for (long i = length - 1; i >= 0; i--)
            {
                var current = GetElement(i);
                accumulator = callback(accumulator, current, i, this);
            }

            return accumulator;
        }

        public virtual U ReduceRight<U>(System.Func<U, object, long, VP_ArrayBufferView<T>, U> callback, U initialValue)
        {
            long length = Length;
            U accumulator = initialValue;

            for (long i = length - 1; i >= 0; i--)
            {
                accumulator = callback(accumulator, GetElement(i), i, this);
            }

            return accumulator;
        }

        public virtual VP_ArrayBufferView<T> Reverse()
        {
            long length = Length;
            long mid = length / 2;

            for (long i = 0; i < mid; i++)
            {
                var a = GetElement(i);
                var b = GetElement(length - 1 - i);

                SetElement(i, b);
                SetElement(length - 1 - i, a);
            }

            return this;
        }

        public virtual void Set(VP_ArrayLike array, long? offset = 0)
        {
            var buffer = Buffer;

            // Si el offset es mayor que el tamaño actual del buffer, expandimos el buffer
            if (offset + array.Length > buffer.Length)
            {
                // Creamos un nuevo buffer más grande que el original
                byte[] newBuffer = new byte[(int)offset + array.Length];

                // Copiamos el contenido original del buffer al nuevo buffer
                System.Array.Copy(Buffer, newBuffer, Buffer.Length);

                // Actualizamos el buffer en VP_DataView con el nuevo buffer más grande
                BufferSource = (T)(object)new VP_ArrayBuffer(newBuffer);

                // Actualizamos el buffer para que apunte al nuevo buffer expandido
                buffer = newBuffer;
            }

            // Iteramos sobre el array y escribimos los valores en el buffer
            for (int i = 0; i < array.Length; i++)
            {
                buffer[(int)offset + i] = (byte)(array.N[i] & 0xFF);  // Solo almacenamos el byte menos significativo
            }

            // Escribimos el buffer actualizado de vuelta a la vista
            BufferSource = (T)(object)new VP_ArrayBuffer(buffer);
        }

        public virtual void Set(System.Collections.Generic.IEnumerable<object> array, long offset = 0)
        {
            if (offset < 0)
                throw new System.ArgumentOutOfRangeException(nameof(offset));

            long index = offset;

            foreach (var item in array)
            {
                if (index >= Length)
                    throw new System.IndexOutOfRangeException("Set operation exceeds array bounds.");

                SetElement(index, item);
                index++;
            }
        }

        public virtual VP_ArrayBufferView<T> Slice(long? start = null, long? end = null)
        {
            long length = Length;

            long from = start.HasValue ? (start.Value < 0 ? length + start.Value : start.Value) : 0;
            long to = end.HasValue ? (end.Value < 0 ? length + end.Value : end.Value) : length;

            from = System.Math.Max(0, from);
            to = System.Math.Min(length, to);
            long sliceLength = System.Math.Max(to - from, 0);

            var result = CreateInstance(sliceLength);

            for (long i = 0; i < sliceLength; i++)
                result.SetElement(i, GetElement(from + i));

            return result;
        }

        public virtual bool Some(System.Func<object, long, VP_ArrayBufferView<T>, bool> predicate)
        {
            long length = Length;

            for (long i = 0; i < length; i++)
            {
                if (predicate(GetElement(i), i, this))
                    return true;
            }

            return false;
        }

        public virtual VP_ArrayBufferView<T> Sort(System.Comparison<object> compareFn = null)
        {
            long length = Length;
            var elements = new System.Collections.Generic.List<object>((int)length);

            for (long i = 0; i < length; i++)
                elements.Add(GetElement(i));

            // Ordenar
            if (compareFn != null)
                elements.Sort(compareFn);
            else
                elements.Sort((a, b) => System.Collections.Comparer.DefaultInvariant.Compare(a, b));

            // Volver a escribir al buffer
            for (long i = 0; i < length; i++)
                SetElement(i, elements[(int)i]);

            return this;
        }

        public virtual VP_ArrayBufferView<T> Subarray(long? begin = null, long? end = null)
        {
            long length = Length;

            long from = begin.HasValue ? (begin.Value < 0 ? length + begin.Value : begin.Value) : 0;
            long to = end.HasValue ? (end.Value < 0 ? length + end.Value : end.Value) : length;

            from = System.Math.Max(0, from);
            to = System.Math.Min(length, to);
            long newLength = System.Math.Max(to - from, 0);

            long byteOffset = ByteOffset + from * GetBytesPerElement();
            long byteLength = newLength * GetBytesPerElement();

            return CreateSubarrayInstance(BufferSource, byteOffset, byteLength);
        }

        protected abstract VP_ArrayBufferView<T> CreateSubarrayInstance(T buffer, long byteOffset, long byteLength);

        public virtual VP_ArrayBufferView<T> ValueOf()
        {
            return this;
        }
    }
}
