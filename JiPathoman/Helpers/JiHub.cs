using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JiPathoman.Helpers
{
    public class JiHub : Hub
    {

        public async Task SendMessage(object message)
        {
            await Clients.All.SendAsync("ReceiveMessage", message);
        }

    }
}
