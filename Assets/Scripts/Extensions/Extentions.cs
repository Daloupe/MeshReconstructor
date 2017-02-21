using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;

static class EnumerableExtentions
{
    public static Collection<T> ToCollection<T>(this IEnumerable<T> enumerable)
    {
        var collection = new Collection<T>();
        foreach (T i in enumerable)
            collection.Add(i);
        return collection;
    }

    /// <summary>
    /// Provides a Distinct method that takes a key selector lambda as parameter. The .net framework only provides a Distinct method that takes an instance of an implementation of IEqualityComparer<T> where the standard parameterless Distinct that uses the default equality comparer doesn't suffice.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="TKey"></typeparam>
    /// <param name="this"></param>
    /// <param name="keySelector"></param>
    /// <returns></returns>
    public static IEnumerable<T> Distinct<T, TKey>(this IEnumerable<T> @this, Func<T, TKey> keySelector)
    {
        return @this.GroupBy(keySelector).Select(grps => grps).Select(e => e.First());
    }

    /// <summary>
    ///     Applies an Action <typeparamref name="T"/> to all elements
    ///     of an array.
    /// </summary>
    /// <typeparam name="T">
    ///     Type of elements in the array
    /// </typeparam>
    /// <param name="elements">
    ///     Array of elements
    /// </param>
    /// <param name="action">
    ///     The <see cref="Action{TProperty}"/> to be performed in all
    ///     elements.
    /// </param>
    public static void Each<T>(this IEnumerable<T> elements, Action<T> action)
    {
        foreach (var e in elements) action(e);
    }

    /// <summary>
    /// Finds all the indexes of the given value in an enumerable list
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="obj"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public static IEnumerable<int> IndicesOf<T>(this IEnumerable<T> obj, T value)
    {
        return (from i in Enumerable.Range(0, obj.Count())
                where obj.ElementAt(i).Equals(value)
                select i);
    }
    /// <summary>
    /// Finds all the indexes of the given values in an enumerable list
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="obj"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public static IEnumerable<int> IndicesOf<T>(this IEnumerable<T> obj, IEnumerable<T> value)
    {
        return (from i in Enumerable.Range(0, obj.Count())
                where value.Contains(obj.ElementAt(i))
                select i);
    }



    /// <summary>
    /// Determines whether a variable of type T is contained in the supplied list of arguments of type T, allowing for more concise code.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="source"></param>
    /// <param name="list"></param>
    /// <returns></returns>
    public static bool In<T>(this T source, params T[] list)
    {
        if (null == source) throw new ArgumentNullException("source");
        return list.Contains(source);
    }
    ///// <summary>
    ///// Returns the range of elements between the specified start and end indexes. Negative numbers count from the end, rather than the start, of the sequence. Values of 'end' larger than the actual sequence are truncated and do not cause index-out-of-bounds exceptions. Functionally very similar to Python's list[x:y] slices.
    ///// </summary>
    //public static IEnumerable<T> Slice<T>(this IEnumerable<T> collection, int start, int end)
    //{
    //    int index = 0;
    //    int count = 0;

    //    if (collection == null)
    //        throw new ArgumentNullException("collection");

    //    // Optimise item count for ICollection interfaces.
    //    if (collection is ICollection<T>)
    //        count = ((ICollection<T>)collection).Count;
    //    else if (collection is ICollection)
    //        count = ((ICollection)collection).Count;
    //    else
    //        count = collection.Count();

    //    // Get start/end indexes, negative numbers start at the end of the collection.
    //    if (start < 0)
    //        start += count;

    //    if (end < 0)
    //        end += count;

    //    foreach (var item in collection)
    //    {
    //        if (index >= end)
    //            yield break;

    //        if (index >= start)
    //            yield return item;

    //        ++index;
    //    }
    //}

    public static bool IsNotNullOrEmpty<T>(this IEnumerable<T> source)
    {
        return source != null && source.Any();
    }

    // are there any common values between a and b?
    public static bool SharesAnyValueWith<T>(this IEnumerable<T> a, IEnumerable<T> b)
    {
        return a.Intersect(b).Any();
    }
    // does a contain all of b? (ignores duplicates)
    public static bool ContainsAllUniqueFrom<T>(this IEnumerable<T> a, IEnumerable<T> b)
    {
        return !b.Except(a).Any();
    }

