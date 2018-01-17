using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using Telegram.Bot.Args;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InlineKeyboardButtons;
using Telegram.Bot.Types.InlineQueryResults;
using Telegram.Bot.Types.InputMessageContents;
using Telegram.Bot.Types.ReplyMarkups;
using Dark.Modules.Data;
using System.Net.Http;
using System.Net;
using Newtonsoft.Json.Linq;
using System.Threading;

namespace Telegram.Bot.Examples.Echo
{
    class Program
    {

        private static List<UserTimer> _CooldownList = new List<UserTimer>();
        public class UserTimer : IEquatable<UserTimer>
        {

            public ulong UserId { get; set; }
            public System.Threading.Timer CooldownTimer = null;
            public DateTime CooldownEnding;
            public bool CancelTimer { get; set; } = false;

            public bool Equals(UserTimer other)
            {
                return this.UserId.Equals(other.UserId);
            }
        }

        public bool IsUserOnCooldown(ulong username)
        {
            var user = (from t in _CooldownList
                        where t.UserId.Equals(username)
                        select t).FirstOrDefault();

            return user != null;
        }

        public TimeSpan GetCooldownEnding(ulong userId)
        {
            // get the user
            var user = (from t in _CooldownList
                        where t.UserId.Equals(userId)
                        select t).FirstOrDefault();

            if (user != null)
            {
                return user.CooldownEnding.Subtract(DateTime.Now);
            }

            // we shouldnt get here because the user exists by checking outside however return something to show an error
            return new TimeSpan(1);
        }

        public static void TimerCompleted(object stateInfo)
        {
            // get its owner class
            UserTimer timer = (UserTimer)stateInfo;
            // we want to check to see if we want to stop the timer
            if (timer.CancelTimer == true)
            {
                // stop this timer 
                timer.CooldownTimer.Dispose();
                // remove this from the cooldownlist
                _CooldownList.Remove(timer);

                Console.WriteLine("Timer stopped. Found player");
            }
            else if (timer.CooldownEnding <= DateTime.Now)
            {
                // nobody attacked the player

                // stop this timer 
                timer.CooldownTimer.Dispose();
                // remove this from the cooldownlist
                _CooldownList.Remove(timer);

                // here, do stuff that you would need to do to cancel the deul   
                Console.WriteLine("Timer stopped. Player not found.");
                Database.AddCoins(p1, 10);
               Bot.SendTextMessageAsync(dieid, "Nobody joined your game. Refunded your coins.");
                p1 = 0;


            }
        }

        private static readonly TelegramBotClient Bot = new TelegramBotClient(""); // Put the bot token here. 

        static void Main(string[] args)
        {
            Bot.OnMessage += BotOnMessageReceived;
           

            Bot.StartReceiving();
            Console.WriteLine("bot started");
            Console.ReadLine();
            Bot.StopReceiving();
        }


        static int p1 = 0;
        static int p2 = 0;
        static long dieid = 0;
        static string player1 = null;
        static string player2 = null;
        static int p1wins = 0;
        static int p2wins = 0;



