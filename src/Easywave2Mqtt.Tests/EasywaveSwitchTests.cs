using Easywave2Mqtt.Easywave;
using Microsoft.Extensions.Logging;

namespace Easywave2Mqtt.Tests
{

  public sealed class EasywaveSwitchTests
  {
    private static readonly ILogger<EasywaveSwitch> Logger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<EasywaveSwitch>();

    [Fact]
    public async Task ALightThatIsOnShouldTurnOffWhenButtonAIsPressedInOneButtonMode()
    {
      var sw = new EasywaveSwitch("lgt001", "Light", true, Logger);
      sw.Subscriptions.Add(new EasywaveSubscription("20da5f", 'A', false));
      await sw.HandleEvent("20da5f", 'A',"null");
      Assert.Equal(SwitchState.On, sw.State);
      await sw.HandleEvent("20da5f", 'A',"null");
      Assert.Equal(SwitchState.Off, sw.State);
      await sw.HandleEvent("20da5f", 'A',"null");
      Assert.Equal(SwitchState.On, sw.State);
    }

    [Fact]
    public async Task ALightThatIsOnShouldSwitchOffWhenTheOffCommandIsReceived()
    {
      var sw = new EasywaveSwitch("lgt001", "Light", true, Logger);
      sw.Subscriptions.Add(new EasywaveSubscription("20da5f", 'A', false));
      sw.Subscriptions.Add(new EasywaveSubscription("000063", 'A', true));
      await sw.HandleEvent("20da5f", 'A',"null");
      Assert.Equal(SwitchState.On, sw.State);
      sw.RequestSend += async (s, e) => Console.WriteLine($"Sending {e} to {s}");
      await sw.HandleCommand("OFF");
      Assert.Equal(SwitchState.Off, sw.State);
    }
    [Fact]
    public async Task ALightInOneButtonModeShouldSendButtonAWhenRequestedToSwitchOff()
    {
      var sw = new EasywaveSwitch("lgt001", "Light", true, Logger);
      sw.Subscriptions.Add(new EasywaveSubscription("20da5f", 'A', false));
      sw.Subscriptions.Add(new EasywaveSubscription("000063", 'A', true));
      await sw.HandleEvent("20da5f", 'A',"");
      Assert.Equal(SwitchState.On, sw.State);
      sw.RequestSend += async (s, e) => Assert.Equal('A', e);
      await sw.HandleCommand("OFF");
      Assert.Equal(SwitchState.Off, sw.State);
    }

    [Fact]
    public async Task ALightNotInOneButtonModeShouldSendButtonBWhenRequestedToSwitchOff()
    {
      var sw = new EasywaveSwitch("lgt001", "Light", false, Logger);
      sw.Subscriptions.Add(new EasywaveSubscription("20da5f", 'A', false));
      sw.Subscriptions.Add(new EasywaveSubscription("000063", 'A', true));
      await sw.HandleEvent("20da5f", 'A',"null");
      Assert.Equal(SwitchState.On, sw.State);
      sw.RequestSend += async (s, e) => Assert.Equal('B', e);
      await sw.HandleCommand("OFF");
      Assert.Equal(SwitchState.Off, sw.State);
    }

  }

}