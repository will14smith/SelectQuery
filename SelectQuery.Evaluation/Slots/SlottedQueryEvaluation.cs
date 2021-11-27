using System;
using System.Buffers;
using System.Collections.Generic;

namespace SelectQuery.Evaluation.Slots
{
    internal class SlottedQueryEvaluation : IDisposable
    {
        private readonly Slot[] _slots;

        public SlottedQueryEvaluation(Slot[] slots)
        {
            _slots = slots;
        }

        public IReadOnlyList<Slot> Slots => _slots;

        public struct Slot
        {
            public SlotType Type { get; private set; }
            public object Value { get; private set; }
            public ArraySegment<byte> Buffer { get; private set; }

            public static Slot CreateValue(object value) => new Slot { Type = SlotType.Value, Value = value };
            public static Slot CreateSpan(ArraySegment<byte> value) => new Slot { Type = SlotType.Span, Buffer = value };
        }

        public enum SlotType
        {
            Empty,
            Value,
            Span,
        }
        
        public void Dispose()
        {
            ArrayPool<Slot>.Shared.Return(_slots);
        }
    }
}