        private static async void BotOnMessageReceived(object sender, MessageEventArgs messageEventArgs)
        {

            var message = messageEventArgs.Message;



            if (message == null || message.Type != MessageType.TextMessage) return;

            
           
            
            if (message.Text.StartsWith("/dice"))
            {
                Console.WriteLine("testing");


                
                if (p1 == 0)
                {
                    await Bot.SendTextMessageAsync(message.Chat.Id, "Temporarily disabled. \n\nWe're working on it. \n\nGo bug @JakePaulFan01 or @SomeKindOfGay");
                    return;
                    p1 = message.From.Id;
                    dieid = message.Chat.Id;
                    player1 = message.From.Username;

                    var tableName = Database.GetCoins(p1);
                    long coins = tableName.FirstOrDefault().Coins;
                    long compare = 10;

                    if (coins >= compare)
                    {
                        Database.RemoveCoins(p1, 10);
                        
                       
                        await Bot.SendTextMessageAsync(message.Chat.Id, "awaiting player two... \n\nIf nobody comes within 5 minutes, it will be cancelled.");

                        // now set this person on cooldown
                        UserTimer timer = new UserTimer()
                        {
                            UserId = Convert.ToUInt64(p1),
                            CooldownEnding = DateTime.Now.AddMinutes(5)
                        };
                        System.Threading.TimerCallback timerCall = new System.Threading.TimerCallback(TimerCompleted);

                        // add this user to the cooldown list
                        _CooldownList.Add(timer);

                        // start the cooldown period. As soon as this goes, it runs
                        timer.CooldownTimer = new Timer(timerCall, timer, 1000, 10);

                    }

                    else
                    {
                        try
                        {
                            await Bot.SendTextMessageAsync(message.From.Id, "Shouldn't you be ashamed of trying to play with no money? \n\nI won't pay for you! Come back when you can afford to play.");
                        }
                        catch { }
                            return;
                    }

                }

                else if (p2 == 0)
                {
                    if (message.Chat.Id != dieid)
                    {
                        
                        await Bot.SendTextMessageAsync(message.Chat.Id, "The dice command is currently in use.");
                        return;
                    }

                    if (message.From.Id == p1)
                    {
                        await Bot.SendTextMessageAsync(message.Chat.Id, "You cannot play against yourself");
                        return;

                    }

                    

                    p2 = message.From.Id;
                   
                    player2 = message.From.Username;

                    var tableName = Database.GetCoins(p1);
                    long coins = tableName.FirstOrDefault().Coins;
                    long compare = 10;

                    if (coins >= compare)
                    {
                        Database.RemoveCoins(p2, 10);
                       

                        await Bot.SendTextMessageAsync(message.Chat.Id, "@" + player1 + " Player two has joined the game.");

                        try
                        {
                            Console.WriteLine("player 1: " + p1);

                            var ourTimer = (from c in _CooldownList
                                            where c.UserId.Equals(p1)
                                            select c).FirstOrDefault();
                            ourTimer.CancelTimer = true;
                        }

                        catch (Exception ex)
                        {
                            Console.WriteLine(ex);

                        }

                        if (p1 != 0 && p2 != 0)
                        {


                            try
                            {
                                // First, you'd get a random integer for each user, this will later determine who wins.
                                Random r = new Random();


                                int wl = r.Next(1, 2);

                                if (p1wins >= (p2wins + 2))
                                {
                                    wl = 2;
                                }

                                if (p2wins >= (p1wins + 2))
                                {
                                    wl = 1;
                                }



                                // If player one has a higher integer, then they will win.

                                if (wl == 1)
                                {

                                    Database.AddCoins(p1, 20);
                                    await Bot.SendTextMessageAsync(message.Chat.Id, "@" + player1 + " you have won!");

                                    p1wins = p1wins  + 1;

                                    p1 = 0;
                                    p2 = 0;
                                }

                                // If player two has a higher integer, then they will win.

                                else if (wl == 2)
                                {

                                    Database.AddCoins(p2, 20);

                                    await Bot.SendTextMessageAsync(message.Chat.Id, "@" + player2 + " you have won!");

                                    p2wins = p2wins + 1;

                                    p1 = 0;
                                    p2 = 0;
                                }
                            }

                            catch (Exception ex)
                            {
                                await Bot.SendTextMessageAsync(message.Chat.Id, "Exception! \n\n" + Convert.ToString(ex) + "\n\nSend this to @SomeKindOfGay");
                            }

                        }
                    }



                    else
                    {
                        await Bot.SendTextMessageAsync(message.Chat.Id, "Shouldn't you be ashamed of trying to play with no money? \n\nI won't pay for you! Come back when you can afford to play.");
                        return;
                    }

                    if (p1 != 0 && p2 != 0)
                    {


                        try
                        {
                            // First, you'd get a random integer for each user, this will later determine who wins.
                            Random r = new Random();


                            int wl = r.Next(1, 2);

                            if (p1wins >= (p2wins +2))
                            {
                                wl = 2;

                            }

                            if (p2wins >= (p1wins + 2))
                            {
                                wl = 1;
                                
                            }



                            // If player one has a higher integer, then they will win.

                            if (wl == 1)
                            {

                                Database.AddCoins(p1, 20);
                                await Bot.SendTextMessageAsync(message.Chat.Id, "@" + player1 + " you have won!");

                                p1 = 0;
                                p2 = 0;

                                p1wins = p1wins + 1;
                            }

                            // If player two has a higher integer, then they will win.

                            else if (wl == 2)
                            {

                                Database.AddCoins(p2, 20);

                                await Bot.SendTextMessageAsync(message.Chat.Id, "@" + player2 + " you have won!");

                                p1 = 0;
                                p2 = 0;

                                p2wins = p2wins + 1;
                            }
                        }

                        catch (Exception ex)
                        {
                            await Bot.SendTextMessageAsync(message.Chat.Id, "Exception! \n\n" + Convert.ToString(ex) + "\n\nSend this to @SomeKindOfGay");
                        }
                    
                }
                }
            }
            

            if (message.Text.StartsWith("/help") || message.Text.StartsWith("/start") || message.Text.StartsWith("< Return")) // send inline keyboard
            {
                IReplyMarkup keyboard = new ReplyKeyboardRemove();
                keyboard = new ReplyKeyboardMarkup(new[]
{
                        new [] // first row
                        {
                            
                            new KeyboardButton("Coin management"),
                        },

                          new [] // first row
                        {
                          
                            new KeyboardButton("Place an order at the bar"),
                        },

                    });

                await Bot.SendTextMessageAsync(
                    message.Chat.Id,
                    "Choose",
                    replyMarkup: keyboard);


            }

            if (message.Text.StartsWith("Coin management")) // send inline keyboard
            {
                IReplyMarkup keyboard = new ReplyKeyboardRemove();
                keyboard = new ReplyKeyboardMarkup(new[]
{
                        new [] // first row
                        {
                            new KeyboardButton("< Return"),
                            new KeyboardButton("Claim Daily Bonus"),
                            new KeyboardButton("See your coins"),
                        },

                    });

                await Bot.SendTextMessageAsync(
                    message.Chat.Id,
                    "Choose",
                    replyMarkup: keyboard);


            }

           


            if (message.Text.StartsWith("/beer") || message.Text.StartsWith("Beer (2G)")) // send inline keyboard
            {
                try
                {
                    var database = Database.GetCoins(message.From.Id);
                    long coins = database.FirstOrDefault().Coins;

                    if (coins >= 2)
                    {
                        Console.WriteLine(message.From.Username + " has ordered a beer");
                        await Bot.SendTextMessageAsync(message.Chat.Id, "@" + message.From.Username + " has ordered a beer.", replyMarkup: new ReplyKeyboardRemove());
                        await Bot.SendTextMessageAsync(message.Chat.Id, "*gives " + "@" + message.From.Username + " their beer.*");

                        Database.RemoveCoins(message.From.Id, 2);
                    }

                    else
                    {
                        await Bot.SendTextMessageAsync(message.Chat.Id, "@" + message.From.Username + " you are too poor for this drink.");
                    }
                   
                }

                catch (Exception ex)
                {
                    Database.EnterUser(message.From.Id);

                    Console.WriteLine(message.From.Username + " has ordered a beer");
                    await Bot.SendTextMessageAsync(message.Chat.Id, "@" + message.From.Username + " has ordered a beer.");
                    await Bot.SendTextMessageAsync(message.Chat.Id, "*gives " + "@" + message.From.Username + " their beer.*");

                    Database.RemoveCoins(message.From.Id, 2);
                }
               
            }

            if (message.Text.StartsWith("/blood")) // send inline keyboard
            {
                try
                {
                    var database = Database.GetCoins(message.From.Id);
                    long coins = database.FirstOrDefault().Coins;

                    if (coins >= 2)
                    {
                        Console.WriteLine(message.From.Username + " has ordered blood");
                        await Bot.SendTextMessageAsync(message.Chat.Id, "@" + message.From.Username + " has ordered the blood of the innocent... I'm slightly concerned.");
                        await Bot.SendTextMessageAsync(message.Chat.Id, "*dips " + "@" + message.From.Username + " in lava*");

                        Database.RemoveCoins(message.From.Id, 2);
                    }

                    else
                    {
                        await Bot.SendTextMessageAsync(message.Chat.Id, "@" + message.From.Username + " you are too poor for this drink.");
                    }

                }


                catch (Exception ex)
                {
                    Database.EnterUser(message.From.Id);

                    Console.WriteLine(message.From.Username + " has ordered blood");
                    await Bot.SendTextMessageAsync(message.Chat.Id, "@" + message.From.Username + " has ordered the blood of the innocent... I'm slightly concerned.");
                    await Bot.SendTextMessageAsync(message.Chat.Id, "*dips " + "@" + message.From.Username + " in lava*");

                    Database.RemoveCoins(message.From.Id, 2);
                }
            }

                if (message.Text.StartsWith("cid")) // send inline keyboard
            {
                await Bot.SendTextMessageAsync(message.Chat.Id, "@" + message.From.Username + " int " + message.Chat.Id);
            }

                if (message.Text.StartsWith("/water") || message.Text.StartsWith("Water (10G)")) // send inline keyboard
            {
                try
                {
                    var database = Database.GetCoins(message.From.Id);
                    long coins = database.FirstOrDefault().Coins;

                    if (coins >= 10)
                    {
                        Console.WriteLine(message.From.Username + " has ordered a water");
                        await Bot.SendTextMessageAsync(message.Chat.Id, "@" + message.From.Username + " has ordered a fucking water.", replyMarkup: new ReplyKeyboardRemove());
                        await Bot.SendTextMessageAsync(message.Chat.Id, "*gives " + "@" + message.From.Username + " their water.*");

                        Database.RemoveCoins(message.From.Id, 10);
                    }

                    else
                    {
                        await Bot.SendTextMessageAsync(message.Chat.Id, "@" + message.From.Username + " you are too poor for this drink.");
                    }

                }

                catch (Exception ex)
                {
                    Database.EnterUser(message.From.Id);

                    Console.WriteLine(message.From.Username + " has ordered a water");
                    await Bot.SendTextMessageAsync(message.Chat.Id, "@" + message.From.Username + " has ordered a fucking water.");
                    await Bot.SendTextMessageAsync(message.Chat.Id, "*gives " + "@" + message.From.Username + " their water.*");

                    Database.RemoveCoins(message.From.Id, 10);
                }

            }

            if (message.Text.StartsWith("/blowjob") || message.Text.StartsWith("Blowjob Cocktail (2G)")) // send inline keyboard
            {
                try
                {
                    var database = Database.GetCoins(message.From.Id);
                    long coins = database.FirstOrDefault().Coins;

                    if (coins >= 2)
                    {
                        Console.WriteLine(message.From.Username + " has ordered a blowjob");
                        await Bot.SendTextMessageAsync(message.Chat.Id, "@" + message.From.Username + " has ordered a blowjob. \n\nI'll see them at the back.", replyMarkup: new ReplyKeyboardRemove());
                        await Bot.SendTextMessageAsync(message.Chat.Id, "*gives " + "@" + message.From.Username + " their blowjob...* \n\nIt's a cocktail..\n\nhttps://www.liquor.com/recipes/blow-job/");

                        Database.RemoveCoins(message.From.Id, 2);
                    }

                    else
                    {
                        await Bot.SendTextMessageAsync(message.Chat.Id, "@" + message.From.Username + " you are too poor for this drink.");
                    }

                }

                catch (Exception ex)
                {
                    Database.EnterUser(message.From.Id);

                    Console.WriteLine(message.From.Username + " has ordered a blowjob");
                    await Bot.SendTextMessageAsync(message.Chat.Id, "@" + message.From.Username + " has ordered a blowjob. \n\nI'll see them at the back.");
                    await Bot.SendTextMessageAsync(message.Chat.Id, "*gives " + "@" + message.From.Username + " their blowjob...* \n\nIt's a cocktail..\n\nhttps://www.liquor.com/recipes/blow-job/");

                    Database.RemoveCoins(message.From.Id, 2);
                }

            }

            if (message.Text.StartsWith("/hotchocolate") || message.Text.StartsWith("Hot Chocolate (2G)")) // send inline keyboard
            {
                try
                {
                    var database = Database.GetCoins(message.From.Id);
                    long coins = database.FirstOrDefault().Coins;

                    if (coins >= 2)
                    {
                        Console.WriteLine(message.From.Username + " has ordered a hot chocolate");
                        await Bot.SendTextMessageAsync(message.Chat.Id, "@" + message.From.Username + " has ordered a Hot Chocolate... \n\nCaution: HOT", replyMarkup: new ReplyKeyboardRemove());
                        await Bot.SendTextMessageAsync(message.Chat.Id, "*gives " + "@" + message.From.Username + " their hot chocolate*");

                        Database.RemoveCoins(message.From.Id, 2);
                    }

                    else
                    {
                        await Bot.SendTextMessageAsync(message.Chat.Id, "@" + message.From.Username + " you are too poor for this drink.");
                    }

                }

                catch (Exception ex)
                {
                    Database.EnterUser(message.From.Id);

                    Console.WriteLine(message.From.Username + " has ordered a hot chocolate");
                    await Bot.SendTextMessageAsync(message.Chat.Id, "@" + message.From.Username + " has ordered a Hot Chocolate... \n\nCaution: HOT");
                    await Bot.SendTextMessageAsync(message.Chat.Id, "*gives " + "@" + message.From.Username + " their hot chocolate*");

                    Database.RemoveCoins(message.From.Id, 2);
                }
            }

            if (message.Text.StartsWith("/sandwich") || message.Text.StartsWith("Sandwich (5G)")) // send inline keyboard
            {
                try
                {
                    var database = Database.GetCoins(message.From.Id);
                    long coins = database.FirstOrDefault().Coins;

                    if (coins >= 5)
                    {
                        Console.WriteLine(message.From.Username + " has ordered a sandwich");
                        await Bot.SendTextMessageAsync(message.Chat.Id, "@" + message.From.Username + " has ordered a sandwich... \n\nCaution: HOT ;)", replyMarkup: new ReplyKeyboardRemove());
                        await Bot.SendTextMessageAsync(message.Chat.Id, "*gives " + "@" + message.From.Username + " their sandwich* \n\nThe label reads: 'contains extra condiments'");

                        Database.RemoveCoins(message.From.Id, 5);
                    }

                    else
                    {
                        await Bot.SendTextMessageAsync(message.Chat.Id, "@" + message.From.Username + " you are too poor for this food item. \nBlame @Krutonium");
                    }

                }

                catch (Exception ex)
                {
                    Database.EnterUser(message.From.Id);

                    Console.WriteLine(message.From.Username + " has ordered a sandwich");
                    await Bot.SendTextMessageAsync(message.Chat.Id, "@" + message.From.Username + " has ordered a sandwich... \n\nCaution: HOT ;)");
                    await Bot.SendTextMessageAsync(message.Chat.Id, "*gives " + "@" + message.From.Username + " their sandwich* \n\nThe label reads: 'contains extra condiments'");

                    Database.RemoveCoins(message.From.Id, 5);

                }
            }

            if (message.Text.StartsWith("/cookies") || message.Text.StartsWith("Cookies (5G)")) // send inline keyboard
            {
                try
                {
                    var database = Database.GetCoins(message.From.Id);
                    long coins = database.FirstOrDefault().Coins;

                    if (coins >= 5)
                    {
                        Console.WriteLine(message.From.Username + " has ordered cookies");
                        await Bot.SendTextMessageAsync(message.Chat.Id, "@" + message.From.Username + " has ordered cookies", replyMarkup: new ReplyKeyboardRemove());
                        await Bot.SendTextMessageAsync(message.Chat.Id, "*gives " + "@" + message.From.Username + " their cookies*");

                        Database.RemoveCoins(message.From.Id, 5);
                    }

                    else
                    {
                        await Bot.SendTextMessageAsync(message.Chat.Id, "@" + message.From.Username + " you are too poor for this food item. \nBlame @Krutonium");
                    }

                }

                catch (Exception ex)
                {
                    Database.EnterUser(message.From.Id);

                    Console.WriteLine(message.From.Username + " has ordered cookies");
                    await Bot.SendTextMessageAsync(message.Chat.Id, "@" + message.From.Username + " has ordered cookies");
                    await Bot.SendTextMessageAsync(message.Chat.Id, "*gives " + "@" + message.From.Username + " their cookies*");

                    Database.RemoveCoins(message.From.Id, 5);

                }
            }

            if (message.Text.StartsWith("/steak") || message.Text.StartsWith("Steak (5G)")) // send inline keyboard
            {
                try
                {
                    var database = Database.GetCoins(message.From.Id);
                    long coins = database.FirstOrDefault().Coins;

                    if (coins >= 5)
                    {
                        Console.WriteLine(message.From.Username + " has ordered steak");
                        await Bot.SendTextMessageAsync(message.Chat.Id, "@" + message.From.Username + " has ordered steak", replyMarkup: new ReplyKeyboardRemove());
                        await Bot.SendTextMessageAsync(message.Chat.Id, "*gives " + "@" + message.From.Username + " their steak* \n\nBlood dripping down as they chew it slowly. \n#gore");

                        Database.RemoveCoins(message.From.Id, 5);
                    }

                    else
                    {
                        await Bot.SendTextMessageAsync(message.Chat.Id, "@" + message.From.Username + " you are too poor for this food item. \nBlame @Krutonium");
                    }

                }

                catch (Exception ex)
                {
                    Database.EnterUser(message.From.Id);

                    Console.WriteLine(message.From.Username + " has ordered steak");
                    await Bot.SendTextMessageAsync(message.Chat.Id, "@" + message.From.Username + " has ordered steak");
                    await Bot.SendTextMessageAsync(message.Chat.Id, "*gives " + "@" + message.From.Username + " their steak* \n\nBlood dripping down as they chew it slowly. \n#gore");

                    Database.RemoveCoins(message.From.Id, 5);

                }
            }

            if(message.Text.StartsWith("/remove"))
            {
                await Bot.SendTextMessageAsync(message.Chat.Id, "Removed inline keyboard.", replyMarkup: new ReplyKeyboardRemove());
            }

            if (message.Text.StartsWith("/dick") || message.Text.StartsWith("Dick (5G)")) // send inline keyboard
            {
                try
                {
                    var database = Database.GetCoins(message.From.Id);
                    long coins = database.FirstOrDefault().Coins;

                    if (coins >= 5)
                    {
                        Console.WriteLine(message.From.Username + " has ordered dick");
                        await Bot.SendTextMessageAsync(message.Chat.Id, "@" + message.From.Username + " has ordered dick...", replyMarkup: new ReplyKeyboardRemove());
                        await Bot.SendTextMessageAsync(message.Chat.Id, "*gives " + "@" + message.From.Username + " spotted dick* \n\nGotta be english to get this one ;)");

                        Database.RemoveCoins(message.From.Id, 5);
                    }

                    else
                    {
                        await Bot.SendTextMessageAsync(message.Chat.Id, "@" + message.From.Username + " you are too poor for this food item. \nBlame @Krutonium");
                    }

                }

                catch (Exception ex)
                {
                    Database.EnterUser(message.From.Id);

                    Console.WriteLine(message.From.Username + " has ordered dick");
                    await Bot.SendTextMessageAsync(message.Chat.Id, "@" + message.From.Username + " has ordered dick...");
                    await Bot.SendTextMessageAsync(message.Chat.Id, "*gives " + "@" + message.From.Username + " spotted dick* \n\nGotta be english to get this one ;)");

                    Database.RemoveCoins(message.From.Id, 5);

                }
            }

            if (message.Text.StartsWith("/menu") || message.Text.StartsWith("Place an order at the bar"))
            {
                IReplyMarkup keyboard = new ReplyKeyboardRemove();
                keyboard = new ReplyKeyboardMarkup(new[]
{
                        new [] // first row
                        {
                            new KeyboardButton("Dick (5G)"),
                            new KeyboardButton("Steak (5G)"),
                            new KeyboardButton("Cookies (5G)"),
                            new KeyboardButton("Hot Chocolate (2G)"),
                            new KeyboardButton("Sandwich (5G)"),
                        },

                         new [] // second row
                        {
                            new KeyboardButton("Beer (2G)"),
                          new KeyboardButton("Snowball (2G)"),
                           new KeyboardButton("Vodka (2G)"),
                           new KeyboardButton("Water (10G)"),
                            new KeyboardButton("Green tea (2G)"),
                        },

                          new [] // second row
                        {
                              new KeyboardButton("< Return"),
                            new KeyboardButton("Tea (2G)"),
                          new KeyboardButton("Blowjob Cocktail (2G)"),
                           
                        },

                    });

                await Bot.SendTextMessageAsync(
                    message.Chat.Id,
                    "What can I get you?",
                    replyMarkup: keyboard);

            }
          

            if (message.Text.StartsWith("/ave_horns")) // send inline keyboard
            {
                await Bot.SendTextMessageAsync(message.Chat.Id, "🦌 Deerhorn castle, best castle 2018! \n\nFor Queen Diego!");
                string file = "https://furrycentr.al/ah.webp";
                Telegram.Bot.Types.FileToSend Telegramfile = new Telegram.Bot.Types.FileToSend(file);
                await Bot.SendStickerAsync(message.Chat.Id, Telegramfile);
            }



            if (message.Text.StartsWith("/snowball") || message.Text.StartsWith("Snowball (2G)")) // send inline keyboard
            {
                try
                {
                    var database = Database.GetCoins(message.From.Id);
                    long coins = database.FirstOrDefault().Coins;

                    if (coins >= 2)
                    {
                        Console.WriteLine(message.From.Username + " has ordered a snowball");
                        await Bot.SendTextMessageAsync(message.Chat.Id, "@" + message.From.Username + " has ordered a snowball (Lemonade and Advocaat).", replyMarkup: new ReplyKeyboardRemove());
                        await Bot.SendTextMessageAsync(message.Chat.Id, "*gives " + "@" + message.From.Username + " their snowball.*");

                        Database.RemoveCoins(message.From.Id, 2);
                    }

                    else
                    {
                        await Bot.SendTextMessageAsync(message.Chat.Id, "@" + message.From.Username + " you are too poor for this drink.");
                    }

                }

                catch (Exception ex)
                {
                    Database.EnterUser(message.From.Id);

                    Console.WriteLine(message.From.Username + " has ordered a snowball");
                    await Bot.SendTextMessageAsync(message.Chat.Id, "@" + message.From.Username + " has ordered a snowball (Lemonade and Advocaat).");
                    await Bot.SendTextMessageAsync(message.Chat.Id, "*gives " + "@" + message.From.Username + " their snowball.*");

                    Database.RemoveCoins(message.From.Id, 2);
                }
            }

            if (message.Text.StartsWith("/vodka") || message.Text.StartsWith("Vodka (2G)")) // send inline keyboard
            {
                try
                {
                    var database = Database.GetCoins(message.From.Id);
                    long coins = database.FirstOrDefault().Coins;

                    if (coins >= 2)
                    {
                        Console.WriteLine(message.From.Username + " has ordered a vodka");
                        await Bot.SendTextMessageAsync(message.Chat.Id, "@" + message.From.Username + " has ordered a vodka. Вы бесполезны", replyMarkup: new ReplyKeyboardRemove());
                        await Bot.SendTextMessageAsync(message.Chat.Id, "*gives " + "@" + message.From.Username + " their vokda.*");

                        Database.RemoveCoins(message.From.Id, 2);
                    }

                    else
                    {
                        await Bot.SendTextMessageAsync(message.Chat.Id, "@" + message.From.Username + " you are too poor for this drink.");
                    }

                }

                catch (Exception ex)
                {
                    Database.EnterUser(message.From.Id);

                    Console.WriteLine(message.From.Username + " has ordered a vodka");
                    await Bot.SendTextMessageAsync(message.Chat.Id, "@" + message.From.Username + " has ordered a vodka. Вы бесполезны");
                    await Bot.SendTextMessageAsync(message.Chat.Id, "*gives " + "@" + message.From.Username + " their vodka.*");

                    Database.RemoveCoins(message.From.Id, 2);
                }
            }

            if (message.Text.StartsWith("/greentea") || message.Text.StartsWith("Green tea (2G)")) // send inline keyboard
            {
                try
                {
                    var database = Database.GetCoins(message.From.Id);
                    long coins = database.FirstOrDefault().Coins;

                    if (coins >= 2)
                    {
                        Console.WriteLine(message.From.Username + " has ordered a Green Tea");
                        await Bot.SendTextMessageAsync(message.Chat.Id, "@" + message.From.Username + " has ordered a green tea. Brits unite!", replyMarkup: new ReplyKeyboardRemove());
                        await Bot.SendTextMessageAsync(message.Chat.Id, "*gives " + "@" + message.From.Username + " their tea.*");

                        Database.RemoveCoins(message.From.Id, 2);
                    }

                    else
                    {
                        await Bot.SendTextMessageAsync(message.Chat.Id, "@" + message.From.Username + " you are too poor for this drink.");
                    }

                }

                catch (Exception ex)
                {
                    Database.EnterUser(message.From.Id);

                    Console.WriteLine(message.From.Username + " has ordered a Green Tea");
                    await Bot.SendTextMessageAsync(message.Chat.Id, "@" + message.From.Username + " has ordered a green tea. Brits unite!", replyMarkup: new ReplyKeyboardRemove());
                    await Bot.SendTextMessageAsync(message.Chat.Id, "*gives " + "@" + message.From.Username + " their tea.*");

                    Database.RemoveCoins(message.From.Id, 2);
                }
            }

            if (message.Text.StartsWith("/tea") || message.Text.StartsWith("Tea (2G)")) // send inline keyboard
            {
                try
                {
                    var database = Database.GetCoins(message.From.Id);
                    long coins = database.FirstOrDefault().Coins;

                    if (coins >= 2)
                    {
                        Console.WriteLine(message.From.Username + " has ordered a Tea");
                        await Bot.SendTextMessageAsync(message.Chat.Id, "@" + message.From.Username + " has ordered a tea. Brits unite!");
                        await Bot.SendTextMessageAsync(message.Chat.Id, "*gives " + "@" + message.From.Username + " their tea.*");

                        Database.RemoveCoins(message.From.Id, 2);
                    }

                    else
                    {
                        await Bot.SendTextMessageAsync(message.Chat.Id, "@" + message.From.Username + " you are too poor for this drink.");
                    }

                }

                catch (Exception ex)
                {
                    Database.EnterUser(message.From.Id);

                    Console.WriteLine(message.From.Username + " has ordered a Tea");
                    await Bot.SendTextMessageAsync(message.Chat.Id, "@" + message.From.Username + " has ordered a tea. Brits unite!");
                    await Bot.SendTextMessageAsync(message.Chat.Id, "*gives " + "@" + message.From.Username + " their tea.*");

                    Database.RemoveCoins(message.From.Id, 2);
                }
            }

            if (message.Text.StartsWith("I wanna flirt with the bartender")) // send inline keyboard
            {
                Console.WriteLine(message.From.Username + " is flirting with me, help ");
                await Bot.SendTextMessageAsync(message.Chat.Id, "*Beats the life out of "+ "@" + message.From.Username + " *");
            }

           

            if (message.Text.Contains("yiff") || message.Text.Contains("Yiff")) // send inline keyboard
            {
                if (message.Chat.Id == -1001276644593 || message.Chat.Id == -1001153349667)
                {
                    await Bot.SendTextMessageAsync(message.Chat.Id, "@admin -- @" + message.From.Username + " is trying to get me to post porn!");
                    return; 
                }
                Console.WriteLine("Making API Call...");
                Image:
                using (var client = new HttpClient(new HttpClientHandler { AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate })) //This acts like a webbrowser
                {
                    
                    client.DefaultRequestHeaders.Add("User-Agent", "DarkBartneder/1.0 (by Darkmane on e621)");
                    string websiteurl = "https://e621.net/post/index.json?tags=m/m%20order:random+rating:e&limit=1";                           //The API site
                    client.BaseAddress = new Uri(websiteurl);                                   //This redirects the code to the API?
                    HttpResponseMessage response = client.GetAsync("").Result;   //Then it gets the information
                    response.EnsureSuccessStatusCode();                                       //Makes sure that its successfull
                    string result = await response.Content.ReadAsStringAsync();     //Gets the full website
                    result = result.TrimStart(new char[] { '[' }).TrimEnd(new char[] { ']' });
                    try { 
                    var json = JObject.Parse(result);                                                //Reads the json from the html (?)

                    string CatImage = json["file_url"].ToString();                                   //Saves the url to CatImage string
                    string Tags = json["tags"].ToString();

                    if (Tags.Contains("cub") || Tags.Contains("child") || Tags.Contains("children") || Tags.Contains("scat") || Tags.Contains("vore"))
                    {
                        await Bot.SendTextMessageAsync(message.Chat.Id, "Generated an invalid image. Please wait..");
                        goto Image;
                    }


                    Telegram.Bot.Types.FileToSend Telegramfile = new Telegram.Bot.Types.FileToSend(CatImage);


                        using (var client1 = new HttpClient(new HttpClientHandler { AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate })) //This acts like a webbrowser
                        {
                            string websiteurl1 = "http://tinyurl.com/api-create.php?url=" + CatImage;                           //The API site
                            client1.BaseAddress = new Uri(websiteurl1);                                   //This redirects the code to the API?
                            HttpResponseMessage response1 = client1.GetAsync("").Result;   //Then it gets the information
                            response1.EnsureSuccessStatusCode();                                       //Makes sure that its successfull
                            string result1 = await response1.Content.ReadAsStringAsync();     //Gets the full website




                            try
                            {
                                await Bot.SendPhotoAsync(message.Chat.Id, Telegramfile, "Here is the yiff! \n\nURL: " + result1);
                            }


                            catch (Exception ex) {
                                Console.WriteLine(ex);
                                await Bot.SendTextMessageAsync(message.Chat.Id, "API request failed. Please try again.");
                            }
                        }
                        
                    }
                    catch(Exception ex)
                    {
                        Console.WriteLine(ex);
                        await Bot.SendTextMessageAsync(message.Chat.Id, "API request failed. Please try again.");
                    }

                }
            }

            if (message.Text.Contains("yifF") || message.Text.Contains("YifF")) // send inline keyboard
            {
                if (message.Chat.Id == -1001276644593 || message.Chat.Id == -1001153349667)
                {
                    await Bot.SendTextMessageAsync(message.Chat.Id, "@admin -- @" + message.From.Username + " is trying to get me to post porn!");
                    return;
                }
                Console.WriteLine("Making API Call...");
                Image:
                using (var client = new HttpClient(new HttpClientHandler { AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate })) //This acts like a webbrowser
                {

                    client.DefaultRequestHeaders.Add("User-Agent", "DarkBartneder/1.0 (by Darkmane on e621)");
                    string websiteurl = "https://e621.net/post/index.json?tags=m/f%20order:random+rating:e&limit=1";                           //The API site
                    client.BaseAddress = new Uri(websiteurl);                                   //This redirects the code to the API?
                    HttpResponseMessage response = client.GetAsync("").Result;   //Then it gets the information
                    response.EnsureSuccessStatusCode();                                       //Makes sure that its successfull
                    string result = await response.Content.ReadAsStringAsync();     //Gets the full website
                    result = result.TrimStart(new char[] { '[' }).TrimEnd(new char[] { ']' });
                    try
                    {
                        var json = JObject.Parse(result);                                                //Reads the json from the html (?)

                        string CatImage = json["file_url"].ToString();                                   //Saves the url to CatImage string
                        string Tags = json["tags"].ToString();

                        if (Tags.Contains("cub") || Tags.Contains("child") || Tags.Contains("children") || Tags.Contains("scat") || Tags.Contains("vore"))
                        {
                            await Bot.SendTextMessageAsync(message.Chat.Id, "Generated an invalid image. Please wait..");
                            goto Image;
                        }

                        Telegram.Bot.Types.FileToSend Telegramfile = new Telegram.Bot.Types.FileToSend(CatImage);


                        using (var client1 = new HttpClient(new HttpClientHandler { AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate })) //This acts like a webbrowser
                        {
                            string websiteurl1 = "http://tinyurl.com/api-create.php?url=" + CatImage;                           //The API site
                            client1.BaseAddress = new Uri(websiteurl1);                                   //This redirects the code to the API?
                            HttpResponseMessage response1 = client1.GetAsync("").Result;   //Then it gets the information
                            response1.EnsureSuccessStatusCode();                                       //Makes sure that its successfull
                            string result1 = await response1.Content.ReadAsStringAsync();     //Gets the full website




                            try
                            {
                                await Bot.SendPhotoAsync(message.Chat.Id, Telegramfile, "Here is the yiff! \n\nURL: " + result1);
                            }


                            catch (Exception ex)
                            {
                                Console.WriteLine(ex);
                                await Bot.SendTextMessageAsync(message.Chat.Id, "API request failed. Please try again.");
                            }
                        }


                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex);
                        await Bot.SendTextMessageAsync(message.Chat.Id, "API request failed. Please try again.");
                    }
                }
            }



