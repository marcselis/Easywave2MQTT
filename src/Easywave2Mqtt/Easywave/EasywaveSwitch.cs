using System.Collections.ObjectModel;

namespace Easywave2Mqtt.Easywave
{
    public partial class EasywaveSwitch : IEasywaveEventListener
    {
        public event StateChanged? StateChanged;
        public event RequestSend? RequestSend;

        private readonly ILogger<EasywaveSwitch> _logger;
        private State _state = State.Off;

        public EasywaveSwitch(string id, string name, ILogger<EasywaveSwitch> logger)
        {
            Id = id;
            Name = name;
            _logger = logger;
        }

        public string Id { get; set; }
        public string Name { get; set; }

        public State State
        {
            get { return _state; }
            private set
            {
                if (_state == value)
                {
                    return;
                }

                _state = value;
                LogStateSwitch(Name, _state);
                _ =StateChanged?.Invoke(this);
            }
        }

        public Collection<EasywaveSubscription> Subscriptions { get; } = new();

        public Task HandleEvent(string address, char keyCode, string _)
        {
            foreach (EasywaveSubscription subscription in Subscriptions)
            {
                if (subscription.Address != address)
                {
                    continue;
                }

                if (subscription.KeyCode == keyCode)
                {
                    State = State.On;
                }

                if (subscription.KeyCode == (char)(keyCode - 1))
                {
                    State = State.Off;
                }
            }

            return Task.CompletedTask;
        }

        public async Task HandleCommand(string command)
        {
            var trigger = Subscriptions.FirstOrDefault(sub => sub.CanSend);
            if (trigger != null)
            {
                if (RequestSend != null)
                {
                    switch (command)
                    {
                        case "on":
                            await RequestSend(trigger.Address, trigger.KeyCode).ConfigureAwait(false);
                            State = State.On;
                            break;
                        case "off":
                            await RequestSend(trigger.Address, (char)(trigger.KeyCode + 1)).ConfigureAwait(false);
                            State = State.Off;
                            break;
                    }
                }
            }
        }

        public void AddSubscription(string address, char keyCode, bool canSend = false)
        {
            Subscriptions.Add(new EasywaveSubscription(address, keyCode, canSend));
        }

        [LoggerMessage(EventId=1, Level=LogLevel.Warning, Message= "Switch {Name} is turned {State}")]
        public partial void LogStateSwitch(string name, State state);
    }
}