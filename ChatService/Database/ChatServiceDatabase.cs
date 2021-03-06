﻿using ChatService.Handlers;
using Messages;
using Messages.Database;
using Messages.DataTypes;
using Messages.DataTypes.Database.Chat;
using Messages.NServiceBus.Commands;
using Messages.NServiceBus.Events;
using Messages.ServiceBusRequest.Echo.Requests;

using MySql.Data.MySqlClient;

using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatService.Database
{
    /// <summary>
    /// This portion of the class contains methods and functions
    /// </summary>
    public partial class ChatServiceDatabase : AbstractDatabase
    {

        // Constructor 
        private ChatServiceDatabase()
        {
        }

        public static ChatServiceDatabase getInstance()
        {
            if(instance == null)
            {
                Debug.consoleMsg("Creating instance of the ChatServiceDatabase");
                instance = new ChatServiceDatabase();
                return instance;
            }
            
            else
            {
                return instance;
            }
        }

        public void saveMessage(ChatMessage message)
        {
            if (openConnection() == true)
            {
                string query = "INSERT INTO chathistory(sender,receiver,timestamp,message)" +
                    "VALUES(@Sender,@Receiver,@Timestamp,@Content);";

                MySqlCommand command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@Sender", message.sender);
                command.Parameters.AddWithValue("@Receiver", message.receiver);
                command.Parameters.AddWithValue("@Timestamp", message.unix_timestamp);
                command.Parameters.AddWithValue("@Content", message.messageContents);
                command.ExecuteNonQuery();
                closeConnection();

            }
            else
            {
                Debug.consoleMsg("Unable to connect to database");
            }
        }

        public GetChatContacts getChatContacts(string userName)
        {
            if (openConnection() == true)
            {
                string query = "SELECT DISTINCT receiver FROM " + dbname + ".chathistory WHERE sender = @Username" +
                    " UNION SELECT DISTINCT sender FROM " + dbname + ".chathistory WHERE receiver = @Username;";
                
                MySqlCommand command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@Username", userName);
                MySqlDataReader reader = command.ExecuteReader();

                List<string> companies = new List<string>();

                if (reader.Read())
                    do
                    {
                        Debug.consoleMsg(reader.GetString("receiver"));
                        companies.Add(reader.GetString("receiver"));
                    } while (reader.Read());
                else
                {
                    Debug.consoleMsg("Error: No such user: '" + userName + "' in database");
                }

                GetChatContacts ret = new GetChatContacts()
                {
                    usersname = userName,
                    contactNames = companies
                };
                Debug.consoleMsg("Leaving Function");
                closeConnection();
                return ret;
            }
            else
            {
                Debug.consoleMsg("Unable to connect to database");
                closeConnection();
                return null;
            }
        }

        public GetChatHistory getChatHistory(string userName, string compName)
        {
            if (openConnection() == true)
             { 

                string query = "SELECT * FROM " + dbname + ".chathistory WHERE ((sender = @Username AND receiver= @Company) OR (sender = @Company AND receiver = @Username)) ORDER BY timestamp;";

                MySqlCommand command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@Username", userName);
                command.Parameters.AddWithValue("@Company", compName);
                MySqlDataReader reader = command.ExecuteReader();

                List<ChatMessage> messageList = new List<ChatMessage>();

                if (reader.Read())
                    do
                    {
                        ChatMessage temp = new ChatMessage()
                        {
                            sender = reader.GetString("sender"),
                            receiver = reader.GetString("receiver"),
                            unix_timestamp = reader.GetInt32("timestamp"),
                            messageContents = reader.GetString("message")
                        };

                        messageList.Add(temp);
                    } while (reader.Read());
                else
                {
                    Debug.consoleMsg("Error: No conversation between user: '" + userName + "' and company: '" + compName + "' in database");
                }

                ChatHistory hist = new ChatHistory()
                {
                    messages = messageList,
                    user1 = userName,
                    user2 = compName
                };

                GetChatHistory ret = new GetChatHistory()
                {
                    history = hist
                };

                closeConnection();
                return ret;
            }
            else
            {
                Debug.consoleMsg("Unable to connect to database");
                closeConnection();
                return null;
            }
        }
    }



    public partial class ChatServiceDatabase : AbstractDatabase
    {
        /// <summary>
        /// The name of the database.
        /// Both of these properties are required in order for both the base class and the
        /// table definitions below to have access to the variable.
        /// </summary>
        private const String dbname = "chatservicedb";
        public override string databaseName { get; } = dbname;

        /// <summary>
        /// The singleton isntance of the database
        /// </summary>
        protected static ChatServiceDatabase instance = null;

        /// <summary>
        /// This property represents the database schema, and will be used by the base class
        /// to create and delete the database.
        /// </summary>
        protected override Table[] tables { get; } =
        {
            new Table
            (
                dbname,
                "chathistory",
                new Column[]
                {
                    
                    new Column
                    (
                        "sender", "VARCHAR(40)",
                        new string[]
                        {
                            "NOT NULL",
                        }, false
                    ),
                    new Column
                    (
                        "receiver", "VARCHAR(40)",
                        new string[]
                        {
                            "NOT NULL",
                        }, false
                    ),
                    new Column
                    (
                        "timestamp", "INT(10)",
                        new string[]
                        {
                            "NOT NULL",
                        }, false
                    ),
                    new Column
                    (
                        "message", "VARCHAR(140)",
                        new string[]
                        {
                            "NOT NULL"
                        }, false
                    ),
                    new Column
                    (
                        "id", "INT(9)",
                        new string[]
                        {
                            "NOT NULL",
                            "AUTO_INCREMENT",
                            "UNIQUE"
                        }, true
                    )
                }
            )
        };
    }
}
