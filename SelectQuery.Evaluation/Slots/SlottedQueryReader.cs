using System;
using System.Collections.Generic;
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

            ReadNode(ref reader, node, false);
        }

        private object ReadNode(ref JsonReader reader, SlottedExpressionTree node, bool needValue)
        {
            // if node is a slot, capture whole object to output
            // if not, read/skip as needed and recurse into children

            if (node != null)
            {
                if (node.Slot.IsNone && node.Children.Count == 0 && !needValue)
                {
                    reader.ReadNextBlock();
                    return null;
                }

                needValue |= node.Slot.IsSome;
            }

            var token = reader.GetCurrentJsonToken();
            var value = token switch
            {
                JsonToken.BeginObject => ReadObject(ref reader, node, needValue),
                JsonToken.BeginArray => ReadArray(ref reader, node, needValue),
                JsonToken.Number => ReadNumber(ref reader),
                JsonToken.String => reader.ReadString(),
                JsonToken.True or JsonToken.False => reader.ReadBoolean(),
                JsonToken.Null => ReadNull(ref reader),
                _ => throw new ArgumentOutOfRangeException(
                    $"unexpected token: {token} at {reader.GetCurrentOffsetUnsafe()}")
            };

            if (node != null && node.Slot.IsSome)
            {
                _slots[node.Slot.AsT0] = new SlottedQueryEvaluation.Slot(value);
            }
            
            return value;
        } 
        
        private object ReadObject(ref JsonReader reader, SlottedExpressionTree node, bool needValue)
        {
            var value = needValue ? new Dictionary<string, object>() : null;

            reader.ReadIsBeginObjectWithVerify();

            do
            {
                var prop = reader.ReadPropertyName();

                if (node != null && node.Children.TryGetValue(prop, out var child))
                {
                    var childValue = ReadNode(ref reader, child, needValue);
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
        
        private object ReadArray(ref JsonReader reader, SlottedExpressionTree node, bool needValue)
        {
            var value = needValue ? new List<object>() : null;
            
            reader.ReadIsBeginArrayWithVerify();

            do
            {
                var element = ReadNode(ref reader, node, needValue);
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
            var str = Encoding.UTF8.GetString(segment.Array, segment.Offset, segment.Count);
            return decimal.Parse(str);
        }

        private static object ReadNull(ref JsonReader reader)
        {
            reader.ReadIsNull();
            return null;
        }
    }
}