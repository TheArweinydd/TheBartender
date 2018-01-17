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
using Newtonsoft.Json;
using System.Threading;

namespace Telegram.Bot.Examples.Echo
{
    class Program
    {

        private static List<UserTimer> _CooldownList = new List<UserTimer>();
        static Menu menu = new Menu();
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
            //Load Menu
            Console.WriteLine("Reading Menu...");
            menu = JsonConvert.DeserializeObject<Menu>(System.IO.File.ReadAllText("./files/MenuItems.json"));
            Console.WriteLine("Menu Loaded. Initializing Bot...");

            //Connect up to Telegram
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




            


            if (message.Text.StartsWith("/help") || message.Text.StartsWith("/start") || message.Text.StartsWith("< Return")) // send inline keyboard
            {
                
                ReplyMarkup keyboard = new ReplyKeyboardMarkup(new[]
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

                keyboard.Selective = true;

                await Bot.SendTextMessageAsync(
                    message.Chat.Id,
                    "Choose",
                    replyMarkup: keyboard, replyToMessageId: message.MessageId);

              
            }

            if (message.Text.StartsWith("Coin management")) // send inline keyboard
            {
   
                ReplyMarkup keyboard = new ReplyKeyboardMarkup(new[]
{
                        new [] // first row
                        {
                            new KeyboardButton("< Return"),
                            new KeyboardButton("Claim Daily Bonus"),
                            new KeyboardButton("See your coins"),
                        },

                    });

                keyboard.Selective = true;

                await Bot.SendTextMessageAsync(
                    message.Chat.Id,
                    "Choose",
                    replyMarkup: keyboard, replyToMessageId: message.MessageId);

               
                
            }

            try //Lets make sure the user exists
            {
                var db = Database.GetCoins(message.From.Id);
                long coins = db.FirstOrDefault().Coins;
            } catch (Exception ex)//new user
            {
                Database.EnterUser(message.From.Id);
            }


            foreach (var item in menu.MenuItems)
            {
                //Tests for /beer == /beer, then if beer == beer (price doesn't matter)
                if (message.Text.ToLower().StartsWith(item.itemName.ToLower()) || message.Text.ToLower().StartsWith(item.command.ToLower()))
                {
                   
                    var keyboard = new ReplyKeyboardRemove();
                    keyboard.Selective = true;
                    var database = Database.GetCoins(message.From.Id);
                    long coins = database.FirstOrDefault().Coins;
                    
                    if(coins >= item.Cost)
                    {
                        string m1 = item.orderMessage;
                        string m2 = item.customMessage;
                        m1 = m1.Replace("$(0)", message.From.Username);
                        m1 = m1.Replace("$(1)", item.itemName);
                        m2 = m2.Replace("$(0)", message.From.Username);
                        m2 = m2.Replace("$(1)", item.itemName);
                        Console.WriteLine(m1);
                        await Bot.SendTextMessageAsync(message.Chat.Id, m1, replyMarkup: keyboard, replyToMessageId: message.MessageId);
                        await Bot.SendTextMessageAsync(message.Chat.Id, m2);
                        Database.RemoveCoins(message.From.Id, item.Cost);
                        return;
                    }
                }
            }

            if (message.Text.StartsWith("usid"))
            {
                await Bot.SendTextMessageAsync(message.Chat.Id,Convert.ToString(message.From.Id));
            }

            if (message.Text.StartsWith("#feedback"))
            {
               await Bot.ForwardMessageAsync(172791481, message.Chat.Id, message.MessageId);
            }
           

            if (message.Text.StartsWith("/menu") || message.Text.StartsWith("Place an order at the bar"))
            {
               
                ReplyMarkup keyboard = new ReplyKeyboardMarkup(new[]
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

                          }

                    });

                keyboard.Selective = true;


                await Bot.SendTextMessageAsync(
                    message.Chat.Id,
                    "What can I get you?",
                    replyMarkup: keyboard, replyToMessageId: message.MessageId);

               

            }

            if (message.Text.StartsWith("I wanna flirt with the bartender")) // send inline keyboard
            {
                Console.WriteLine(message.From.Username + " is flirting with me, help ");
                await Bot.SendTextMessageAsync(message.Chat.Id, "*Beats the life out of "+ "@" + message.From.Username + " *");
            }


            if (message.Text.StartsWith("/ave_horns")) // send inline keyboard
            {
                await Bot.SendTextMessageAsync(message.Chat.Id, "🦌 Deerhorn castle, best castle 2018! \n\nFor Queen Diego!");
                string file = "https://furrycentr.al/ah.webp";
                Telegram.Bot.Types.FileToSend Telegramfile = new Telegram.Bot.Types.FileToSend(file);
                await Bot.SendStickerAsync(message.Chat.Id, Telegramfile);
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
        public class Menu
        {
            public List<Order> MenuItems;
        }
        public class Order
        {
            public string itemName;
            public string command;
            public string orderMessage;
            public string customMessage;
            public int Cost;
        }
    }
}
