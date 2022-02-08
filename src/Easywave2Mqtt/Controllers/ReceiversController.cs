using System.Collections.Generic;
using Easywave2Mqtt.Configuration;
using Easywave2Mqtt.Dtos;
using Easywave2Mqtt.Events;
using Easywave2Mqtt.Tools;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Easywave2Mqtt.Controllers
{
  [ApiController]
  [Route("api/[controller]")]
  public class ReceiversController : ControllerBase
  {
    private readonly ILogger<ReceiversController> _logger;
    private readonly AppDbContext _context;
    private readonly IBus _bus;

    public ReceiversController(ILogger<ReceiversController> logger, AppDbContext context, IBus bus)
    {
      _logger = logger;
      _context = context;
      _bus = bus;
    }

    [HttpGet]
    [Produces("application/json")]
    [ProducesResponseType(typeof(IEnumerable<Receiver>), 200)]
    public ActionResult<IEnumerable<Receiver>> Get()
    {
      using (_logger.BeginScope("Getting receivers"))
      {
        IEnumerable<Receiver> devices = _context.Devices.AsNoTracking().Where(d => d.Type == DeviceType.Light).Select(d => ConvertToReceiver(d));
        return Ok(devices);
      }
    }

    [HttpGet("{id}")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(Receiver), 200)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<Receiver>> GetById(string? id)
    {
      using (_logger.BeginScope($"Getting device {id}"))
      {
        var device = await _context.Devices.AsNoTracking().FirstOrDefaultAsync(d => d.Id == id).ConfigureAwait(false);
        if (device == null || device.Type!=DeviceType.Light)
        {
          return NotFound();
        }
        return Ok(ConvertToReceiver(device));
      }
    }

    [HttpPost]
    [Produces("application/json")]
    [ProducesResponseType(typeof(Receiver), 201)]
    [ProducesResponseType(422)]
    public async Task<ActionResult<Receiver>> AddDevice([FromBody] Receiver receiver)
    {
      using(_logger.BeginScope($"Creating new receivere {receiver.Id}"))
      {
        var device = ConvertToDevice(receiver);
        var entity = await _context.Devices.AddAsync(device).ConfigureAwait(false);
        _ = await _context.SaveChangesAsync().ConfigureAwait(false);
        await _bus.PublishAsync(new ReceiverAdded(device.Id, device.Name, device.Area, device.IsToggle, device.ListensTo!)).ConfigureAwait(false);
        return CreatedAtAction(nameof(GetById), new { id = entity.Entity.Id }, ConvertToReceiver(entity.Entity));
      }

    }

    private static Receiver ConvertToReceiver(Device device)
    {
      IEnumerable<Subscription> subscriptions = device.ListensTo!.Select(s => new Dtos.Subscription(s.Address, s.KeyCode, s.CanSend));
      return new Receiver(device.Id, device.Name, device.Area, device.IsToggle, subscriptions);
    }

    private static Device ConvertToDevice(Receiver receiver)
    {
      IEnumerable<ListensTo> cfgSubscriptions = receiver.Subscriptions!.Select(s => new ListensTo(s.Address, s.KeyCode, s.CanSend));
      return new Device(receiver.Id, receiver.Name, DeviceType.Light, receiver.Area, receiver.IsToggle, cfgSubscriptions);
    }
  }
}
