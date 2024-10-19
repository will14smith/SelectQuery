using System;
using System.Collections.Generic;

namespace SelectParser;

public readonly struct Option<T> : IEquatable<Option<T>>
{
    public Option(T value)
    {
        HasValue = true;
        Value = value;
    }
    public Option()
    {
        HasValue = false;
        Value = default;
    }

    public bool HasValue { get; }
    public T? Value { get; }
    public T AsT0 => HasValue ? Value! : throw new InvalidOperationException();
    
    public bool IsSome => HasValue;
    public bool IsNone => !HasValue;
    
    public static implicit operator Option<T>(T t) => new(t);
    public static implicit operator Option<T>(None _) => new();
    
    public bool Equals(Option<T> other) => HasValue == other.HasValue && EqualityComparer<T?>.Default.Equals(Value, other.Value);
    public override bool Equals(object? obj) => obj is Option<T> other && Equals(other);
    public override int GetHashCode()
    {
        unchecked
        {
            return (HasValue.GetHashCode() * 397) ^ EqualityComparer<T?>.Default.GetHashCode(Value);
        }
    }

    public static bool operator ==(Option<T> left, Option<T> right) => left.Equals(right);
    public static bool operator !=(Option<T> left, Option<T> right) => !left.Equals(right);

    public Option<TOut> Select<TOut>(Func<T, TOut> mapFn) => HasValue ? new Option<TOut>(mapFn(Value!)) : new Option<TOut>();
    public Option<TOut> SelectMany<TOut>(Func<T, Option<TOut>> mapFn) => HasValue ? mapFn(Value!) : new Option<TOut>();

    public TResult Match<TResult>(Func<T, TResult> someFn, Func<T?, TResult> noneFn) => HasValue ? someFn(Value!) : noneFn(Value);
}

public struct None { }