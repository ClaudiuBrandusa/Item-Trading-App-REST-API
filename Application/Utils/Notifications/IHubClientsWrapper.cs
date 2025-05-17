using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Utils.Notifications;
public interface IHubClientsWrapper
{
    Task SendTo(string target, string message, object content);
}
