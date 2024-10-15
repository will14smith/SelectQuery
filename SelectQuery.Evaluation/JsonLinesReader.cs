using System;
using System.Text.Json;

namespace SelectQuery.Evaluation;

public ref struct JsonLinesReader
{
    private ReadOnlySpan<byte> _buffer;
    public Utf8JsonReader Current { get; private set; }
    
    public JsonLinesReader(ReadOnlySpan<byte> buffer)
    {
        _buffer = buffer;
    }

    public bool Read()
    {
        if (_buffer.Length == 0)
        {
            return false;
        }

        var newLineIndex = _buffer.IndexOf((byte)'\n');
        while (newLineIndex == 0)
        {
            _buffer = _buffer.Slice(1);
            newLineIndex = _buffer.IndexOf((byte)'\n');
        }
        
        if (_buffer.Length == 0)
        {
            return false;
        }
        
        if (newLineIndex == -1)
        {
            Current = new Utf8JsonReader(_buffer);
            _buffer = Span<byte>.Empty;
            return true;
        }
        
        Current = new Utf8JsonReader(_buffer.Slice(0, newLineIndex));
        _buffer = _buffer.Slice(newLineIndex + 1);
        return true;
    }
}