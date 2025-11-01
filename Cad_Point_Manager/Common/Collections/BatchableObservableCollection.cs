using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace Cad_Point_Manager.Common.Collections
{
    public class BatchableObservableCollection<T> : ObservableCollection<T>
    {
        private bool _suppress;
        private bool _dirty;

        public IDisposable DeferNotifications()
        {
            _suppress = true;
            return new Scope(this);
        }

        protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            if (_suppress)
            {
                _dirty = true;
                return;
            }
            base.OnCollectionChanged(e);
        }

        public void EndDefer()
        {
            _suppress = false;
            if (_dirty)
            {
                _dirty = false;
                base.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            }
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
