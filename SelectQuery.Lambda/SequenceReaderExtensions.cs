using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace SelectQuery.Lambda
{
    public static class SequenceReaderExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static unsafe bool TryRead<T>(ref this ReadOnlySequence<byte> reader, out T value) where T : unmanaged
        {
            var span = reader.First.Span;
            if (span.Length < sizeof(T))
                return TryReadMultisegment(ref reader, out value);

            value = Unsafe.ReadUnaligned<T>(ref MemoryMarshal.GetReference(span));
            reader = reader.Slice(sizeof(T));
            return true;
        }

        private static unsafe bool TryReadMultisegment<T>(ref ReadOnlySequence<byte> reader, out T value) where T : unmanaged
        {
            Debug.Assert(reader.First.Span.Length < sizeof(T));

            // Not enough data in the current segment, try to peek for the data we need.
            T buffer = default;
            var tempSpan = new Span<byte>(&buffer, sizeof(T));

            if (reader.Length < sizeof(T))
            {
                value = default;
                return false;
            }

            reader.CopyTo(tempSpan);

            value = Unsafe.ReadUnaligned<T>(ref MemoryMarshal.GetReference(tempSpan));
            reader = reader.Slice(sizeof(T));
            return true;
        }

        
        public static bool TryReadBigEndian(ref this ReadOnlySequence<byte> reader, out int value)
        {
            return BitConverter.IsLittleEndian 
                ? TryReadReverseEndianness(ref reader, out value) 
                : reader.TryRead(out value);
        }

        private static bool TryReadReverseEndianness(ref ReadOnlySequence<byte> reader, out int value)
        {
            if (!reader.TryRead(out value))
            {
                return false;
            }

            value = BinaryPrimitives.ReverseEndianness(value);
            return true;

        }
    }
}
