using System.IO.Ports;

// See https://aka.ms/new-console-template for more information
Console.WriteLine("Checking ports");
      var portNames = SerialPort.GetPortNames();
      if (portNames.Length==0)
      {
        Console.Error.WriteLine("No serial ports found");
        return;
      }
      Console.WriteLine("Ports found:");
      foreach (var portName in portNames)      {
        Console.WriteLine(portName);
      }
      Console.WriteLine("Testing ports:");
      foreach (var portName in portNames)      {
        Console.WriteLine($"Testing {portName}");
        try        {
          using var serialPort = new SerialPort(portName);
          serialPort.Open();
          Console.WriteLine($"Port {portName} opened successfully");
          serialPort.Close();
        }
        catch (Exception ex)        {
          Console.WriteLine($"Error opening port {portName}: {ex.Message}");
        }
      } 
