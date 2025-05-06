namespace VirtualPhenix.Nintendo64
{
    public class VP_Uint16Array : VP_Uint16Array<VP_ArrayBuffer>
    {
		public VP_Uint16Array() : base(new VP_ArrayBuffer(), 0)
		{

		}
		public VP_Uint16Array(VP_ArrayBuffer buffer, long byteOffset = 0, long? length = null) : base(buffer, byteOffset, length)
        {

        }
    }
    public class VP_Uint16Array<T> : VP_ArrayBufferView<T> where T : IArrayBufferLike
    {
        public const int BYTES_PER_ELEMENT = 2;

        public VP_Uint16Array(T buffer, long byteOffset = 0, long? length = null)
            : base(buffer, byteOffset, length ?? (buffer.ByteLength - byteOffset)) { }

        public override object this[long index]
        {
            get
            {
                long byteIndex = ByteOffset + index * BYTES_PER_ELEMENT;
                if (index < 0 || byteIndex + 1 >= Buffer.LongLength)
                    throw new System.IndexOutOfRangeException();
                return System.BitConverter.ToUInt16(Buffer, (int)byteIndex);
            }
            set
            {
                long byteIndex = ByteOffset + index * BYTES_PER_ELEMENT;
                if (index < 0 || byteIndex + 1 >= Buffer.LongLength)
                    throw new System.IndexOutOfRangeException();
                byte[] bytes = System.BitConverter.GetBytes((ushort)value);
                Buffer[byteIndex] = bytes[0];
                Buffer[byteIndex + 1] = bytes[1];
            }
        }

        public override string Species
        {
            get { return "Uint16Array"; }
        }
        public override TypedArrayKind Kind
        {
            get { return TypedArrayKind.Uint16; }
        }
        protected override int GetBytesPerElement() => BYTES_PER_ELEMENT;

        protected override object GetElement(long index) => this[index];

        protected override void SetElement(long index, object value)
        {
            this[index] = (ushort)value;
        }

        protected override VP_ArrayBufferView<T> CreateInstance(long length)
        {
            var buffer = new VP_ArrayBuffer(length * BYTES_PER_ELEMENT);
            return (VP_Uint16Array<T>)(object)new VP_Uint16Array<VP_ArrayBuffer>(buffer);
        }

        protected override VP_ArrayBufferView<T> CreateSubarrayInstance(T buffer, long byteOffset, long byteLength)
        {
            return new VP_Uint16Array<T>(buffer, byteOffset, byteLength);
        }
    }
}
