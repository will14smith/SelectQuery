using System;
using System.Collections.Generic;

namespace SelectQuery.Evaluation.Slots
{
    internal class SlottedQueryEvaluation
    {
        public SlottedQueryEvaluation(IReadOnlyList<Slot> slots)
        {
            Slots = slots;
        }

        public IReadOnlyList<Slot> Slots { get; }

        public abstract class Slot
        {
            public class ValueSlot : Slot
            {
                public object Value { get; }

                public ValueSlot(object value)
                {
                    Value = value;
                }
            }          
            
            public class SpanSlot : Slot
            {
                public ArraySegment<byte> Buffer { get; }

                public SpanSlot(ArraySegment<byte> buffer)
                {
                    Buffer = buffer;
                }
            }
        }
    }
}