using Microsoft.AspNetCore.SignalR;

namespace userstrctureapi.Services
{
    public class RealTimeHub : Hub
    {

        public async Task SendNotification(string message)
        {
            await Clients.All.SendAsync("ReceiveNotification", message);
        }

    }
}