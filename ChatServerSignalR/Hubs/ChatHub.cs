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
                return;
            await Clients.Client(currentChatUsers[receiver].connectionId).SendAsync("ReceiveMessage", user, message);
        }
        public async Task DisconnectFromServer(string user)
        {
            currentChatUsers.Remove(user);
        }
        public async Task LeaveGame(string user)
        {
            await Clients.Client(currentChatUsers[user].connectionId).SendAsync("ReceiveDisconnect");
        }

        public async Task NewConnectionAdded(string user)
        {
            await Clients.AllExcept(Context.ConnectionId).SendAsync("ReceiveMessage", "Server", user + " has joined the chat!");
            if(currentChatUsers.ContainsKey(user))  currentChatUsers.Remove(user);
            currentChatUsers.Add(user, new ConnectedUser { connectionId = Context.ConnectionId, userState = UserStatesConst.USER_AVAILABLE });

        }
        public async Task<int> ConsultSingleContactState(string userName)
        {
            if (!currentChatUsers.ContainsKey(userName)) return Constants.UserStatesConst.USER_ABSENT;
            return currentChatUsers[userName].userState;
        }
        public async Task<int[]> ConsultContactsStates(string[] contacts)
        {
            List<int> allContactsState = new List<int>();
            foreach(string contact in contacts)
            {
                if (!currentChatUsers.ContainsKey(contact)) allContactsState.Add(Constants.UserStatesConst.USER_ABSENT);
                else allContactsState.Add(currentChatUsers[contact].userState);
            }
            return allContactsState.ToArray();
            
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
        public async Task SendConnectedToFriends(string[] contacts, string currentUser)
        {
            foreach(string contact in contacts)
            {
                if (currentChatUsers.ContainsKey(contact))
                    await Clients.Client(currentChatUsers[contact].connectionId).SendAsync("ReceiveFriendConnect",currentUser);
            }
        }
       

        public async Task AcceptGameRequest(int nRounds, string challengedUser, string currentUser)
        {
            currentChatUsers[currentUser].userState = UserStatesConst.USER_PLAYING;
            currentChatUsers[challengedUser].userState = UserStatesConst.USER_PLAYING;
            await Clients.Client(currentChatUsers[currentUser].connectionId).SendAsync("ReceiveGameCode", GameConstants.USER_ACCEPTED, nRounds, challengedUser);
            await Clients.Client(currentChatUsers[challengedUser].connectionId).SendAsync("ReceiveGameCode", GameConstants.USER_ACCEPTED, nRounds, currentUser);

        }
        public async Task SendUsersPick(int sendersPick, string nextUsername)
        {
            await Clients.Client(currentChatUsers[nextUsername].connectionId).SendAsync("ReceiveRivalsPick", sendersPick);
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
