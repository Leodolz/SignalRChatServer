using ChatServerSignalR.Helpers;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ChatServerSignalR.Constants;
using ChatServerSignalR.Models;

namespace ChatServerSignalR.Hubs
{
    public class ChatHub : Hub
    {
        private static Queue<string> chatHistory = new Queue<string>();
        private static Dictionary<string, ConnectedUser> currentChatUsers = new Dictionary<string, ConnectedUser>();
        public async Task SendBroadcastMessage(string user, string message)
        {
            chatHistory.Enqueue(user+":"+message);
            await Clients.AllExcept(new List<string> { Context.ConnectionId}).SendAsync("ReceiveMessage", user, message);
        }
        public async Task SendMessage(string user, string message, string receiver)
        {
            if (!currentChatUsers.ContainsKey(receiver))
                System.Diagnostics.Debug.WriteLine("User not found");
            await Clients.Client(currentChatUsers[receiver].connectionId).SendAsync("ReceiveMessage", user, message);
        }
        public async Task DisconnectFromServer(string user)
        {
            currentChatUsers.Remove(user);
        }
        
        public async Task NewConnectionAdded(string user)
        {
            await Clients.AllExcept(Context.ConnectionId).SendAsync("ReceiveMessage", "Server", user + " has joined the chat!");
            currentChatUsers.Add(user, new ConnectedUser { connectionId = Context.ConnectionId, userState = UserStatesConst.USER_AVAILABLE });
           
        }
        public async Task ShowMessageHistory(string user)
        {
            Queue<string> QueueCopy = CollectionsHelper.Clone<Queue<string>>(chatHistory);
            await Clients.Client(Context.ConnectionId).SendAsync("ReceiveMessage", "Server", "Welcome User: " + user);
            while (QueueCopy.Count > 0)
            {
                await Clients.Client(Context.ConnectionId).SendAsync("ReceiveMessage", "Server", QueueCopy.Dequeue());
            }
        }

        public async Task PlayWithUserRequest(int nRounds, string challengedUser, string currentUser)
        {
            await Clients.Client(currentChatUsers[challengedUser].connectionId).SendAsync("ReceiveGameRequest", currentUser, nRounds);
        }

        public async Task AcceptGameRequest(int nRounds, string challengedUser, string currentUser)
        {
            //Send invitation to challengedUser
            //Set both users state to playing and send both play game code 
            currentChatUsers[currentUser].userState = UserStatesConst.USER_PLAYING;
            currentChatUsers[challengedUser].userState = UserStatesConst.USER_PLAYING;
            await Clients.Client(currentChatUsers[currentUser].connectionId).SendAsync("ReceiveGameCode", GameConstants.USER_ACCEPTED, nRounds);
            await Clients.Client(currentChatUsers[challengedUser].connectionId).SendAsync("ReceiveGameCode", GameConstants.USER_ACCEPTED, nRounds);

        }
        public string GetConnectionId()
        {
            return Context.ConnectionId;
        }
        public async Task RequestEcho(string message)
        {
            await Clients.Client(Context.ConnectionId).SendAsync("ReceiveMessage", "Server", message);
        }
    }
}
