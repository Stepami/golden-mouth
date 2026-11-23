using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Reki.Infrastructure.Auth;
using Telegram.Bot.Types;

namespace Reki.Api.Controllers;

[ApiController]
[ApiExplorerSettings(IgnoreApi = true)]
[Authorize(AuthenticationSchemes = AuthConstants.Schemes.TelegramWebhook)]
[Route("/telegram")]
public sealed class TelegramController : ControllerBase
{
    [HttpPost("webhook")]
    public Ok Webhook([FromBody] Update update) => TypedResults.Ok();
}
