using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Utf8Json;

namespace SelectQuery.Evaluation.Slots
{
    internal class SlottedQueryReader
    {
        private readonly SlottedQuery _query;
        private readonly SlottedQueryEvaluation.Slot[] _slots;
        public IReadOnlyList<SlottedQueryEvaluation.Slot> Slots => _slots;
        
        public SlottedQueryReader(SlottedQuery query)
        {
            _query = query;
            _slots = new SlottedQueryEvaluation.Slot[query.NumberOfSlots];
        }
        
        public void Read(ref JsonReader reader)
        {
            var node = _query.SlotTree;

            ReadNode(ref reader, new [] { node }, false);
        }

        private object ReadNode(ref JsonReader reader, IReadOnlyCollection<SlottedExpressionTree> nodes, bool needValue)
        {
            // if node is a non-passthrough slot, capture whole object to output
            // if node is a passthrough slot, capture offsets to object
            // if not, read/skip as needed and recurse into children

            needValue |= NeedValue(nodes);

            var canSkipBlock = CanSkipReading(nodes);
            if (canSkipBlock && !needValue)
            {
                reader.ReadNextBlock();
                return null;
            }
            
            var startOffset = reader.GetCurrentOffsetUnsafe();

            var token = reader.GetCurrentJsonToken();
            var value = token switch
            {
                JsonToken.BeginObject => ReadObject(ref reader, nodes, needValue),
                JsonToken.BeginArray => ReadArray(ref reader, nodes, needValue),
                JsonToken.Number => ReadNumber(ref reader),
                JsonToken.String => reader.ReadString(),
                JsonToken.True or JsonToken.False => reader.ReadBoolean(),
                JsonToken.Null => ReadNull(ref reader),
                _ => throw new ArgumentOutOfRangeException($"unexpected token: {token} at {reader.GetCurrentOffsetUnsafe()}")
            };

            foreach (var node in nodes)
            {
                if (!node.Slot.IsSome)
                {
                    continue;
                }
                
                if (node.Passthrough)
                {
                    var buffer = new ArraySegment<byte>(reader.GetBufferUnsafe(), startOffset, checked(reader.GetCurrentOffsetUnsafe() - startOffset));
                    _slots[node.Slot.AsT0] = new SlottedQueryEvaluation.Slot.SpanSlot(buffer);
                }
                else
                {
                    _slots[node.Slot.AsT0] = new SlottedQueryEvaluation.Slot.ValueSlot(value);
                }
            }

            return value;
        }

        private static bool CanSkipReading(IEnumerable<SlottedExpressionTree> nodes) => nodes.All(node => node.Slot.IsNone && !node.HasChildren);
        private static bool NeedValue(IEnumerable<SlottedExpressionTree> nodes) => nodes.Any(node => node.Slot.IsSome && !node.Passthrough);

        private object ReadObject(ref JsonReader reader, IReadOnlyCollection<SlottedExpressionTree> nodes, bool needValue)
        {
            var value = needValue ? new Dictionary<string, object>() : null;

            reader.ReadIsBeginObjectWithVerify();

            do
            {
                var prop = reader.ReadPropertyName();

                var children = nodes.GetChildren(prop);
                if (children.Count > 0)
                {
                    var childValue = ReadNode(ref reader, children, needValue);
                    if (needValue)
                    {
                        value[prop] = childValue;
                    }
                }
                else if (needValue)
                {
                    value[prop] = ReadNode(ref reader, null, true);
                } 
                else
                {
                    reader.ReadNextBlock();
                }
                
            } while (reader.ReadIsValueSeparator());
            
            reader.ReadIsEndObjectWithVerify();

            return value;
        }
        
        private object ReadArray(ref JsonReader reader, IReadOnlyCollection<SlottedExpressionTree> nodes, bool needValue)
        {
            var value = needValue ? new List<object>() : null;
            
            reader.ReadIsBeginArrayWithVerify();

            do
            {
                var element = ReadNode(ref reader, nodes, needValue);
                if (needValue)
                {
                    value.Add(element);
                }
            } while (reader.ReadIsValueSeparator());
            
            reader.ReadIsEndArrayWithVerify();

            return needValue ? value.ToArray() : null;
        }
        
        private static object ReadNumber(ref JsonReader reader)
        {
            var segment = reader.ReadNumberSegment();
            var str = Encoding.UTF8.GetString(segment.Array!, segment.Offset, segment.Count);
            return decimal.Parse(str);
        }

        private static object ReadNull(ref JsonReader reader)
        {
            reader.ReadIsNull();
            return null;
        }
    }
}