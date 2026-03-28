using Microsoft.AspNetCore.Mvc;

namespace BlazorChatApp.Controllers
{
    public class IcoController : ControllerBase
    {
        private readonly IWebHostEnvironment _env;

        public IcoController(IWebHostEnvironment env)
        {
            _env = env;
        }

        [HttpGet("/favicon.ico")]
        public IActionResult Index()
        {
            string path = Path.Combine(_env.WebRootPath, "chat.ico");
            return PhysicalFile(path, "image/x-icon");
        }
    }
}
