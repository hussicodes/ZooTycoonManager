using ZooTycoonManager.Interfaces;
using System.Collections.Generic;

namespace ZooTycoonManager.Subjects
{
    public class GameStatsSubject : ISubject
    {
        private List<IObserver> _observers = new List<IObserver>();
        public int HabitatCount { get; private set; }
        public int AnimalCount { get; private set; }

        public void Attach(IObserver observer)
        {
            _observers.Add(observer);
            observer.Update(this);
        }

        public void Detach(IObserver observer)
        {
            _observers.Remove(observer);
        }

        public void Notify()
        {
            foreach (var observer in _observers)
            {
                observer.Update(this);
            }
        }

        public void UpdateStats(int habitatCount, int animalCount)
        {
            HabitatCount = habitatCount;
            AnimalCount = animalCount;
            Notify();
        }
    }
} 