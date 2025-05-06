namespace VirtualPhenix.Nintendo64
{
    public class VP_Int8Array : VP_Int8Array<VP_ArrayBuffer>
    {
        public VP_Int8Array(VP_ArrayBuffer buffer, long byteOffset = 0, long? length = null) : base(buffer, byteOffset, length)
        {

        }
    }
    public class VP_Int8Array<T> : VP_ArrayBufferView<T> where T : IArrayBufferLike
    {
        public const int BYTES_PER_ELEMENT = 1;

        public VP_Int8Array(T buffer, long byteOffset = 0, long? length = null)
            : base(buffer, byteOffset, length ?? (buffer.ByteLength - byteOffset)) { }

        public override object this[long index]
        {
            get
            {
                if (index < 0 || index >= ByteLength / BYTES_PER_ELEMENT)
                    throw new System.IndexOutOfRangeException();
                return (sbyte)Buffer[ByteOffset + index];
            }
            set
            {
                if (index < 0 || index >= ByteLength / BYTES_PER_ELEMENT)
                    throw new System.IndexOutOfRangeException();
                Buffer[ByteOffset + index] = (byte)value;
            }
        }

        public override string Species
        {
            get { return "Int8Array"; }
        }
        public override TypedArrayKind Kind
        {
            get { return TypedArrayKind.Int8; }
        }

        protected override int GetBytesPerElement() => BYTES_PER_ELEMENT;

        protected override object GetElement(long index) => this[index];

        protected override void SetElement(long index, object value)
        {
            this[index] = (sbyte)value;
        }

        protected override VP_ArrayBufferView<T> CreateInstance(long length)
        {
            var buffer = new VP_ArrayBuffer(length * BYTES_PER_ELEMENT);
            return (VP_Int8Array<T>)(object)new VP_Int8Array<VP_ArrayBuffer>(buffer);
        }

        protected override VP_ArrayBufferView<T> CreateSubarrayInstance(T buffer, long byteOffset, long byteLength)
        {
            return new VP_Int8Array<T>(buffer, byteOffset, byteLength);
        }
    }
}
