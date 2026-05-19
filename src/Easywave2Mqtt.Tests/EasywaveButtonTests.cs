using Easywave2Mqtt;
using Easywave2Mqtt.Configuration;
using Easywave2Mqtt.Easywave;
using Microsoft.Extensions.Logging.Abstractions;

namespace Easywave2Mqtt.Tests
{

  public sealed class EasywaveButtonTests
  {
    private const int ActionTimeout = 40;
    private const int RepeatTimeout = 20;

    public EasywaveButtonTests()
    {
      Program.Settings = new Settings
                         {
                           EasywaveActionTimeout = ActionTimeout,
                           EasywaveRepeatTimeout = RepeatTimeout
                         };
    }

    [Fact]
    public void Constructor_SetsPublicProperties()
    {
      using var button = CreateButton(
                                      id: "button-1",
                                      keyCode: 'A',
                                      name: "Kitchen button",
                                      area: "Kitchen");

      Assert.Equal("button-1", button.Id);
      Assert.Equal('A', button.KeyCode);
      Assert.Equal("Kitchen button", button.Name);
      Assert.Equal("Kitchen", button.Area);
    }

    [Fact]
    public async Task HandleCommand_CompletesWithoutRaisingButtonEvents()
    {
      using var button = CreateButton();

      var eventCount = 0;
      button.Pressed += _ =>
                        {
                          eventCount++;
                          return Task.CompletedTask;
                        };
      button.DoublePressed += _ =>
                              {
                                eventCount++;
                                return Task.CompletedTask;
                              };
      button.TriplePressed += _ =>
                              {
                                eventCount++;
                                return Task.CompletedTask;
                              };
      button.Held += _ =>
                     {
                       eventCount++;
                       return Task.CompletedTask;
                     };
      button.Released += _ =>
                         {
                           eventCount++;
                           return Task.CompletedTask;
                         };

      await button.HandleCommand("on");

      Assert.Equal(0, eventCount);
    }

    [Fact]
    public async Task HandlePress_WhenSinglePress_RaisesPressedAfterActionTimeout()
    {
      using var button = CreateButton();

      var pressed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
      var eventCount = 0;

      button.Pressed += b =>
                        {
                          Assert.Same(button, b);
                          eventCount++;
                          pressed.SetResult();
                          return Task.CompletedTask;
                        };

      await button.HandlePress();

      await AssertCompletesAsync(pressed.Task);
      Assert.Equal(1, eventCount);
    }

    [Fact]
    public async Task HandlePress_WhenTwoPressesSeparatedByRepeatTimeout_RaisesDoublePressed()
    {
      using var button = CreateButton();

      var doublePressed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
      var pressedCount = 0;
      var doublePressedCount = 0;

      button.Pressed += _ =>
                        {
                          pressedCount++;
                          return Task.CompletedTask;
                        };
      button.DoublePressed += b =>
                              {
                                Assert.Same(button, b);
                                doublePressedCount++;
                                doublePressed.SetResult();
                                return Task.CompletedTask;
                              };

      await button.HandlePress();
      await Task.Delay(RepeatTimeout + 10);
      await button.HandlePress();

      await AssertCompletesAsync(doublePressed.Task);

      Assert.Equal(0, pressedCount);
      Assert.Equal(1, doublePressedCount);
    }

    [Fact]
    public async Task HandlePress_WhenThreePressesSeparatedByRepeatTimeout_RaisesTriplePressed()
    {
      using var button = CreateButton();

      var triplePressed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
      var triplePressedCount = 0;

      button.TriplePressed += b =>
                              {
                                Assert.Same(button, b);
                                triplePressedCount++;
                                triplePressed.SetResult();
                                return Task.CompletedTask;
                              };

      await button.HandlePress();
      await Task.Delay(RepeatTimeout + 10);
      await button.HandlePress();
      await Task.Delay(RepeatTimeout + 10);
      await button.HandlePress();

      await AssertCompletesAsync(triplePressed.Task);

      Assert.Equal(1, triplePressedCount);
    }

    [Fact]
    public async Task HandlePress_WhenRepeatedMessagesAreBelowThreshold_DoesNotRaisePressedImmediately()
    {
      using var button = CreateButton();

      var eventCount = 0;
      button.Pressed += _ =>
                        {
                          eventCount++;
                          return Task.CompletedTask;
                        };
      button.Held += _ =>
                     {
                       eventCount++;
                       return Task.CompletedTask;
                     };

      await button.HandlePress();
      await button.HandlePress();
      await button.HandlePress();
      await button.HandlePress();

      await Task.Delay(RepeatTimeout);

      Assert.Equal(0, eventCount);
    }

    [Fact]
    public async Task HandlePress_WhenRepeatThresholdReached_RaisesHeld()
    {
      using var button = CreateButton();

      var held = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
      var heldCount = 0;

      button.Held += b =>
                     {
                       Assert.Same(button, b);
                       heldCount++;
                       held.SetResult();
                       return Task.CompletedTask;
                     };

      await button.HandlePress();

      for (var i = 0; i < 5; i++)
      {
        await button.HandlePress();
      }

      await AssertCompletesAsync(held.Task);

      Assert.Equal(1, heldCount);
    }

    [Fact]
    public async Task HandlePress_AfterHeld_RaisesReleasedWhenActionTimeoutExpires()
    {
      using var button = CreateButton();

      var held = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
      var released = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

      button.Held += _ =>
                     {
                       held.SetResult();
                       return Task.CompletedTask;
                     };

      button.Released += b =>
                         {
                           Assert.Same(button, b);
                           released.SetResult();
                           return Task.CompletedTask;
                         };

      await button.HandlePress();

      for (var i = 0; i < 5; i++)
      {
        await button.HandlePress();
      }

      await AssertCompletesAsync(held.Task);
      await AssertCompletesAsync(released.Task);
    }

    private static EasywaveButton CreateButton(
    string id = "id",
    char keyCode = 'A',
    string name = "Button",
    string? area = null)
    {
      return new EasywaveButton(
                                id,
                                keyCode,
                                name,
                                area,
                                NullLogger<EasywaveButton>.Instance);
    }

    private static async Task AssertCompletesAsync(Task task)
    {
      var timeout = Task.Delay(TimeSpan.FromMilliseconds(ActionTimeout * 5));
      var completed = await Task.WhenAny(task, timeout);

      Assert.Same(task, completed);
      await task;
    }
  }

}