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

  public class TransmittersController : ControllerBase
  {
    private readonly ILogger<TransmittersController> _logger;
    private readonly AppDbContext _context;
    private readonly IBus _bus;

    public TransmittersController(ILogger<TransmittersController> logger, AppDbContext context, IBus bus)
    {
      _logger = logger;
      _context = context;
      _bus = bus;
    }

    [HttpGet]
    [Produces("application/json")]
    [ProducesResponseType(typeof(IEnumerable<Transmitter>), 200)]
    public ActionResult<IEnumerable<Transmitter>> Get()
    {
      using (_logger.BeginScope("Getting transmitters"))
      {
        var devices = _context.Devices.Where(d => d.Type == DeviceType.Transmitter).Select(d => new Transmitter(d.Id!, d.Name!, d.Area, d.Buttons!));
        return Ok(devices);
      }
    }

    [HttpGet("{id}")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(Transmitter), 200)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<Transmitter>> GetById(string? id)
    {
      using (_logger.BeginScope($"Getting device {id}"))
      {
        var device = await _context.Devices.AsNoTracking().FirstOrDefaultAsync(d => d.Id == id).ConfigureAwait(false);
        if (device == null)
        {
          return NotFound();
        }
        return Ok(new Transmitter(device.Id, device.Name, device.Area, device.Buttons!));
      }
    }

    [HttpPost]
    [Produces("application/json")]
    [ProducesResponseType(typeof(Transmitter), 201)]
    [ProducesResponseType(422)]
    public async Task<ActionResult<Transmitter>> CreateTransmitter([FromBody] Transmitter transmitter)
    {
      using (_logger.BeginScope($"Creating new transmitter {transmitter.Id}"))
      {
        var entity = await _context.Devices.AddAsync(new Device(transmitter.Id, transmitter.Name, DeviceType.Transmitter, transmitter.Area, transmitter.Buttons)).ConfigureAwait(false);
        var device = entity.Entity;
        _ = await _context.SaveChangesAsync().ConfigureAwait(false);
        await _bus.PublishAsync(new TransmitterAdded(device.Id, device.Name, device.Area, device.Buttons!)).ConfigureAwait(false);
        return CreatedAtAction(nameof(GetById), new { id = device.Id }, new Transmitter(device.Id, device.Name, device.Area, device.Buttons!));
      }

    }
  }

}

