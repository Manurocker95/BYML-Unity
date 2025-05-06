namespace VirtualPhenix.Nintendo64
{
    public class VP_Int64Array : VP_Int64Array<VP_ArrayBuffer>
    {
		public VP_Int64Array() : base(new VP_ArrayBuffer(), 0)
		{

		}
		public VP_Int64Array(VP_ArrayBuffer buffer, long byteOffset = 0, long? length = null) : base(buffer, byteOffset, length)
        {

        }
    }

    public class VP_Int64Array<T> : VP_ArrayBufferView<T> where T : IArrayBufferLike
    {
        public const int BYTES_PER_ELEMENT = 8;

        public VP_Int64Array(T buffer, long byteOffset = 0, long? length = null)
            : base(buffer, byteOffset, length ?? (buffer.ByteLength - byteOffset)) { }

        public override object this[long index]
        {
            get
            {
                long byteIndex = ByteOffset + index * BYTES_PER_ELEMENT;
                if (index < 0 || byteIndex + 7 >= Buffer.LongLength)
                    throw new System.IndexOutOfRangeException();
                return System.BitConverter.ToInt64(Buffer, (int)byteIndex);
            }
            set
            {
                long byteIndex = ByteOffset + index * BYTES_PER_ELEMENT;
                if (index < 0 || byteIndex + 7 >= Buffer.LongLength)
                    throw new System.IndexOutOfRangeException();
                byte[] bytes = System.BitConverter.GetBytes((long)value);
                Buffer[byteIndex + 0] = bytes[0];
                Buffer[byteIndex + 1] = bytes[1];
                Buffer[byteIndex + 2] = bytes[2];
                Buffer[byteIndex + 3] = bytes[3];
                Buffer[byteIndex + 4] = bytes[4];
                Buffer[byteIndex + 5] = bytes[5];
                Buffer[byteIndex + 6] = bytes[6];
                Buffer[byteIndex + 7] = bytes[7];
            }
        }

        public override string Species
        {
            get { return "Int64Array"; }
        }
        public override TypedArrayKind Kind
        {
            get { return TypedArrayKind.Int64; }
        }
        protected override int GetBytesPerElement() => BYTES_PER_ELEMENT;

        protected override object GetElement(long index) => this[index];

        protected override void SetElement(long index, object value)
        {
            this[index] = (long)value;
        }

        protected override VP_ArrayBufferView<T> CreateInstance(long length)
        {
            var buffer = new VP_ArrayBuffer(length * BYTES_PER_ELEMENT);
            return (VP_Int64Array<T>)(object)new VP_Int64Array<VP_ArrayBuffer>(buffer);
        }

        protected override VP_ArrayBufferView<T> CreateSubarrayInstance(T buffer, long byteOffset, long byteLength)
        {
            return new VP_Int64Array<T>(buffer, byteOffset, byteLength);
        }
    }
}
