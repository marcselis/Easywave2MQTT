using System.Collections.ObjectModel;
using Easywave2Mqtt.Mqtt;

namespace Easywave2Mqtt.Easywave
{
  public partial class EasywaveBlind(string id, string name, ILogger<EasywaveBlind> logger) : IEasywaveEventListener
  {
    private readonly ILogger<EasywaveBlind> _logger = logger;
    private BlindState _state = BlindState.Unknown;
#pragma warning disable S4487 // Unread "private" fields should be removed
    private Task? _timer = null;
#pragma warning restore S4487 // Unread "private" fields should be removed
    private CancellationTokenSource? _cancellationTokenSource = null;

    public string Name { get; set; } = name;
    public bool IsToggle { get; }

    public BlindState State
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

    public Collection<EasywaveSubscription> Subscriptions { get; } = [];

    public string Id { get; set; } = id;

    public event BlindStateChanged? StateChanged;
    public event RequestSend? RequestSend;

    public async Task HandleCommand(string command)
    {
      EasywaveSubscription? trigger = Subscriptions.FirstOrDefault(sub => sub.CanSend);
      if (trigger == null)
      {
        return;
      }
      if (RequestSend != null)
      {
        switch (command)
        {
          case Cover.OpenCommand:
            await RequestSend(trigger.Address, trigger.KeyCode).ConfigureAwait(false);
            DelayState(BlindState.Open);
            State = BlindState.Opening;
            break;
          case Cover.CloseCommand:
            await RequestSend(trigger.Address, (char)(trigger.KeyCode + 1)).ConfigureAwait(false);
            DelayState(BlindState.Closed);
            State = BlindState.Closing;
            break;
          case Cover.StopCommand:
            await RequestSend(trigger.Address, (char)(trigger.KeyCode + 2)).ConfigureAwait(false);
            StopDelay();
            State = BlindState.Stopped;
            break;
        }
      }

    }

    private void DelayState(BlindState newState)
    {
      StopDelay();
      _cancellationTokenSource = new CancellationTokenSource();
      _timer = Task.Delay(10000, _cancellationTokenSource.Token).ContinueWith(_ => State = newState, _cancellationTokenSource.Token);
    }

    private void StopDelay()
    {
      if (_cancellationTokenSource != null && !_cancellationTokenSource.IsCancellationRequested)
      {
        _timer = null;
        _cancellationTokenSource.Cancel();
        _cancellationTokenSource.Dispose();
        _cancellationTokenSource = null;
      }
    }

    public Task HandleEvent(string address, char keyCode, string action)
    {
      EasywaveSubscription? subscription = Subscriptions.FirstOrDefault(s => s.Address == address);
      if (subscription != null)
      {
        switch (keyCode)
        {
          case 'A':
            State = BlindState.Open;
            break;
          case 'B':
            State = BlindState.Closed;
            break;
          case 'C':
            State = BlindState.Stopped;
            break;
          default:
            break;
        }
      }
      return Task.CompletedTask;
    }

    public void AddSubscription(string address, char keyCode, bool canSend = false)
    {
      Subscriptions.Add(new EasywaveSubscription(address, keyCode, canSend));
    }

    [LoggerMessage(EventId = 1, Level = LogLevel.Warning, Message = "Blind {Name} is switched to {State}")]
    public partial void LogStateSwitch(string name, BlindState state);

  }

}