    // does a contain all of b? (ignores duplicates)
    public static bool ContainsAllFrom<T>(this IEnumerable<T> a, IEnumerable<T> b)
    {
        var counts = a.GroupBy(t => t).ToDictionary(g => g.Key, g => g.Count());
        foreach (var t in b)
        {
            int count;
            // if t isn't in a or has too few occurrences return false. Otherwise, reduce
            // the count by 1
            if (!counts.TryGetValue(t, out count) || count == 0) { return false; }
            counts[t] = count - 1;
        }

        return true;
    }

    ///// <summary>
    /////   Returns all combinations of a chosen amount of selected elements in the sequence.
    ///// </summary>
    ///// <typeparam name = "T">The type of the elements of the input sequence.</typeparam>
    ///// <param name = "source">The source for this extension method.</param>
    ///// <param name = "select">The amount of elements to select for every combination.</param>
    ///// <param name = "repetition">True when repetition of elements is allowed.</param>
    ///// <returns>All combinations of a chosen amount of selected elements in the sequence.</returns>
    //public static IEnumerable<IEnumerable<T>> Combinations<T>(this IEnumerable<T> source, int select, bool repetition = false)
    //{
    //    //Contract.Requires(source != null);
    //    //Contract.Requires(select >= 0);

    //    return select == 0
    //        ? new[] { new T[0] }
    //        : source.SelectMany((element, index) =>
    //            source
    //                .Skip(repetition ? index : index + 1)
    //                .Combinations(select - 1, repetition)
    //                .Select(c => new[] { element }.Concat(c)));
    //}

    //public static IEnumerable<IEnumerable<T>> Permutations<T>(this IEnumerable<T> items)
    //{
    //    if (items.Count() > 1)
    //    {
    //        return items.SelectMany(item => GetPermutations(items.Where(i => !i.Equals(item))),
    //                                (item, permutation) => new[] { item }.Concat(permutation));
    //    }
    //    else
    //    {
    //        return new[] { items };
    //    }
    //}

    private static Random random = new Random();

    public static T SelectRandom<T>(this IEnumerable<T> sequence)
    {
        if (sequence == null)
        {
            throw new ArgumentNullException();
        }

        if (!sequence.Any())
        {
            throw new ArgumentException("The sequence is empty.");
        }

        //optimization for ICollection<T>
        if (sequence is ICollection<T>)
        {
            ICollection<T> col = (ICollection<T>)sequence;
            return col.ElementAt(random.Next(col.Count));
        }

        int count = 1;
        T selected = default(T);

        foreach (T element in sequence)
        {
            if (random.Next(count++) == 0)
            {
                //Select the current element with 1/count probability
                selected = element;
            }
        }

        return selected;
    }



    public static int Product(this IEnumerable<int> values)
    {
        return values.Aggregate((a, b) => a * b);
    }


    public static int Sum(this IEnumerable<int> values)
    {
        return values.Aggregate((a, b) => a + b);
    }

    //public static int GenerateHashCode<T>(this IEnumerable<T> values)
    //{
    //    return values.Aggregate((a, b) => a.GetHashCode() + b.GetHashCode()).GetHashCode();
    //}

    /// <summary>
    ///   Returns whether the sequence contains a certain amount of elements.
    /// </summary>
    /// <typeparam name = "T">The type of the elements of the input sequence.</typeparam>
    /// <param name = "source">The source for this extension method.</param>
    /// <param name = "count">The amount of elements the sequence should contain.</param>
    /// <returns>True when the sequence contains the specified amount of elements, false otherwise.</returns>
    public static bool CountOf<T>(this IEnumerable<T> source, int count)
    {
        //Contract.Requires(source != null);
        //Contract.Requires(count >= 0);

        return source.Take(count + 1).Count() == count;
    }

    public static bool None<T>(this IEnumerable<T> source)
    {
        return source.Any() == false;
    }

    public static bool None<T>(this IEnumerable<T> source, Func<T, bool> query)
    {
        return source.Any(query) == false;
    }

    public static bool Many<T>(this IEnumerable<T> source)
    {
        return source.Count() > 1;
    }

    public static bool Many<T>(this IEnumerable<T> source, Func<T, bool> query)
    {
        return source.Count(query) > 1;
    }

    public static bool OneOf<T>(this IEnumerable<T> source)
    {
        return source.Count() == 1;
    }

    public static bool OneOf<T>(this IEnumerable<T> source, Func<T, bool> query)
    {
        return source.Count(query) == 1;
    }

    public static bool XOf<T>(this IEnumerable<T> source, int count)
    {
        return source.Count() == count;
    }

    public static bool XOf<T>(this IEnumerable<T> source, Func<T, bool> query, int count)
    {
        return source.Count(query) == count;
    }
}
