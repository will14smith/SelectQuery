using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Utf8Json;

namespace SelectQuery.Evaluation.Slots
{
    internal static class SlottedQueryWriter
    {
        public static void Write(ref JsonWriter writer, SlottedQueryEvaluation.Slot value)
        {
            switch (value.Type)
            {
                case SlottedQueryEvaluation.SlotType.Span: WriteSpan(ref writer, value.Buffer); break;
                case SlottedQueryEvaluation.SlotType.Value: WriteValue(ref writer, value.Value); break;
                
                default: throw new ArgumentOutOfRangeException(nameof(value));
            }
        }

        private static void WriteSpan(ref JsonWriter writer, ArraySegment<byte> buffer)
        {
            writer.EnsureCapacity(buffer.Count);

            var target = writer.GetBuffer();
            Array.Copy(buffer.Array, buffer.Offset, target.Array, writer.CurrentOffset, buffer.Count);

            writer.AdvanceOffset(buffer.Count);
        }

        private static void WriteValue(ref JsonWriter writer, object value)
        {
            switch (value)
            {
                case Dictionary<string, object> dict: WriteObject(ref writer, dict); break;
                case object[] array: WriteArray(ref writer, array); break;
                
                case bool b: writer.WriteBoolean(b); break;
                case decimal d: writer.WriteRaw(Encoding.UTF8.GetBytes(d.ToString(CultureInfo.InvariantCulture))); break;
                case string s: writer.WriteString(s); break;
                
                case null: writer.WriteNull(); break;
                
                default: throw new ArgumentOutOfRangeException($"unexpected type: {value.GetType().FullName}");
            }
        }

        private static void WriteObject(ref JsonWriter writer, Dictionary<string, object> dict)
        {
            writer.WriteBeginObject();

            var first = true;
            
            foreach (var entry in dict)
            {
                if (first)
                {
                    first = false;
                }
                else
                {
                    writer.WriteValueSeparator();
                }
                
                writer.WritePropertyName(entry.Key);
                WriteValue(ref writer, entry.Value);
            }
            
            writer.WriteEndObject();
        }   
        
        private static void WriteArray(ref JsonWriter writer, object[] array)
        {
            writer.WriteBeginArray();

            var first = true;
            
            foreach (var entry in array)
            {
                if (first)
                {
                    first = false;
                }
                else
                {
                    writer.WriteValueSeparator();
                }
                
                WriteValue(ref writer, entry);
            }
            
            writer.WriteEndArray();
        }
    }
}