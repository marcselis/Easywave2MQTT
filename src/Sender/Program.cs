// See https://aka.ms/new-console-template for more information
using System.IO.Ports;

Console.WriteLine("Opening port");
var port = new SerialPort(args[0], 57600, Parity.None, 8, StopBits.One)
{
  Handshake = Handshake.None,
  DtrEnable = true,
  RtsEnable = true,
  ReadTimeout = 1000,
  NewLine = "\r"
};
port.Open();
for (int i = 0; i < int.Parse(args[1]); i++)
{
  Console.WriteLine($"Sending {args[2]}");
  port.WriteLine(args[2]);
}
port.Close();
