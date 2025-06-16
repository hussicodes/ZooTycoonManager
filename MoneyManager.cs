using System;
using System.Collections.Generic;

namespace ZooTycoonManager
{
    public class MoneyManager : ISubject<decimal>
    {
        private static MoneyManager _instance;
        private static readonly object _lock = new object();
        private List<IObserver<decimal>> _observers = new List<IObserver<decimal>>();
        private decimal _currentMoney;
        public decimal CurrentMoney => _currentMoney;
        private MoneyManager() { }

        public static MoneyManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new MoneyManager();
                        }
                    }
                }
                return _instance;
            }
        }

        public void Initialize(decimal initialMoney)
        {
            _currentMoney = initialMoney;
            Notify();
        }

        public void AddMoney(decimal amount)
        {
            if (amount < 0)
            {
                return; 
            }
            _currentMoney += amount;
            Notify();
        }

        public bool SpendMoney(decimal amount)
        {
            if (amount < 0)
            {
                return false;
            }

            if (_currentMoney >= amount)
            {
                _currentMoney -= amount;
                Notify();
                return true;
            }
            return false;
        }

        public void Attach(IObserver<decimal> observer)
        {
            if (!_observers.Contains(observer))
            {
                _observers.Add(observer);
            }
        }

        public void Detach(IObserver<decimal> observer)
        {
            _observers.Remove(observer);
        }

        public void Notify()
        {
            foreach (var observer in _observers)
            {
                observer.Update(_currentMoney);
            }
        }
    }
} 