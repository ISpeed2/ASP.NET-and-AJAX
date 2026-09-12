using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ASP2
{
    [Route("api/[controller]")]
    [ApiController]
    public class CounterController : ControllerBase
    {
        private readonly ICounter _first;
        private readonly ICounter _second;
        private readonly IServiceProvider _provider;

        public CounterController(ICounter first, ICounter second, IServiceProvider provider)
        {
            _first = first;
            _second = second;
            _provider = provider;
        }

        [HttpGet]
        public IActionResult Get()
        {
            using var scope = _provider.CreateScope();
            var scopedCounter = scope.ServiceProvider.GetRequiredService<ICounter>();

            return Ok(new
            {
                First = _first.Value,
                Second = _second.Value,
                ReferenceEquals = ReferenceEquals(_first, _second),
                NewScopeValue = scopedCounter.Value
            });
        }
    }
}