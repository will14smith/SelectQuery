using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text;
using Utf8Json;

namespace SelectQuery.Evaluation.Slots
{
    internal class SlottedQueryReader
    {
        private readonly SlottedQuery _query;
        public SlottedQueryEvaluation.Slot[] Slots { get; }

        public SlottedQueryReader(SlottedQuery query)
        {
            _query = query;
            Slots = ArrayPool<SlottedQueryEvaluation.Slot>.Shared.Rent(query.NumberOfSlots);
            Array.Clear(Slots, 0, query.NumberOfSlots);
        }
        
        public void Read(ref JsonReader reader)
        {
            var nodes = PooledList.With(_query.SlotTree);

            using (nodes)
            {
                ReadNode(ref reader, ref nodes, false);
            }
        }

        private object ReadNode(ref JsonReader reader, ref PooledList<SlottedExpressionTree> nodes, bool needValue)
        {
            // if node is a non-passthrough slot, capture whole object to output
            // if node is a passthrough slot, capture offsets to object
            // if not, read/skip as needed and recurse into children

            needValue |= NeedValue(ref nodes);

            var canSkipBlock = CanSkipReading(ref nodes);
            if (canSkipBlock && !needValue)
            {
                reader.ReadNextBlock();
                return null;
            }
            
            var startOffset = reader.GetCurrentOffsetUnsafe();

            var token = reader.GetCurrentJsonToken();
            var value = token switch
            {
                JsonToken.BeginObject => ReadObject(ref reader, ref nodes, needValue),
                JsonToken.BeginArray => ReadArray(ref reader, ref nodes, needValue),
                JsonToken.Number => ReadNumber(ref reader),
                JsonToken.String => reader.ReadString(),
                JsonToken.True or JsonToken.False => reader.ReadBoolean(),
                JsonToken.Null => ReadNull(ref reader),
                _ => throw new ArgumentOutOfRangeException($"unexpected token: {token} at {reader.GetCurrentOffsetUnsafe()}")
            };

            for (var i = 0; i < nodes.Count; i++)
            {
                var node = nodes[i];
                
                if (!node.Slot.IsSome)
                {
                    continue;
                }
                
                if (node.Passthrough)
                {
                    var buffer = new ArraySegment<byte>(reader.GetBufferUnsafe(), startOffset, checked(reader.GetCurrentOffsetUnsafe() - startOffset));
                    Slots[node.Slot.AsT0] = SlottedQueryEvaluation.Slot.CreateSpan(buffer);
                }
                else
                {
                    Slots[node.Slot.AsT0] = SlottedQueryEvaluation.Slot.CreateValue(value);
                }
            }

            return value;
        }

        private static bool CanSkipReading(ref PooledList<SlottedExpressionTree> nodes)
        {
            for (var index = 0; index < nodes.Count; index++)
            {
                var node = nodes[index];
                if (!node.Slot.IsNone || node.HasChildren)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool NeedValue(ref PooledList<SlottedExpressionTree> nodes)
        {
            for (var index = 0; index < nodes.Count; index++)
            {
                var node = nodes[index];
                if (node.Slot.IsSome && !node.Passthrough)
                {
                    return true;
                }
            }

            return false;
        }

        private object ReadObject(ref JsonReader reader, ref PooledList<SlottedExpressionTree> nodes, bool needValue)
        {
            var value = needValue ? new Dictionary<string, object>() : null;

            reader.ReadIsBeginObjectWithVerify();

            do
            {
                var prop = reader.ReadPropertyName();

                var children = nodes.GetChildren(prop);
                using (children)
                {
                    if (children.Count > 0)
                    {
                        var childValue = ReadNode(ref reader, ref children, needValue);
                        if (needValue)
                        {
                            value[prop] = childValue;
                        }
                    }
                    else if (needValue)
                    {
                        var empty = new PooledList<SlottedExpressionTree>();
                        using (empty)
                        {
                            value[prop] = ReadNode(ref reader, ref empty, true);
                        }
                    }
                    else
                    {
                        reader.ReadNextBlock();
                    }
                }
            } while (reader.ReadIsValueSeparator());
            
            reader.ReadIsEndObjectWithVerify();

            return value;
        }
        
        private object ReadArray(ref JsonReader reader, ref PooledList<SlottedExpressionTree> nodes, bool needValue)
        {
            var value = needValue ? new List<object>() : null;
            
            reader.ReadIsBeginArrayWithVerify();

            do
            {
                var element = ReadNode(ref reader, ref nodes, needValue);
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