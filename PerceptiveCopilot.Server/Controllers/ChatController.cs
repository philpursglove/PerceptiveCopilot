using Microsoft.AspNetCore.Mvc;
using PerceptiveCopilot.Server.DTO;

namespace PerceptiveCopilot.Server
{
    [Route("api/[controller]")]
    [ApiController]
    public class ChatController : ControllerBase
    {
        public ChatController()
        {

        }

        public async Task Chat(ChatRequest request)
        {
            // Implement your chat logic here
        }
    }
}