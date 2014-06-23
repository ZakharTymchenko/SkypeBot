using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using SKYPE4COMLib;

namespace SkypeBot
{
    public static class SkypeInteraction
    {
        // constants and variables
        private const string headAdmin = "alexander.sirko1";
        private static bool initialized = false;
        
        private static SkypeClass skype = new SkypeClass();
        private static Config cfg = new Config();
        private static System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();

        // config
        private static string mainConference = "";
        private static bool reSender = false;

        // first
        public static bool FirstLaunch()
        {
            if (initialized)
                throw new InvalidOperationException("Already initialized");
            
            try
            {
                skype.Attach();
            }
            catch
            {
                return false;
            }

            LoadConfig();

            timer.Interval = 3 * 1000;
            timer.Tick += timer_Tick;
            timer.Start();

            return (initialized = true);
        }

        #region Conference

        private static List<string> FetchDetailedConferenceList()
        {
            List<string> list = new List<string>();

            var chats = skype.Chats;
            
            foreach (IChat chat in chats)
            {
                if (chat.Members.Count > 2)
                {
                    string members = "";
                    foreach (IUser user in chat.Members)
                    {
                        members += user.Handle + ", ";
                    }
                    members.Trim();

                    if (members.Split(' ').Contains(headAdmin + ","))
                        list.Add(string.Format("-Chat of {0} last Activity @{2}{3}--Code: {1}{3}", members, chat.Name, chat.ActivityTimestamp.ToShortDateString(), Environment.NewLine));
                }
            }

            return list;
        }

        // unused?
        private static List<string> FetchRawConferenceList()
        {
            List<string> list = new List<string>();

            var chats = skype.Chats;

            foreach (IChat chat in chats)
            {
                if (chat.Members.Count > 2)
                {
                    string members = "";
                    foreach (IUser user in chat.Members)
                    {
                        members += user.Handle + " ";
                    }
                    members.Trim();

                    if (members.Split(' ').Contains(headAdmin))
                        list.Add(chat.Name);
                }
            }

            return list;
        }

        #endregion

        #region RegularActions

        private static void timer_Tick(object sender, EventArgs e)
        {
            // Checking Auth Requests
            var usersWaiting = skype.UsersWaitingAuthorization;

            foreach (IUser user in usersWaiting)
            {
                if (!user.IsAuthorized && skype.User.Handle != "replica.of.kzd")
                    skype.set_UserIsAuthorized(user.Handle, true);
            }

            // Cheking new Messages
            var unreadMessages = skype.MissedMessages;

            foreach (IChatMessage message in unreadMessages)
            {
                message.Seen = true;

                // self messages
                if (message.Sender.Handle == skype.User.Handle)
                    continue;
                // admin is filtered
                else if (message.Sender.Handle == headAdmin)
                {
                    if (message.Body.StartsWith("."))
                        ProcessAdminCommand(message.Chat, message.Body);
                }
                else if (message.Chat.Name == mainConference)
                    continue;
                else if (reSender)
                    SendMessageToMainConfrence(message);
            }
        }

        private static void SendMessageToMainConfrence(IChatMessage message)
        {
            string msg = string.Format("From {0}{1}: {2}", message.Sender.Handle, (string.IsNullOrEmpty(message.Sender.FullName) ? "" : string.Format(" ({0})", message.Sender.FullName)), message.Body);
            var chats = skype.Chats;

            foreach (IChat chat in chats)
            {
                if (chat.Name == mainConference)
                    chat.SendMessage(msg);
            }
        }

        private static void SendMessageToMainConfrence(string msg)
        {
            var chats = skype.Chats;

            foreach (IChat chat in chats)
            {
                if (chat.Name == mainConference)
                    chat.SendMessage(msg);
            }
        }

        #endregion

        #region Administrating

        private static void ProcessAdminCommand(IChat returnChat, string command)
        {
            string[] commandElements = command.Split(' ');
            int size = commandElements.Length;

            switch (commandElements[0])
            {
                case ".fetch": // list of conf-s with headadmin
                    List<string> confs = FetchDetailedConferenceList();
                    if (confs.Count == 0)
                        returnChat.SendMessage("No conference chats found");
                    else
                    {
                        string confsList = "";
                        foreach (string s in confs)
                        {
                            confsList += s + Environment.NewLine;
                        }
                        returnChat.SendMessage(confsList);
                    }
                    break;

                case ".setmain":
                    if (size == 1)
                        returnChat.SendMessage("No conference code specified");
                    else if (size == 2)
                    {
                        ChatCollection chats = skype.Chats;
                        bool found = false;

                        foreach (IChat chat in chats)
                        {
                            if (chat.Name == commandElements[1])
                            {
                                mainConference = chat.Name;
                                cfg.SetMainConf(chat.Name);
                                returnChat.SendMessage("Main conference is updated");
                                found = true;
                                break;
                            }
                        }
                        if (!found)
                            returnChat.SendMessage("Conference chat with specified code not found");
                    }
                    break;

                case ".ban":
                    if (size == 2)
                    {
                        string userHandle = commandElements[1].ToLower();
                        bool found = false;

                        foreach (IUser user in skype.Friends)
                        {
                            if (user.Handle == userHandle)
                            {
                                skype.set_UserIsAuthorized(userHandle, false);
                                skype.set_UserIsBlocked(userHandle, true);
                                returnChat.SendMessage(string.Format("User with handle {0} banned.", userHandle));
                                found = true;
                                break;
                            }
                        }
                        if (!found)
                            returnChat.SendMessage("User not found");
                    }
                    break;

                case ".unban":
                    if (size == 2)
                    {
                        string userHandle = commandElements[1].ToLower();
                        try
                        {
                            skype.set_UserIsBlocked(userHandle, false);
                        }
                        finally
                        {
                            returnChat.SendMessage(string.Format("User with handle {0} was unbanned.", userHandle));
                        }
                    }
                    break;

                case ".ping": // ping-pong to main conf
                    SendMessageToMainConfrence("pong");
                    break;

                case ".resender": // bot activity
                    if (size == 1)
                        returnChat.SendMessage("Resender is " + (reSender ? "enabled" : "disabled"));
                    else if (size == 2)
                    {
                        switch (commandElements[1])
                        {
                            case "enable":
                                if (!reSender)
                                {
                                    cfg.SetReSender(reSender = true);
                                    returnChat.SendMessage("Resender enabled");
                                }
                                break;

                            case "disable":
                                if (reSender)
                                {
                                    cfg.SetReSender(reSender = false);
                                    returnChat.SendMessage("Resender disabled");
                                }
                                break;

                            default:
                                returnChat.SendMessage("Usage: .resender to get Resender status, .resender [enable/disable] to change");
                                break;
                        }
                    }
                    break;

                default: break;
            }
        }

        #endregion

        #region Database

        private static void LoadConfig()
        {
            Dictionary<string, string> config = cfg.LoadConfig();

            mainConference = config["mainconf"];
            reSender = (int.Parse(config["resender"]) == 1);
        }

        #endregion
    }
}
