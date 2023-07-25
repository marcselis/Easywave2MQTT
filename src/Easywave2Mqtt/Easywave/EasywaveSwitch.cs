using System.Collections.ObjectModel;

namespace Easywave2Mqtt.Easywave
{
  public partial class EasywaveSwitch : IEasywaveEventListener
  {
    private readonly ILogger<EasywaveSwitch> _logger;
    private SwitchState _state = SwitchState.Off;

    public EasywaveSwitch(string id, string name, bool isToggle, ILogger<EasywaveSwitch> logger)
    {
      Id = id;
      Name = name;
      IsToggle = isToggle;
      _logger = logger;
    }

    public string Name { get; set; }
    public bool IsToggle { get; }

    public SwitchState State
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
        _ = StateChanged?.Invoke(this);
      }
    }

    public Collection<EasywaveSubscription> Subscriptions { get; } = new();

    public string Id { get; set; }

    public Task HandleEvent(string address, char keyCode, string _)
    {
      EasywaveSubscription? subscription = Subscriptions.FirstOrDefault(s => s.Address == address);
      if (subscription == null)
      {
        return Task.CompletedTask;
      }
      if (IsToggle)
      {
        if (subscription.KeyCode == keyCode)
        { //Toggle the state
          State = State == SwitchState.On ? SwitchState.Off : SwitchState.On;
        }
      }
      else if (subscription.KeyCode == keyCode)
      { //Turn the switch on
        State = SwitchState.On;
      }
      else if (subscription.KeyCode == (char)(keyCode - 1))
      { //Turn the switch off
        State = SwitchState.Off;
      }
      return Task.CompletedTask;
    }

    public async Task HandleCommand(string command)
    {
      EasywaveSubscription? trigger = Subscriptions.FirstOrDefault(sub => sub.CanSend);
      if (trigger != null)
      {
        if (RequestSend != null)
        {
          switch (command)
          {
            case "on":
              await RequestSend(trigger.Address, trigger.KeyCode).ConfigureAwait(false);
              State = SwitchState.On;
              break;
            case "off":
              await RequestSend(trigger.Address, (char)(trigger.KeyCode + 1)).ConfigureAwait(false);
              State = SwitchState.Off;
              break;
          }
        }
      }
    }

    public event SwitchStateChanged? StateChanged;
    public event RequestSend? RequestSend;

    public void AddSubscription(string address, char keyCode, bool canSend = false)
    {
      Subscriptions.Add(new EasywaveSubscription(address, keyCode, canSend));
    }

    [LoggerMessage(EventId = 1, Level = LogLevel.Warning, Message = "Switch {Name} is turned {State}")]
    public partial void LogStateSwitch(string name, SwitchState state);
  }

}