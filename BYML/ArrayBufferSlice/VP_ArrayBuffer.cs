namespace VirtualPhenix.Nintendo64
{
    public class VP_ArrayBuffer : IArrayBufferLike
    {
        public virtual byte[] Buffer { get; private set; }

        public virtual long ByteLength
        {
            get { return Buffer != null ? Buffer.LongLength : 0; }
        }

        public virtual long LongLength
        {
            get { return Buffer.LongLength; }
        }

        public VP_ArrayBuffer()
        {

        }

        public VP_ArrayBuffer(byte[] newBuffer)
        {
            Buffer = newBuffer;
        }

        public VP_ArrayBuffer(IArrayBufferLike alike)
        {
            Buffer = alike.Buffer;
        }

        public VP_ArrayBuffer(long byteLength)
        {
            if (byteLength < 0 || byteLength > int.MaxValue)
                throw new System.ArgumentOutOfRangeException("byteLength must be between 0 and Int32.MaxValue");

            Buffer = new byte[byteLength];
        }

        public static bool IsView(object obj)
        {
            return obj is ISpeciesTypedArray && obj is IArrayBufferLike;
        }

        public object this[long index]
        {
            get
            {
                if (index < 0 || index >= LongLength)
                    throw new System.IndexOutOfRangeException();
                return Buffer[index];
            }
            set
            {
                if (index < 0 || index >= Buffer.LongLength)
                    throw new System.IndexOutOfRangeException();
                Buffer[index] = (byte)value;
            }
        }

        public VP_ArrayBuffer Transfer(long? newByteLength = null)
        {
            // Si se proporciona un nuevo tamaño, recortamos o ampliamos el buffer
            long size = newByteLength ?? Buffer.Length;

            // Creamos un nuevo buffer con el tamaño proporcionado
            byte[] newBuffer = new byte[size];

            // Copiamos los datos del buffer original al nuevo
            System.Array.Copy(Buffer, newBuffer, System.Math.Min(Buffer.Length, size));

            // Desacoplamos el buffer original (en C# esto se logra anulando la referencia)
            Buffer = null;

            // Devolvemos el nuevo ArrayBuffer
            return new VP_ArrayBuffer(newBuffer);
        }

    }
}
