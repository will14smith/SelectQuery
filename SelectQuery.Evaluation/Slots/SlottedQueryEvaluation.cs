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

        public class Slot
        {
            public Slot(object value)
            {
                Value = value;
            }

            public object Value { get; }
        }
    }
}