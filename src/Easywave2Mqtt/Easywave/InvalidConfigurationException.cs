using System.Runtime.Serialization;

namespace Easywave2Mqtt.Easywave
{
  [Serializable]
#pragma warning disable S3925 // "ISerializable" should be implemented correctly
#pragma warning disable CA1515 // Consider making public types internal
  public sealed class InvalidConfigurationException : Exception
#pragma warning restore CA1515 // Consider making public types internal
#pragma warning restore S3925 // "ISerializable" should be implemented correctly
  {
    public InvalidConfigurationException()
    {
    }

    public InvalidConfigurationException(string? message) : base(message)
    {
    }

    public InvalidConfigurationException(string? message, Exception? innerException) : base(message, innerException)
    {
    }

  }
}