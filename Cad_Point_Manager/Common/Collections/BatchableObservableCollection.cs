using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace Cad_Point_Manager.Common.Collections
{
    public class BatchableObservableCollection<T> : ObservableCollection<T>
    {
        private int _deferLevel;
        private bool _dirty;
        private readonly List<NotifyCollectionChangedEventArgs> _deferredEvents = [];

        public IDisposable DeferNotifications()
        {
            _deferLevel++;
            return new Scope(this);
        }

        protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            if (_deferLevel > 0)
            {
                _deferredEvents.Add(e);
                return;
            }

            base.OnCollectionChanged(e);
        }

        public void EndDefer()
        {
            _deferLevel--;

            if (_deferLevel > 0)
            {
                return;
            }

            foreach (var e in _deferredEvents)
            {
                base.OnCollectionChanged(e);
            }

            _deferredEvents.Clear();
        }

        private sealed class Scope : IDisposable
        {
            private readonly BatchableObservableCollection<T> _owner;
            public Scope(BatchableObservableCollection<T> owner) { _owner = owner; }
            public void Dispose() => _owner.EndDefer();
        }

        public void AddRange(IEnumerable<T> items)
        {
            using (DeferNotifications())
            {
                foreach (var i in items) { Add(i); }
            }
        }

        public void ReplaceWith(IEnumerable<T> items)
        {
            using (DeferNotifications())
            {
                Items.Clear();
                foreach (var i in items) { Add(i); }
            }
        }
    }
}
