using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace Cad_Point_Manager.Common.Collections
{
    public class BatchableObservableCollection<T> : ObservableCollection<T>
    {
        private int _deferLevel;
        private bool _dirty;

        public IDisposable DeferNotifications()
        {
            _deferLevel++;
            return new Scope(this);
        }

        protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            if (_deferLevel > 0)
            {
                _dirty = true;
                return;
            }

            base.OnCollectionChanged(e);
        }


        protected override void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            if (_deferLevel > 0)
            {
                _dirty = true;
                return;
            }

            base.OnPropertyChanged(e);
        }

        public void EndDefer()
        {
            if (_deferLevel == 0)
            {
                return;
            }

            _deferLevel--;

            if (_deferLevel > 0)
            {
                return;
            }

            if (_dirty)
            {
                _dirty = false;

                base.OnPropertyChanged(new PropertyChangedEventArgs(nameof(Count)));

                base.OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));

                base.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            }
        }

        private sealed class Scope : IDisposable
        {
            private readonly BatchableObservableCollection<T> _owner;
            private bool _disposed;

            public Scope(BatchableObservableCollection<T> owner)
            {
                _owner = owner;
            }

            public void Dispose()
            {
                if (_disposed)
                {
                    return;
                }

                _disposed = true;
                _owner.EndDefer();
            }
        }

        public void AddRange(IEnumerable<T> items)
        {
            using (DeferNotifications())
            {
                foreach (var item in items)
                {
                    Add(item);
                }
            }
        }

        public void ReplaceWith(IEnumerable<T> items)
        {
            using (DeferNotifications())
            {
                Items.Clear();

                foreach (var item in items)
                {
                    Add(item);
                }
            }
        }
    }
}