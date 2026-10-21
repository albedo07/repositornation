using System;
using System.Collections.Generic;

namespace AethelbornPerformance
{
    // Engine-independent policy shared by the plugin and executable tests.
    public static class PerformancePolicy
    {
        // 0 OFF, 1 ON, 2 ERROR, 3 NO FEATURES. Never show ON after a failure.
        public static int IndicatorState(bool failed, bool enabled, bool usableFeature)
        { return failed ? 2 : (!enabled ? 0 : (usableFeature ? 1 : 3)); }
        public static bool BuildingShadows(bool disableAll, bool distant) { return distant && !disableAll; }
        public static long ResidentBytes(long value) { return value > 0 ? value : -1; }
        public static bool Finite(float value) { return !float.IsNaN(value) && !float.IsInfinity(value); }
        public static bool Far(bool previous, float distanceSquared, float threshold, float hysteresis)
        {
            threshold = Math.Max(1f, threshold);
            float edge = previous ? Math.Max(1f, threshold - Math.Max(0f, hysteresis)) : threshold;
            return distanceSquared > edge * edge;
        }
        public static string RootName(string name)
        {
            if (name == null) return "";
            const string suffix = "(Clone)";
            return name.EndsWith(suffix, StringComparison.Ordinal) ? name.Substring(0, name.Length - suffix.Length) : name;
        }
        public static HashSet<string> Names(string text)
        {
            HashSet<string> names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (text == null) return names;
            string[] parts = text.Split(',');
            for (int i = 0; i < parts.Length; i++) { string name = parts[i].Trim(); if (name.Length > 0) names.Add(name); }
            return names;
        }
        public static int ParticleCap(int original, int count, int totalBudget, int maximum, bool far)
        {
            if (original <= 0) return original;
            int quota = Math.Max(1, totalBudget / Math.Max(1, count));
            int cap = Math.Max(1, Math.Min(maximum, quota));
            if (far) cap = Math.Max(1, cap / 2);
            return Math.Min(original, cap);
        }
    }
    // Stable snapshot of the game's existing registry; source-list changes cannot skip entries mid-pass.
    public sealed class DiscoverySweep<T> where T : class
    {
        private T[] _items = new T[0];
        public int Examined { get; private set; }
        public int Total { get; private set; }
        public int SourceCount { get; private set; }
        public int Revision { get; private set; }
        public bool Started { get; private set; }
        public bool Complete { get { return Started && Examined >= Total; } }
        public bool Limited { get { return SourceCount > Total; } }
        public void Begin(System.Collections.IList source, int revision, int maximum)
        {
            Clear(); Started = true; SourceCount = source == null ? 0 : source.Count; Revision = revision;
            Total = Math.Min(SourceCount, Math.Max(0, maximum));
            if (_items.Length < Total) _items = new T[Total];
            for (int i = 0; i < Total; i++) _items[i] = source[i] as T;
        }
        public int Step(Action<T> visit, int maximum, long deadline, Func<long> clock)
        {
            int done = 0;
            while (Started && Examined < Total && done < Math.Max(1, maximum))
            {
                T item = _items[Examined]; _items[Examined] = null; Examined++; done++; visit(item);
                // One hierarchy may exceed the soft time budget; it cannot be interrupted safely.
                if (clock() >= deadline) break;
            }
            return done;
        }
        public bool Ready(int revision, int loaded, int pending, bool quiet)
        { return Complete && !Limited && Revision == revision && SourceCount == loaded && pending == 0 && quiet; }
        public void Clear()
        { Array.Clear(_items, 0, _items.Length); Examined = 0; Total = 0; SourceCount = 0; Started = false; }
    }
    // Fixed storage: lifecycle hooks enqueue references only, never inspect hierarchies.
    public sealed class DiscoveryQueue<T> where T : class
    {
        private readonly T[] _items;
        private int _head;
        public int Count { get; private set; }
        public int Total { get; private set; }
        public int Examined { get; private set; }
        public bool Overflowed { get; private set; }
        public DiscoveryQueue(int capacity) { _items = new T[Math.Max(1, capacity)]; }
        public bool Add(T item)
        {
            if (Count == _items.Length) { Overflowed = true; return false; }
            _items[(_head + Count) % _items.Length] = item; Count++; Total++; return true;
        }
        public T Take()
        {
            if (Count == 0) return null;
            T item = _items[_head]; _items[_head] = null; _head = (_head + 1) % _items.Length;
            Count--; Examined++; return item;
        }
        public bool Ready(bool initialComplete, bool limited, int pendingShadows, bool quiet)
        { return initialComplete && !limited && !Overflowed && Count == 0 && pendingShadows == 0 && quiet; }
        public void Clear()
        { Array.Clear(_items, 0, _items.Length); _head = 0; Count = 0; Total = 0; Examined = 0; Overflowed = false; }
    }
    // Ordered event storage without copying a multicast array on every removal.
    // Versioned retired nodes retain native invocation-snapshot behavior during reentrancy.
    public sealed class OrderedCallbacks
    {
        private sealed class Node
        {
            public Action Callback;
            public Node Previous, Next, Same, RetiredNext;
            public long Added, Removed;
        }
        private readonly Dictionary<Action, Node> _last = new Dictionary<Action, Node>();
        private Node _first, _tail, _retired;
        private long _version;
        private int _invoking;
        public int Count { get; private set; }
        public void Add(Action callback)
        {
            if (callback == null) return;
            Node same; _last.TryGetValue(callback, out same);
            Node n = new Node { Callback = callback, Previous = _tail, Same = same, Added = ++_version };
            if (_tail == null) _first = n; else _tail.Next = n;
            _tail = n; _last[callback] = n; Count++;
        }
        public bool Remove(Action callback)
        {
            Node n;
            if (callback == null || !_last.TryGetValue(callback, out n)) return false;
            if (n.Same == null) _last.Remove(callback); else _last[callback] = n.Same;
            n.Removed = ++_version; Count--;
            if (_invoking == 0) Unlink(n);
            else { n.RetiredNext = _retired; _retired = n; }
            return true;
        }
        private void Unlink(Node n)
        {
            if (n.Previous == null) _first = n.Next; else n.Previous.Next = n.Next;
            if (n.Next == null) _tail = n.Previous; else n.Next.Previous = n.Previous;
            n.Callback = null; n.Previous = null; n.Next = null; n.Same = null; n.RetiredNext = null;
        }
        public void Invoke()
        {
            long snapshot = _version; Node end = _tail; _invoking++;
            try
            {
                for (Node n = _first; n != null; n = n.Next)
                {
                    if (n.Added <= snapshot && (n.Removed == 0 || n.Removed > snapshot)) n.Callback();
                    if (object.ReferenceEquals(n, end)) break;
                }
            }
            finally
            {
                _invoking--;
                if (_invoking == 0) while (_retired != null) { Node n = _retired; _retired = n.RetiredNext; Unlink(n); }
            }
        }
        public Action[] Snapshot()
        {
            Action[] result = new Action[Count]; int i = 0;
            for (Node n = _first; n != null; n = n.Next) if (n.Removed == 0) result[i++] = n.Callback;
            return result;
        }
        public static Action Combine(Action[] callbacks) { return Combine(callbacks, 0, callbacks.Length); }
        private static Action Combine(Action[] callbacks, int start, int count)
        {
            if (count == 0) return null;
            if (count == 1) return callbacks[start];
            int left = count / 2;
            return (Action)Delegate.Combine(Combine(callbacks, start, left), Combine(callbacks, start + left, count - left));
        }
    }
    // Never overwrite a later edit by the game or another mod.
    public sealed class OwnedValue<T>
    {
        public readonly T Original;
        private T _last;
        public bool Owned, Contested;
        public OwnedValue(T original) { Original = original; _last = original; }
        public bool TryApply(T current, T desired)
        {
            if (Contested) return false;
            if (!EqualityComparer<T>.Default.Equals(current, Owned ? _last : Original))
            { Contested = true; Owned = false; return false; }
            bool change = !EqualityComparer<T>.Default.Equals(current, desired);
            _last = desired; Owned = !EqualityComparer<T>.Default.Equals(desired, Original);
            return change;
        }
        public bool CanRestore(T current)
        {
            bool restore = Owned && EqualityComparer<T>.Default.Equals(current, _last);
            Owned = false; return restore;
        }
    }
}
