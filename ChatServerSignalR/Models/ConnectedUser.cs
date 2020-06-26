using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ChatServerSignalR.Models
{
    public class ConnectedUser
    {
        public string connectionId { get; set; }
        public int userState { get; set; }
    }
}
