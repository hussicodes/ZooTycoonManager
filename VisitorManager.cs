using System.Collections.Generic;

namespace ZooTycoonManager
{
    public class VisitorManager : ISubject<string>
    {
        private List<IObserver<string>> _observers = new List<IObserver<string>>();
        private List<Visitor> _visitors = new List<Visitor>();
        private static VisitorManager _instance;
        private static readonly object _lock = new object();

        private VisitorManager() { }

        public static VisitorManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new VisitorManager();
                        }
                    }
                }
                return _instance;
            }
        }

        public void AddVisitor(Visitor visitor)
        {
            _visitors.Add(visitor);
            Notify();
        }

        public void RemoveVisitor(Visitor visitor)
        {
            _visitors.Remove(visitor);
            Notify();
        }

        public List<Visitor> GetVisitors()
        {
            return _visitors;
        }

        public int VisitorCount => _visitors.Count;

        public void Attach(IObserver<string> observer)
        {
            _observers.Add(observer);
        }

        public void Detach(IObserver<string> observer)
        {
            _observers.Remove(observer);
        }

        public void Notify()
        {
            string visitorInfo = $"Visitors: {_visitors.Count}";
            foreach (var observer in _observers)
            {
                observer.Update(visitorInfo);
            }
        }
    }
} 