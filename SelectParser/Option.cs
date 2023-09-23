using System;
using System.Collections.Generic;
using OneOf;
using OneOf.Types;

namespace SelectParser;

public struct Option<T> : IEquatable<Option<T>>
{
    public static readonly Option<T> None = new();
    
    public bool IsSome { get; }
    public T Value { get; }

    public bool IsNone => !IsSome;
    
    private Option(T value)
    {
        IsSome = true;
        Value = value;
    }

    public static implicit operator Option<T>(T t) => new(t);
    public static implicit operator Option<T>(None _) => new();
    
    public Option<TOut> Select<TOut>(Func<T, TOut> mapFn)
    {
        return IsSome ? mapFn(Value) : Option<TOut>.None;
    }

    public Option<TOut> SelectMany<TOut>(Func<T, Option<TOut>> mapFn)
    {
        return IsSome ? mapFn(Value) : Option<TOut>.None;
    }
    
    public TOut Match<TOut>(Func<T, TOut> someFn, Func<TOut> noneFn)
    {
        return IsSome ? someFn(Value) : noneFn();
    }

    public bool Equals(Option<T> other) => IsSome == other.IsSome && EqualityComparer<T>.Default.Equals(Value, other.Value);
    
    public override bool Equals(object? obj) => obj is Option<T> other && Equals(other);
    public override int GetHashCode()
    {
        unchecked
        {
            return (IsSome.GetHashCode() * 397) ^ EqualityComparer<T>.Default.GetHashCode(Value);
        }
    }

    public static bool operator ==(Option<T> left, Option<T> right) => left.Equals(right);
    public static bool operator !=(Option<T> left, Option<T> right) => !left.Equals(right);
}