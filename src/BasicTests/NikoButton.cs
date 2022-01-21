// See https://aka.ms/new-console-template for more information
using MQTTnet.Client;

public class NikoButton : Device, IDisposable
{
    private readonly IMqttClient _client;
    int _lastButton = 0;
    ButtonAction _lastAction = ButtonAction.Unknown;
    CancellationTokenSource tokenSource = new CancellationTokenSource();


    public NikoButton(IMqttClient client, uint id, string model, string name) : base(id, "Niko", model, name)
    {
        _client = client;
    }

    public async Task HandleButton(int buttonId)
    {
        //Cancel any running background tasks
        tokenSource.Cancel();
        tokenSource.Dispose();
        tokenSource = new CancellationTokenSource();
        if (buttonId == _lastButton && buttonId!=0)
        {
            //if button is the same than the previous button, this means that the button is being pressed for a longer time, so send a long-press event to home assistant
            await _client.SendButtonLongHold(this, _lastButton).ConfigureAwait(false);
            _lastAction = ButtonAction.Hold;
        }
        else
        {
            //the button is another one than the previous button, so depending on what the last action was, we need to send an event to home assistant
            if (_lastButton != 0)
            {

                switch (_lastAction)
                {
                    case ButtonAction.Press:
                        //the last action was a single press, so we send this to home assistant.
                        await _client.SendButtonShortPress(this, _lastButton).ConfigureAwait(false);
                        break;
                    case ButtonAction.Hold:
                        //the last action was a hold, this means that that key was released and now another key has been pressed, so we send the release to home assistant
                        await _client.SendButtonLongRelease(this, _lastButton).ConfigureAwait(false);
                        break;
                }
            }
            _lastAction = ButtonAction.Press;
        }
        //remember what button was pressed
        _lastButton = buttonId;
        //Because we want to make a difference between short- and long presses we can't send something to home assistant at this moment, as we do not know what the next message will be.
        //That is why we start a background task with a delay of 100ms to call this method recursively with a 0 button.
        //If that task runs, the code above will send a button pressed to home assistant
        //If this method is executed again within 100ms the background task will be cancelled and will never execute.
        var token = tokenSource.Token;
        _ = Task.Run(() => Task.Delay(600, token)).ContinueWith(async (t) =>
        {
            if (!token.IsCancellationRequested)
            {
                await HandleButton(0);
            }
        });

    }

    public void Dispose()
    {
        if (tokenSource != null)
        {
            if (!tokenSource.IsCancellationRequested)
            {
                tokenSource.Cancel();
            }
            tokenSource.Dispose();
        }
        GC.SuppressFinalize(this);
    }
}
