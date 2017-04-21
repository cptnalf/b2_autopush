using System.Collections.Generic;
using System.Threading;

namespace BackupLib
{
	/// <summary>
	/// async queue with a contained semaphore, signaling how many available items there are
	/// in the queue.
	/// all functions/properties of this object are synchronized.
	/// </summary>
	/// <typeparam name="T">any type will suffice</typeparam>
	public class AsyncQueue<T>
	{
		private Queue<T> _q;
		private Semaphore _itemsWaiting;
    private ManualResetEvent _insertEvent;
    private int _maxQueued = -1;

		public int Count
		{
			get
				{
					int res = 0;
					lock (((System.Collections.ICollection)_q).SyncRoot) { res = _q.Count; }

					return res;
				}
		}

		public Semaphore ItemsWaiting { get { return _itemsWaiting; } }
		public WaitHandle wh { get { return _itemsWaiting; } }

		public AsyncQueue() : this(System.Int32.MaxValue) { }

		public AsyncQueue(int maxWaiting)
		{
			_itemsWaiting = new Semaphore(0, maxWaiting); /* look at this at a later date. */
      if (maxWaiting < System.Int32.MaxValue && maxWaiting > 0) 
        {
          _maxQueued = maxWaiting;
          _insertEvent = new ManualResetEvent(true);
        }
			_q = new Queue<T>();
		}

		/// <summary>
		/// automagically deals with the semaphore.
		/// </summary>
		/// <param name="item"></param>
		public void push(T item)
		{
      int cnt = 0;

      if (_maxQueued > 0) { _insertEvent.WaitOne(); }

			lock (((System.Collections.ICollection)_q).SyncRoot)
				{
          _q.Enqueue(item);
          if (_maxQueued > 0) { cnt = _q.Count; }
        }
      
      if (_maxQueued > 0) 
        { if (cnt > _maxQueued) { _insertEvent.Reset(); } }
		}

		/// <summary>
		/// does not do anything with the semaphore.
		/// </summary>
		/// <returns></returns>
		public T pop()
		{
      int cnt = 0;
			T item = default(T);

      lock (((System.Collections.ICollection)_q).SyncRoot)
			  {
          item = _q.Dequeue();
          cnt = _q.Count;
        }
      
      if (_maxQueued > 0)
        { if (cnt < _maxQueued) { _insertEvent.Set(); } }
			return item;
		}
	}
}
