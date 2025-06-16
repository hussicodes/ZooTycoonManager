using System.Collections.Generic;
using System.Linq;

namespace ZooTycoonManager
{
    public class AnimalManager : ISubject<string>
    {
        private List<IObserver<string>> _observers = new List<IObserver<string>>();
        private List<Animal> _animals = new List<Animal>();
        private static AnimalManager _instance;
        private static readonly object _lock = new object();

        private AnimalManager() { }

        public static AnimalManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new AnimalManager();
                        }
                    }
                }
                return _instance;
            }
        }

        public void AddAnimal(Animal animal)
        {
            _animals.Add(animal);
            Notify();
        }

        public void RemoveAnimal(Animal animal)
        {
            _animals.Remove(animal);
            Notify();
        }

        public List<Animal> GetAllAnimals()
        {
            return _animals;
        }

        public int AnimalCount => _animals.Count;

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
            string animalInfo = $"Animals: {_animals.Count}";
            foreach (var observer in _observers)
            {
                observer.Update(animalInfo);
            }
        }
    }
} 