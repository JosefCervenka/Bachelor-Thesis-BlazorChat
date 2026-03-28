using BlazorChatApp.Application.Services.SignalRServices;
using BlazorChatApp.Application.Utils.Observers;

namespace BlazorChatApp.Application.Services.UserServices
{
    public class UserService
    {
        private Observer _observer;

        public UserService(ISignalRService signalRService, HttpClient httpClient)
        {
            _observer = new Observer();
        }

        public void RegisterObserver(Action observer)
        {
            _observer.RegisterObserver(observer);
        }

        public void UnregisterObserver(Action observer)
        {
            _observer.UnregisterObserver(observer);
        }

        public async Task NotifyProfileUpdated()
        {
            _observer.Notify();
        }
    }
}
