using System.Diagnostics;
using System.Timers;
using Timer = System.Timers.Timer;

namespace Easywave2Mqtt.Easywave
{

  public sealed partial class EasywaveButton : IEasywaveDevice, IDisposable
  {
    private static readonly int RepeatTimeout= Program.Settings!.EasywaveRepeatTimeout;
    private readonly ILogger<EasywaveButton> _logger;
    private readonly Timer _pressTimer;
    private readonly Stopwatch _stopwatch = new();
    private int _pressCounter;
    private int _repeat;

    internal EasywaveButton(string id, char keyCode, string name, string? area, ILogger<EasywaveButton> logger)
    {
      Id = id;
      KeyCode = keyCode;
      Name = name;
      Area = area;
      _logger = logger;
      _pressTimer = new Timer(Program.Settings!.EasywaveActionTimeout);
      _pressTimer.Elapsed += SendPush;
      _pressTimer.AutoReset = false;
    }

    public char KeyCode { get; }
    public string Name { get; }
    public string? Area { get; }

    public void Dispose()
    {
      _pressTimer.Dispose();
      GC.SuppressFinalize(this);
    }

    public string Id { get; }

    public Task HandleCommand(string command)
    {
      LogIgnoredCommand(Id, command);
      return Task.CompletedTask;
    }

    public event ButtonEvent? Pressed;
    public event ButtonEvent? DoublePressed;
    public event ButtonEvent? TriplePressed;
    public event ButtonEvent? Held;
    public event ButtonEvent? Released;

    /// <summary>
    ///     Handles the detection of a easywave button press
    /// </summary>
    /// <remarks>
    ///     Easywave has a bad habit of sending up to 4 messages when a user presses a button.
    ///     This method attempts to ignore those repeated messages and parse the incoming stream of button press messages to
    ///     detect the users intention:
    ///     - A single press
    ///     - A double press
    ///     - A triple press
    ///     - Holding the button
    ///     - Releasing the button
    /// </remarks>
    internal async Task HandlePress()
    {
      var elapsed = _stopwatch.ElapsedMilliseconds;
      LogHandleButtonPressStart(KeyCode, elapsed);
      _stopwatch.Restart();
      if (!_pressTimer.Enabled)
      {
        //The very first time this method receives a press event, the press timeout timer & the stopwatch are not yet active.
        //That is why we fake that the elapsed time is longer than the time we detect (and ignore) a repeated message, so that the message is processed
        //and the timers are started.
        elapsed = RepeatTimeout + 1;
      }
      if (elapsed < RepeatTimeout)
      {
        if (IncreaseRepeat() < 5)
        {
          LogHandleButtonPressEnd(KeyCode);
          return;
        }
        _pressTimer.Stop();
        _pressTimer.Start();
        await SendHold().ConfigureAwait(false);
      }
      else
      {
        IncreasePressCounter();
        _pressTimer.Stop();
        _pressTimer.Start();
      }
      ResetRepeat();
      LogHandleButtonPressEnd(KeyCode);
    }

    private async Task SendHold()
    {
      if (Held != null)
      {
        await Held(this).ConfigureAwait(false);
      }
      ResetPressCounter();
    }

    private async void SendPush(object? sender, ElapsedEventArgs e)
    {
      LogSendPushStart();
      _pressTimer.Stop();
      switch (_pressCounter)
      {
        case 0:
          if (Released != null)
          {
            await Released(this).ConfigureAwait(false);
          }
          break;
        case 1:
          if (Pressed != null)
          {
            await Pressed(this).ConfigureAwait(false);
          }
          break;
        case 2:
          if (DoublePressed != null)
          {
            await DoublePressed(this).ConfigureAwait(false);
          }
          break;
        default:
          if (TriplePressed != null)
          {
            await TriplePressed(this).ConfigureAwait(false);
          }
          break;
      }
      ResetPressCounter();
      LogSendPushEnd();
    }

    private void IncreasePressCounter()
    {
      _pressCounter++;
      LogIncreasePressCounter(_pressCounter);
    }

    private void ResetPressCounter()
    {
      LogResetPressCounter();
      _pressCounter = 0;
      ResetRepeat();
    }

    private int IncreaseRepeat()
    {
      _repeat++;
      LogIncreaseRepeatCounter(_repeat);
      return _repeat;
    }

    private void ResetRepeat()
    {
      LogResetRepeatCounter();
      _repeat = 0;
    }

    [LoggerMessage(EventId = 4, Level = LogLevel.Trace, Message = "  -->HandlePress {KeyCode} ({ElapsedTime} elapsed)")]
    private partial void LogHandleButtonPressStart(char keyCode, long elapsedTime);

    [LoggerMessage(EventId = 5, Level = LogLevel.Trace, Message = "  <--HandlePress {KeyCode}")]
    public partial void LogHandleButtonPressEnd(char keyCode);

    [LoggerMessage(EventId = 7, Level = LogLevel.Trace, Message = "  -->SendPush")]
    private partial void LogSendPushStart();

    [LoggerMessage(EventId = 8, Level = LogLevel.Trace, Message = "  <--SendPush")]
    private partial void LogSendPushEnd();

    [LoggerMessage(EventId = 9, Level = LogLevel.Debug, Message = "    Increasing press counter to {Counter}")]
    private partial void LogIncreasePressCounter(int counter);

    [LoggerMessage(EventId = 10, Level = LogLevel.Debug, Message = "    Resetting press counter")]
    private partial void LogResetPressCounter();

    [LoggerMessage(EventId = 11, Level = LogLevel.Debug, Message = "    Increasing repeat counter to {Counter}")]
    private partial void LogIncreaseRepeatCounter(int counter);

    [LoggerMessage(EventId = 12, Level = LogLevel.Debug, Message = "    Resetting repeat counter")]
    private partial void LogResetRepeatCounter();

    [LoggerMessage(EventId=13, Level =LogLevel.Trace, Message ="    Button {Id} ignored command {Command}")]
    private partial void LogIgnoredCommand(string id, string command);
  }

}