                if (message.Text.StartsWith("/coins") || message.Text.StartsWith("See your coins")) // send inline keyboard
            {
               
                try
                {
                    var database = Database.GetCoins(message.From.Id);
                    long coins = database.FirstOrDefault().Coins;

                    await Bot.SendTextMessageAsync(message.Chat.Id, "@" + message.From.Username + " You have " + coins + " coins", replyMarkup: new ReplyKeyboardRemove());

                    Console.WriteLine(message.From.Username + " has checked their coins");
                }

                catch (Exception ex)

                {
                    Database.EnterUser(message.From.Id);

                    var database = Database.GetCoins(message.From.Id);
                    long coins = database.FirstOrDefault().Coins;

                    await Bot.SendTextMessageAsync(message.Chat.Id, "@" + message.From.Username + " You have " + coins + " coins");

                    Console.WriteLine(message.From.Username + " has checked their coins");

                }

            }

            if (message.Text.StartsWith("/daily") || message.Text.StartsWith("Claim Daily Bonus")) // send inline keyboard
            {
               
                try
                {
                    var database = Database.GetCoins(message.From.Id);
                    int claimed = database.FirstOrDefault().Daily;

                    int day = DateTime.Now.Day;

                    if (claimed == day)
                    {
                        await Bot.SendTextMessageAsync(message.Chat.Id, "@" + message.From.Username + " Go away, motherfucker. \n\nYou already got your cash.", replyMarkup: new ReplyKeyboardRemove());
                        return;
                    }

                    Console.WriteLine(message.From.Username + " has claimed daily coins");


                    Database.AddCoins(message.From.Id, 25);



                    Database.UpdateDaily(message.From.Id, day);

                    await Bot.SendTextMessageAsync(message.Chat.Id, "@" + message.From.Username +  " Gave you your new wage, motherfucker.", replyMarkup: new ReplyKeyboardRemove());
                }
                catch (Exception ex)
                {
                    Database.EnterUser(message.From.Id);

                    var database = Database.GetCoins(message.From.Id);
                    int claimed = database.FirstOrDefault().Daily;
                    int day = DateTime.Now.Day;

                    if (claimed == day)
                    {
                        await Bot.SendTextMessageAsync(message.Chat.Id, "Go away, motherfucker. \n\nYou already got your cash.");
                        return;
                    }

                    Console.WriteLine(message.From.Username + " has claimed daily coins");


                    Database.AddCoins(message.From.Id, 25);

                   

                    Database.UpdateDaily(message.From.Id, day);

                    await Bot.SendTextMessageAsync(message.Chat.Id, "Gave you your new wage, motherfucker.");
                }
            }

            if (message.Text.StartsWith("degrade me daddy")) // send inline keyboard
            {
                Console.WriteLine(message.From.Username + " has been called worthless");
                await Bot.SendTextMessageAsync(message.Chat.Id, "You're a worthless piece of shit. \n\nDon't even talk to me. \n\nI bet you can't even be used as a sex toy you're that worthless. \n\nGo back to where you came from... The trash.");
            }

            if (message.Text.StartsWith("Fuck no") || message.Text.StartsWith("fuck no")) // send inline keyboard
            {
                Console.WriteLine(message.From.Username + " has denied drugs... What do I do now?");
                await Bot.SendTextMessageAsync(message.Chat.Id, "Understandable, have a nice day.");
            }

            




        }

      
        




    }
}
