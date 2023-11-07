using Microsoft.AspNetCore.SignalR;

namespace Nuts.Datalog.Web
{
    public class DatalogHub : Hub
    {
        string datalogsDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Datalogs");

        public async Task Enumerate()
        {
            var paths = Directory.EnumerateFiles(datalogsDirectory);
            var datalogNames = paths.Select(path => Path.GetFileNameWithoutExtension(path));
            await Clients.Caller.SendAsync("Enumeration", datalogNames);
        }

        //public async Task SendMessage(string user, string message)
        //{
        //    await Clients.All.SendAsync("ReceiveMessage", user, message);
        //}
    }
}
