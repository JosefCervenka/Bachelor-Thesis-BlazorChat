namespace BlazorChatApp.Application.Utils.Observers
{
    public class Observer
    {

        private readonly List<Action> _observers;

        public Observer()
        {
            _observers = new List<Action>();
        }

        public void Notify()
        {
            foreach (var observer in _observers)
            {
                observer.Invoke();
            }
        }

        public void RegisterObserver(Action observer)
        {
            if (observer == null)
            {
                throw new ArgumentNullException(nameof(observer), "Observer cannot be null");
            }
            _observers.Add(observer);
        }

        public void UnregisterObserver(Action observer)
        {
            if (observer == null)
            {
                throw new ArgumentNullException(nameof(observer), "Observer cannot be null");
            }
            _observers.Remove(observer);
        }
    }
}
