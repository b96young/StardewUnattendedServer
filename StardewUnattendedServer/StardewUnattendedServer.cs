using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using StardewUnattendedServer.Framework;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Characters;
using StardewValley.Locations;
using StardewValley.Menus;
using StardewValley.Network;
using StardewValley.Objects;
using SObject = StardewValley.Object;
using System.Runtime.CompilerServices;

namespace StardewUnattendedServer
{
    public class StardewUnattendedServer : Mod
    {
        /// <summary>The mod configuration from the player.</summary>
        private ModConfig Config = new ModConfig();

        private int gameTicks; //stores 1s game ticks for pause code
        private int skipTicks; //stores 1s game ticks for skip code
        private int gameClockTicks; //stores in game clock change 
        private int numPlayers; //stores number of players
        private bool IsEnabled;  //stores if the the server mod is enabled 
        public int bedX;
        public int bedY;
        public bool clientPaused;
        private string inviteCode = "a";
        private string inviteCodeTXT = "a";

        //debug tools
        private bool debug;

        private readonly Dictionary<string, int> PreviousFriendships = new Dictionary<string, int>();  //stores friendship values

        public int connectionsCount = 1;

        private bool eventCommandUsed;
        private string sleepKeyword = "!";
        private string festivalKeyword = "!";
        private string eventKeyword = "!";
        private string leaveKeyword = "!";
        private string unstickKeyword = "!";
        private string pauseKeyword = "!";
        private string unpauseKeyword = "!";
        private bool eggHuntAvailable; //is egg festival ready start timer for triggering eggHunt Event
        private int eggHuntCountDown; //to trigger egg hunt after set time

        private bool flowerDanceAvailable;
        private int flowerDanceCountDown;

        private bool luauSoupAvailable;
        private int luauSoupCountDown;

        private bool jellyDanceAvailable;
        private int jellyDanceCountDown;

        private bool grangeDisplayAvailable;
        private int grangeDisplayCountDown;

        private bool goldenPumpkinAvailable;
        private int goldenPumpkinCountDown;

        private bool iceFishingAvailable;
        private int iceFishingCountDown;

        private bool winterFeastAvailable;
        private int winterFeastCountDown;
        //variables for current time and date
        int currentTime = Game1.timeOfDay;
        SDate currentDate = SDate.Now();
        SDate eggFestival = new SDate(13, "spring");
        SDate dayAfterEggFestival = new SDate(14, "spring");
        SDate flowerDance = new SDate(24, "spring");
        SDate luau = new SDate(11, "summer");
        SDate danceOfJellies = new SDate(28, "summer");
        SDate stardewValleyFair = new SDate(16, "fall");
        SDate spiritsEve = new SDate(27, "fall");
        SDate festivalOfIce = new SDate(8, "winter");
        SDate feastOfWinterStar = new SDate(25, "winter");
        SDate grampasGhost = new SDate(1, "spring", 3);
        ///////////////////////////////////////////////////////





        //variables for timeout reset code

        private int timeOutTicksForReset;
        private int festivalTicksForReset;
        private int shippingMenuTimeoutTicks;


        SDate currentDateForReset = SDate.Now();
        SDate danceOfJelliesForReset = new SDate(28, "summer");
        SDate spiritsEveForReset = new SDate(27, "fall");
        //////////////////////////
        
        //handle shipping menu
        private bool shippingMenuActive = false;
        private int shippingMenuClickDelay = 60; // Prevents click spam waiting for shipping menu to load (game tick delay, 1 tick = 1/60s)
        private int shippingMenuTicksUntilClick = 0;



        public override void Entry(IModHelper helper)
        {

            this.Config = this.Helper.ReadConfig<ModConfig>();

            helper.ConsoleCommands.Add("server", "Toggles headless server on/off", this.ServerToggle);
            helper.ConsoleCommands.Add("debug_server", "Turns debug mode on/off, lets server run when no players are connected", this.DebugToggle);
            helper.Events.GameLoop.SaveLoaded += this.OnSaveLoaded;
            helper.Events.GameLoop.GameLaunched += this.OnGameLaunched;
            helper.Events.GameLoop.DayEnding += this.OnDayEnding;  // Shipping Menu handling (combined with OnUpdateTicked & OnSaving)
            helper.Events.GameLoop.Saving += this.OnSaving;
            helper.Events.GameLoop.OneSecondUpdateTicked += this.OnOneSecondUpdateTicked; //game tick event handler
            helper.Events.GameLoop.TimeChanged += this.OnTimeChanged; // Time of day change handler
            helper.Events.GameLoop.UpdateTicked += this.OnUpdateTicked; //handles various events that should occur as soon as they are available
            helper.Events.Input.ButtonPressed += this.OnButtonPressed;
            helper.Events.Display.Rendered += this.OnRendered;
            helper.Events.Specialized.UnvalidatedUpdateTicked += OnUnvalidatedUpdateTick; //used bc only thing that gets throug save window
            sleepKeyword += this.Config.sleepKeyword;
            festivalKeyword += this.Config.festivalKeyword;
            eventKeyword += this.Config.eventKeyword;
            leaveKeyword += this.Config.leaveKeyword;
            unstickKeyword += this.Config.unstickKeyword;
            pauseKeyword += this.Config.pauseKeyword;
            unpauseKeyword += this.Config.unpauseKeyword;
        }

        private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
        {
            ConfigMenu cm = new ConfigMenu(this, this.Config);
        }






        /// <summary>Raised after the player loads a save slot and the world is initialised.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
        {
            this.Config = this.Helper.ReadConfig<ModConfig>();
            // turns on server after the game loads
            if (Game1.IsServer)
            {
                // store levels, set in game levels to max
                var data = this.Helper.Data.ReadJsonFile<ModData>($"data/{Constants.SaveFolderName}.json") ?? new ModData();



                // Skill numbers
                int FarmingSkillNumber = Farmer.getSkillNumberFromName("farming");
                int MiningSkillNumber = Farmer.getSkillNumberFromName("mining");
                int ForagingSkillNumber = Farmer.getSkillNumberFromName("foraging");
                int FishingSkillNumber = Farmer.getSkillNumberFromName("fishing");
                int CombatSkillNumber = Farmer.getSkillNumberFromName("combat");

                // Levels
                data.FarmingLevel = Game1.player.FarmingLevel;
                data.MiningLevel = Game1.player.MiningLevel;
                data.ForagingLevel = Game1.player.ForagingLevel;
                data.FishingLevel = Game1.player.FishingLevel;
                data.CombatLevel = Game1.player.CombatLevel;

                //Experience
                data.FarmingExperience = Game1.player.experiencePoints[FarmingSkillNumber];
                data.MiningExperience = Game1.player.experiencePoints[MiningSkillNumber];
                data.ForagingExperience = Game1.player.experiencePoints[ForagingSkillNumber];
                data.FishingExperience = Game1.player.experiencePoints[FishingSkillNumber];
                data.CombatExperience = Game1.player.experiencePoints[CombatSkillNumber];

                this.Helper.Data.WriteJsonFile($"data/{Constants.SaveFolderName}.json", data);
                if (this.Config.autoLevel) {
                    if (data.FarmingLevel != 10)
                        Game1.player.setSkillLevel("Farming", 10);
                    if (data.MiningLevel != 10)
                        Game1.player.setSkillLevel("Mining", 10);
                    if (data.ForagingLevel != 10)
                        Game1.player.setSkillLevel("Foraging", 10);
                    if (data.FishingLevel != 10)
                        Game1.player.setSkillLevel("Fishing", 10);
                    if (data.CombatLevel != 10)
                        Game1.player.setSkillLevel("Combat", 10);
                }
                ////////////////////////////////////////
                IsEnabled = true;
                Game1.chatBox.addInfoMessage(Helper.Translation.Get("host.serverMode"));
                this.Monitor.Log("Server Mode On!", LogLevel.Info);
            }

        }

        //debug for running with no one online
        private void DebugToggle(string command, string[] args)
        {
            if (Context.IsWorldReady)
            {
                this.debug = !debug;
                this.Monitor.Log($"Server Debug {(debug ? "On" : "Off")}", LogLevel.Info);
            }
        }

        //draw textbox rules
        public static void DrawTextBox(int x, int y, SpriteFont font, string message, int align = 0, float colorIntensity = 1f)
        {
            SpriteBatch spriteBatch = Game1.spriteBatch;
            int width = (int)font.MeasureString(message).X + 32;
            int num = (int)font.MeasureString(message).Y + 21;
            switch (align)
            {
                case 0:
                    IClickableMenu.drawTextureBox(spriteBatch, Game1.menuTexture, new Rectangle(0, 256, 60, 60), x, y, width, num + 4, Color.White * colorIntensity);
                    Utility.drawTextWithShadow(spriteBatch, message, font, new Vector2(x + 16, y + 16), Game1.textColor);
                    break;
                case 1:
                    IClickableMenu.drawTextureBox(spriteBatch, Game1.menuTexture, new Rectangle(0, 256, 60, 60), x - width / 2, y, width, num + 4, Color.White * colorIntensity);
                    Utility.drawTextWithShadow(spriteBatch, message, font, new Vector2(x + 16 - width / 2, y + 16), Game1.textColor);
                    break;
                case 2:
                    IClickableMenu.drawTextureBox(spriteBatch, Game1.menuTexture, new Rectangle(0, 256, 60, 60), x - width, y, width, num + 4, Color.White * colorIntensity);
                    Utility.drawTextWithShadow(spriteBatch, message, font, new Vector2(x + 16 - width, y + 16), Game1.textColor);
                    break;
            }
        }

        /// <summary>Raised after the game draws to the sprite patch in a draw tick, just before the final sprite batch is rendered to the screen.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void OnRendered(object? sender, RenderedEventArgs e)
        {
            //draw a textbox in the top left corner saying Server On
            if (Game1.options.enableServer && IsEnabled)
            {
                int connectionsCount = Game1.server.connectionsCount;
                DrawTextBox(5, 100, Game1.dialogueFont, Helper.Translation.Get("server.on"));
                DrawTextBox(5, 180, Game1.dialogueFont, Helper.Translation.Get("server.key", new { key = this.Config.serverHotKey }));
                //int profitMargin = this.Config.profitmargin;
                DrawTextBox(5, 260, Game1.dialogueFont, Helper.Translation.Get("server.profit", new { profit = this.Config.profitmargin }));
                //DrawTextBox(5, 260, Game1.dialogueFont, $"Profit Margin: {profitMargin}%");
                DrawTextBox(5, 340, Game1.dialogueFont, Helper.Translation.Get("server.players", new { players = connectionsCount }));
                //DrawTextBox(5, 340, Game1.dialogueFont, $"{connectionsCount} Players Online");
                if (Game1.server.getInviteCode() != null)
                {
                    string inviteCode = Game1.server.getInviteCode();
                    DrawTextBox(5, 420, Game1.dialogueFont, $"Invite Code: {inviteCode}");
                }
            }
        }

        private void ToggleServer()
        {
            if (Context.IsWorldReady)
            {
                if (!IsEnabled)
                {
                    Helper.ReadConfig<ModConfig>();
                    IsEnabled = true;


                    this.Monitor.Log("Server Mode On!", LogLevel.Info);
                    Game1.chatBox.addInfoMessage(Helper.Translation.Get("host.serverMode"));

                    Game1.displayHUD = true;
                    Game1.addHUDMessage(new HUDMessage(Helper.Translation.Get("server.on")));

                    Game1.options.pauseWhenOutOfFocus = false;


                    // store levels, set in game levels to max
                    var data = this.Helper.Data.ReadJsonFile<ModData>($"data/{Constants.SaveFolderName}.json") ?? new ModData();

                    // Skill numbers
                    int FarmingSkillNumber = Farmer.getSkillNumberFromName("farming");
                    int MiningSkillNumber = Farmer.getSkillNumberFromName("mining");
                    int ForagingSkillNumber = Farmer.getSkillNumberFromName("foraging");
                    int FishingSkillNumber = Farmer.getSkillNumberFromName("fishing");
                    int CombatSkillNumber = Farmer.getSkillNumberFromName("combat");

                    // Levels
                    data.FarmingLevel = Game1.player.FarmingLevel;
                    data.MiningLevel = Game1.player.MiningLevel;
                    data.ForagingLevel = Game1.player.ForagingLevel;
                    data.FishingLevel = Game1.player.FishingLevel;
                    data.CombatLevel = Game1.player.CombatLevel;

                    //Experience
                    data.FarmingExperience = Game1.player.experiencePoints[FarmingSkillNumber];
                    data.MiningExperience = Game1.player.experiencePoints[MiningSkillNumber];
                    data.ForagingExperience = Game1.player.experiencePoints[ForagingSkillNumber];
                    data.FishingExperience = Game1.player.experiencePoints[FishingSkillNumber];
                    data.CombatExperience = Game1.player.experiencePoints[CombatSkillNumber];

                    this.Helper.Data.WriteJsonFile($"data/{Constants.SaveFolderName}.json", data);
                    if (this.Config.autoLevel) {
                        if (data.FarmingLevel != 10)
                            Game1.player.setSkillLevel("Farming", 10);
                        if (data.MiningLevel != 10)
                            Game1.player.setSkillLevel("Mining", 10);
                        if (data.ForagingLevel != 10)
                            Game1.player.setSkillLevel("Foraging", 10);
                        if (data.FishingLevel != 10)
                            Game1.player.setSkillLevel("Fishing", 10);
                        if (data.CombatLevel != 10)
                            Game1.player.setSkillLevel("Combat", 10);
                    }
                    ///////////////////////////////////////////
                    Game1.addHUDMessage(new HUDMessage(Helper.Translation.Get("server.complete")));
                    ///////////////////////////////////////////

                }
                else
                {
                    IsEnabled = false;
                    this.Monitor.Log("The server off!", LogLevel.Info);

                    Game1.chatBox.addInfoMessage(Helper.Translation.Get("host.returned"));

                    Game1.displayHUD = true;
                    Game1.addHUDMessage(new HUDMessage(Helper.Translation.Get("server.off")));

                    //set player levels to stored levels

                    // Skill numbers
                    int FarmingSkillNumber = Farmer.getSkillNumberFromName("farming");
                    int MiningSkillNumber = Farmer.getSkillNumberFromName("mining");
                    int ForagingSkillNumber = Farmer.getSkillNumberFromName("foraging");
                    int FishingSkillNumber = Farmer.getSkillNumberFromName("fishing");
                    int CombatSkillNumber = Farmer.getSkillNumberFromName("combat");

                    var data = this.Helper.Data.ReadJsonFile<ModData>($"data/{Constants.SaveFolderName}.json") ?? new ModData();

                    if (this.Config.autoLevel) {
                        // Set levels
                        Game1.player.farmingLevel.Value = data.FarmingLevel;
                        Game1.player.miningLevel.Value = data.MiningLevel;
                        Game1.player.foragingLevel.Value = data.ForagingLevel;
                        Game1.player.fishingLevel.Value = data.FishingLevel;
                        Game1.player.combatLevel.Value = data.CombatLevel;

                        // Set EXP
                        Game1.player.experiencePoints[FarmingSkillNumber] = data.FarmingExperience;
                        Game1.player.experiencePoints[MiningSkillNumber] = data.MiningExperience;
                        Game1.player.experiencePoints[ForagingSkillNumber] = data.ForagingExperience;
                        Game1.player.experiencePoints[FishingSkillNumber] = data.FishingExperience;
                        Game1.player.experiencePoints[CombatSkillNumber] = data.CombatExperience;
                    }
                    //////////////////////////////////////
                }
            }
        }

        // toggles server on/off with console command "server"
        private void ServerToggle(string command, string[] args)
        {
            ToggleServer();
        }



        /// <summary>Raised after the player presses a button on the keyboard, controller, or mouse.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
        {
            //toggles server on/off with configurable hotkey
            if (Context.IsWorldReady)
            {
                if (e.Button == this.Config.serverHotKey)
                {
                    ToggleServer();
                    //warp farmer on button press
                    if (Game1.player.currentLocation is FarmHouse)
                    {
                        Game1.warpFarmer("Farm", 64, 15, false);
                    }
                    else
                    {
                        getBedCoordinates();
                        Game1.warpFarmer("Farmhouse", bedX, bedY, false);
                    }
                }
            }
        }


        private void FestivalsToggle()
        {
            if (!this.Config.festivalsOn)
                return;
        }


        /// <summary>Raised once per second after the game state is updated.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void OnOneSecondUpdateTicked(object? sender, OneSecondUpdateTickedEventArgs e)
        {
            if (!IsEnabled) // server toggle
            {
                Game1.netWorldState.Value.IsPaused = false;
                return;
            }
            NoClientsPause();

            if (this.Config.clientsCanPause)
            {
                List<ChatMessage> messages = this.Helper.Reflection.GetField<List<ChatMessage>>(Game1.chatBox, "messages").GetValue();
                if (messages.Count > 0)
                {
                    var messagetoconvert = messages[messages.Count - 1].message;
                    string actualmessage = ChatMessage.makeMessagePlaintext(messagetoconvert, true);
                    string lastFragment = "";
                    if (actualmessage.Split(' ').Count() > 1)
                        lastFragment = actualmessage.Split(' ')[1];

                    if (lastFragment != null && lastFragment == pauseKeyword)
                    {
                        //Game1.netWorldState.Value.IsPaused = true;
                        //clientPaused = true;
                        this.SendChatMessage(Helper.Translation.Get("game.paused"));
                    }
                    if (lastFragment != null && lastFragment == unpauseKeyword)
                    {
                        //Game1.netWorldState.Value.IsPaused = false;
                        //clientPaused = false;
                        this.SendChatMessage(Helper.Translation.Get("game.unpaused"));
                    }
                }
            }



            //Invite Code Copier 
            if (this.Config.copyInviteCodeToClipboard)
            {

                if (Game1.options.enableServer)
                {
                    if (inviteCode != Game1.server.getInviteCode())
                    {
                        DesktopClipboard.SetText($"Invite Code: {Game1.server.getInviteCode()}");
                        inviteCode = Game1.server.getInviteCode();
                    }
                }
            }

            //write code to a InviteCode.txt in the StardewUnattendedServer mod folder
            if (Game1.options.enableServer)
            {
                if (inviteCodeTXT != Game1.server.getInviteCode())
                {


                    inviteCodeTXT = Game1.server.getInviteCode();

                    try
                    {

                        //Pass the filepath and filename to the StreamWriter Constructor
                        StreamWriter sw = new StreamWriter("Mods/StardewUnattendedServer/InviteCode.txt");

                        //Write a line of text
                        sw.WriteLine(inviteCodeTXT);
                        //Close the file
                        sw.Close();
                    }
                    catch (Exception b)
                    {
                        Console.WriteLine("Exception: " + b.Message);
                    }
                    finally
                    {
                        Console.WriteLine("Executing finally block.");
                    }

                }

            }
            //write number of players online to .txt
            if (Game1.options.enableServer)
            {

                if (connectionsCount != Game1.server.connectionsCount)
                {
                    connectionsCount = Game1.server.connectionsCount;

                    try
                    {

                        //Pass the filepath and filename to the StreamWriter Constructor
                        StreamWriter sw = new StreamWriter("Mods/StardewUnattendedServer/ConnectionsCount.txt");

                        //Write a line of text
                        sw.WriteLine(connectionsCount);
                        //Close the file
                        sw.Close();
                    }
                    catch (Exception b)
                    {
                        Console.WriteLine("Exception: " + b.Message);
                    }
                    finally
                    {
                        Console.WriteLine("Executing finally block.");
                    }

                }

            }

            //left click menu spammer and event skipper to get through random events happening
            //also moves player around, this seems to free host from random bugs sometimes
            if (IsEnabled) // server toggle
            {

                if (Game1.activeClickableMenu != null && Game1.activeClickableMenu is DialogueBox)
                {

                    Game1.activeClickableMenu.receiveLeftClick(10, 10);

                }
                if (Game1.CurrentEvent != null && Game1.CurrentEvent.skippable)
                {
                    skipTicks += 1; // If we spam skipEvent() it gets stuck.

                    if (skipTicks >= 3)
                    {
                        Game1.CurrentEvent.skipEvent();
                        skipTicks = 0;
                    }
                }
            }



            //disable friendship decay
            if (IsEnabled) // server toggle
            {
                if (this.PreviousFriendships.Any())
                {
                    foreach (string key in Game1.player.friendshipData.Keys)
                    {
                        Friendship friendship = Game1.player.friendshipData[key];
                        if (this.PreviousFriendships.TryGetValue(key, out int oldPoints) && oldPoints > friendship.Points)
                            friendship.Points = oldPoints;
                    }
                }

                this.PreviousFriendships.Clear();
                foreach (var pair in Game1.player.friendshipData.FieldDict)
                    this.PreviousFriendships[pair.Key] = pair.Value.Value.Points;
            }







            //eggHunt event
            if (eggHuntAvailable && Game1.CurrentEvent != null && Game1.CurrentEvent.isFestival)
            {
                if (eventCommandUsed)
                {
                    eggHuntCountDown = this.Config.eggHuntCountDownConfig;
                    eventCommandUsed = false;
                }
                eggHuntCountDown += 1;

                //float chatEgg = this.Config.eggHuntCountDownConfig / 60f;
                string chatEgg = $"{(this.Config.eggHuntCountDownConfig / 60f):0.#}";
                if (eggHuntCountDown == 1)
                {
                    this.SendChatMessage(Helper.Translation.Get("festival.eventBegin", new { eventName = Helper.Translation.Get("festival.event.egg"), eventTime = chatEgg}));
                    //this.SendChatMessage($"The Egg Hunt will begin in {chatEgg:0.#} minutes.");
                }

                if (eggHuntCountDown == this.Config.eggHuntCountDownConfig + 1)
                {
                    this.SendChatMessage(Helper.Translation.Get("festival.eventStarting", new { eventName = Helper.Translation.Get("festival.event.egg") }));
                    //this.SendChatMessage("The Egg Hunt is starting!!");
                    this.Helper.Reflection.GetMethod(Game1.CurrentEvent, "answerDialogueQuestion").Invoke(Game1.getCharacterFromName("Lewis"), "yes");
                }
                if (eggHuntCountDown >= this.Config.eggHuntCountDownConfig + 5)
                {
                    //festival timeout
                    festivalTicksForReset += 1;
                    if (festivalTicksForReset >= this.Config.eggFestivalTimeOut + 180)
                    {
                        Game1.options.setServerMode("offline");
                    }
                    ///////////////////////////////////////////////


                }
            }


            //flowerDance event
            if (flowerDanceAvailable && Game1.CurrentEvent != null && Game1.CurrentEvent.isFestival)
            {
                if (eventCommandUsed)
                {
                    flowerDanceCountDown = this.Config.flowerDanceCountDownConfig;
                    eventCommandUsed = false;
                }

                flowerDanceCountDown += 1;

                //float chatFlower = this.Config.flowerDanceCountDownConfig / 60f;
                string chatFlower = $"{(this.Config.flowerDanceCountDownConfig / 60f):0.#}";
                if (flowerDanceCountDown == 1)
                {
                    this.SendChatMessage(Helper.Translation.Get("festival.eventBegin", new { eventName = Helper.Translation.Get("festival.event.flower"), eventTime = chatFlower}));
                    //this.SendChatMessage($"The Flower Dance will begin in {chatFlower:0.#} minutes.");
                }

                if (flowerDanceCountDown == this.Config.flowerDanceCountDownConfig + 1)
                {
                    this.SendChatMessage(Helper.Translation.Get("festival.eventStarting", new { eventName = Helper.Translation.Get("festival.event.flower") }));
                    //this.SendChatMessage("The Flower Dance is starting!!");
                    this.Helper.Reflection.GetMethod(Game1.CurrentEvent, "answerDialogueQuestion").Invoke(Game1.getCharacterFromName("Lewis"), "yes");
                }
                if (flowerDanceCountDown >= this.Config.flowerDanceCountDownConfig + 5)
                {
                    //festival timeout
                    festivalTicksForReset += 1;
                    if (festivalTicksForReset >= this.Config.flowerDanceTimeOut + 90)
                    {
                        Game1.options.setServerMode("offline");
                    }
                    ///////////////////////////////////////////////

                }
            }

            //luauSoup event
            if (luauSoupAvailable && Game1.CurrentEvent != null && Game1.CurrentEvent.isFestival)
            {
                if (eventCommandUsed)
                {
                    luauSoupCountDown = this.Config.luauSoupCountDownConfig;
                    //add iridium starfruit to soup
                    var item = new SObject("Starfruit", 1, false, -1, 3);
                    this.Helper.Reflection.GetMethod(new Event(), "addItemToLuauSoup").Invoke(item, Game1.player);
                    eventCommandUsed = false;

                }

                luauSoupCountDown += 1;

                //float chatSoup = this.Config.luauSoupCountDownConfig / 60f;
                string chatSoup = $"{(this.Config.luauSoupCountDownConfig / 60f):0.#}";
                if (luauSoupCountDown == 1)
                {
                    this.SendChatMessage(Helper.Translation.Get("festival.eventBegin", new { eventName = Helper.Translation.Get("festival.event.luau"), eventTime = chatSoup}));
                    //this.SendChatMessage($"The Soup Tasting will begin in {chatSoup:0.#} minutes.");

                    //add iridium starfruit to soup
                    var item = new SObject("Starfruit", 1, false, -1, 3);
                    this.Helper.Reflection.GetMethod(new Event(), "addItemToLuauSoup").Invoke(item, Game1.player);

                }

                if (luauSoupCountDown == this.Config.luauSoupCountDownConfig + 1)
                {
                    this.SendChatMessage(Helper.Translation.Get("festival.eventStarting", new { eventName = Helper.Translation.Get("festival.event.luau") }));
                    //this.SendChatMessage("The Soup Tasting is starting!!");
                    this.Helper.Reflection.GetMethod(Game1.CurrentEvent, "answerDialogueQuestion").Invoke(Game1.getCharacterFromName("Lewis"), "yes");
                }
                if (luauSoupCountDown >= this.Config.luauSoupCountDownConfig + 5)
                {
                    //festival timeout
                    festivalTicksForReset += 1;
                    if (festivalTicksForReset >= this.Config.luauTimeOut + 80)
                    {
                        Game1.options.setServerMode("offline");
                    }
                    ///////////////////////////////////////////////


                }
            }

            //Dance of the Moonlight Jellies event
            if (jellyDanceAvailable && Game1.CurrentEvent != null && Game1.CurrentEvent.isFestival)
            {
                if (eventCommandUsed)
                {
                    jellyDanceCountDown = this.Config.jellyDanceCountDownConfig;
                    eventCommandUsed = false;
                }

                jellyDanceCountDown += 1;

                //float chatJelly = this.Config.jellyDanceCountDownConfig / 60f;
                string chatJelly = $"{(this.Config.jellyDanceCountDownConfig / 60f):0.#}";
                if (jellyDanceCountDown == 1)
                {
                    this.SendChatMessage(Helper.Translation.Get("festival.eventBegin", new { eventName = Helper.Translation.Get("festival.event.dance"), eventTime = chatJelly}));
                    //this.SendChatMessage($"The Dance of the Moonlight Jellies will begin in {chatJelly:0.#} minutes.");
                }

                if (jellyDanceCountDown == this.Config.jellyDanceCountDownConfig + 1)
                {
                    this.SendChatMessage(Helper.Translation.Get("festival.eventStarting", new { eventName = Helper.Translation.Get("festival.event.dance") }));
                    //this.SendChatMessage("The Dance of the Moonlight Jellies is starting!!");
                    this.Helper.Reflection.GetMethod(Game1.CurrentEvent, "answerDialogueQuestion").Invoke(Game1.getCharacterFromName("Lewis"), "yes");
                }
                if (jellyDanceCountDown >= this.Config.jellyDanceCountDownConfig + 5)
                {
                    //festival timeout
                    festivalTicksForReset += 1;
                    if (festivalTicksForReset >= this.Config.danceOfJelliesTimeOut + 180)
                    {
                        Game1.options.setServerMode("offline");
                    }
                    ///////////////////////////////////////////////

                }
            }

            //Grange Display event
            if (grangeDisplayAvailable && Game1.CurrentEvent != null && Game1.CurrentEvent.isFestival)
            {
                if (eventCommandUsed)
                {
                    grangeDisplayCountDown = this.Config.grangeDisplayCountDownConfig;
                    eventCommandUsed = false;
                }

                grangeDisplayCountDown += 1;
                festivalTicksForReset += 1;
                //festival timeout code
                if (festivalTicksForReset == this.Config.fairTimeOut - 120)
                {
                    this.SendChatMessage(Helper.Translation.Get("festival.ending"));
                }
                if (festivalTicksForReset >= this.Config.fairTimeOut)
                {
                    Game1.options.setServerMode("offline");
                }
                ///////////////////////////////////////////////
                //float chatGrange = this.Config.grangeDisplayCountDownConfig / 60f;
                string chatGrange = $"{(this.Config.grangeDisplayCountDownConfig / 60f):0.#}";
                if (grangeDisplayCountDown == 1)
                {
                    this.SendChatMessage(Helper.Translation.Get("festival.eventBegin", new { eventName = Helper.Translation.Get("festival.event.grange"), eventTime = chatGrange}));
                    //this.SendChatMessage($"The Grange Judging will begin in {chatGrange:0.#} minutes.");
                }

                if (grangeDisplayCountDown == this.Config.grangeDisplayCountDownConfig + 1)
                {
                    this.SendChatMessage(Helper.Translation.Get("festival.eventStarting", new { eventName = Helper.Translation.Get("festival.event.grange") }));
                    this.Helper.Reflection.GetMethod(Game1.CurrentEvent, "answerDialogueQuestion").Invoke(Game1.getCharacterFromName("Lewis"), "yes");
                }
                if (grangeDisplayCountDown == this.Config.grangeDisplayCountDownConfig + 5)
                    this.LeaveFestival();
            }

            //golden pumpkin maze event
            if (goldenPumpkinAvailable && Game1.CurrentEvent != null && Game1.CurrentEvent.isFestival)
            {
                goldenPumpkinCountDown += 1;
                festivalTicksForReset += 1;
                //festival timeout code
                if (festivalTicksForReset == this.Config.spiritsEveTimeOut - 120)
                {
                    this.SendChatMessage(Helper.Translation.Get("festival.ending"));
                }
                if (festivalTicksForReset >= this.Config.spiritsEveTimeOut)
                {
                    Game1.options.setServerMode("offline");
                }
                ///////////////////////////////////////////////
                if (goldenPumpkinCountDown == 10)
                    this.LeaveFestival();
            }

            //ice fishing event
            if (iceFishingAvailable && Game1.CurrentEvent != null && Game1.CurrentEvent.isFestival)
            {
                if (eventCommandUsed)
                {
                    iceFishingCountDown = this.Config.iceFishingCountDownConfig;
                    eventCommandUsed = false;
                }
                iceFishingCountDown += 1;

                string chatIceFish = $"{(this.Config.iceFishingCountDownConfig / 60f):0.#}";
                if (iceFishingCountDown == 1)
                {
                    this.SendChatMessage(Helper.Translation.Get("festival.eventBegin", new { eventName =  Helper.Translation.Get("festival.event.ice"), eventTime = chatIceFish}));
                }

                if (iceFishingCountDown == this.Config.iceFishingCountDownConfig + 1)
                {
                    this.SendChatMessage(Helper.Translation.Get("festival.eventStarting", new { eventName = Helper.Translation.Get("festival.event.ice") }));
                    this.Helper.Reflection.GetMethod(Game1.CurrentEvent, "answerDialogueQuestion").Invoke(Game1.getCharacterFromName("Lewis"), "yes");
                }
                if (iceFishingCountDown >= this.Config.iceFishingCountDownConfig + 5)
                {
                    //festival timeout
                    festivalTicksForReset += 1;
                    if (festivalTicksForReset >= this.Config.festivalOfIceTimeOut + 180)
                    {
                        Game1.options.setServerMode("offline");
                    }
                    ///////////////////////////////////////////////

                }
            }

            //Feast of the Winter event
            if (winterFeastAvailable && Game1.CurrentEvent != null && Game1.CurrentEvent.isFestival)
            {
                winterFeastCountDown += 1;
                festivalTicksForReset += 1;
                //festival timeout code
                if (festivalTicksForReset == this.Config.winterStarTimeOut - 120)
                {
                    this.SendChatMessage(Helper.Translation.Get("festival.ending"));
                }
                if (festivalTicksForReset >= this.Config.winterStarTimeOut)
                {
                    Game1.options.setServerMode("offline");
                }
                ///////////////////////////////////////////////
                if (winterFeastCountDown == 10)
                    this.LeaveFestival();
            }
        }



        //Pause game if no clients Code
        private void NoClientsPause()
        {

            gameTicks += 1;

            if (gameTicks >= 3)
            {
                this.numPlayers = Game1.otherFarmers.Count;

                if (numPlayers >= 1 || debug)
                {
                    if (clientPaused)
                    {
                        Game1.netWorldState.Value.IsPaused = true;
                    }
                    else
                    {
                        Game1.netWorldState.Value.IsPaused = false;
                    }
                }
                else if (numPlayers <= 0 && Game1.timeOfDay >= 610 && Game1.timeOfDay <= 2500 && currentDate != eggFestival && currentDate != flowerDance && currentDate != luau && currentDate != danceOfJellies && currentDate != stardewValleyFair && currentDate != spiritsEve && currentDate != festivalOfIce && currentDate != feastOfWinterStar)
                {
                    Game1.netWorldState.Value.IsPaused = true;
                }

                gameTicks = 0;
            }


            //handles client commands for sleep, go to festival, start festival event.
            if (Context.IsWorldReady && IsEnabled)
            {
                List<ChatMessage> messages = this.Helper.Reflection.GetField<List<ChatMessage>>(Game1.chatBox, "messages").GetValue();
                if (messages.Count > 0)
                {
                    var messagetoconvert = messages[messages.Count - 1].message;
                    string actualmessage = ChatMessage.makeMessagePlaintext(messagetoconvert, true);
                    string lastFragment = "";
                    if (actualmessage.Split(' ').Count() > 1)
                        lastFragment = actualmessage.Split(' ')[1];

                    if (lastFragment != "")
                    {
                        if (lastFragment == sleepKeyword)
                        {
                            if (currentTime >= this.Config.timeOfDayToSleep)
                            {
                                GoToBed();
                                this.SendChatMessage(Helper.Translation.Get("host.toSleep"));
                            }
                            else
                            {
                                this.SendChatMessage(Helper.Translation.Get("sleep.tooEarly"));
                                this.SendChatMessage(Helper.Translation.Get("sleep.after", new { sleepTime = this.Config.timeOfDayToSleep}));
                            }
                        }
                        if (lastFragment == festivalKeyword)
                        {
                            this.SendChatMessage(Helper.Translation.Get("host.toFestival"));

                            if (currentDate == eggFestival)
                            {
                                EggFestival();

                            }
                            else if (currentDate == flowerDance)
                            {
                                FlowerDance();
                            }
                            else if (currentDate == luau)
                            {
                                Luau();
                            }
                            else if (currentDate == danceOfJellies)
                            {
                                DanceOfTheMoonlightJellies();
                            }
                            else if (currentDate == stardewValleyFair)
                            {
                                StardewValleyFair();
                            }
                            else if (currentDate == spiritsEve)
                            {
                                SpiritsEve();
                            }
                            else if (currentDate == festivalOfIce)
                            {
                                FestivalOfIce();
                            }
                            else if (currentDate == feastOfWinterStar)
                            {
                                FeastOfWinterStar();
                            }
                            else
                            {
                                this.SendChatMessage(Helper.Translation.Get("festival.notReady"));
                            }
                        }
                        if (lastFragment == eventKeyword)
                        {
                            if (Game1.CurrentEvent != null && Game1.CurrentEvent.isFestival)
                            {
                                if (currentDate == eggFestival)
                                {
                                    eventCommandUsed = true;
                                    eggHuntAvailable = true;
                                }
                                else if (currentDate == flowerDance)
                                {
                                    eventCommandUsed = true;
                                    flowerDanceAvailable = true;
                                }
                                else if (currentDate == luau)
                                {
                                    eventCommandUsed = true;
                                    luauSoupAvailable = true;
                                }
                                else if (currentDate == danceOfJellies)
                                {
                                    eventCommandUsed = true;
                                    jellyDanceAvailable = true;
                                }
                                else if (currentDate == stardewValleyFair)
                                {
                                    eventCommandUsed = true;
                                    grangeDisplayAvailable = true;
                                }
                                else if (currentDate == spiritsEve)
                                {
                                    eventCommandUsed = true;
                                    goldenPumpkinAvailable = true;
                                }
                                else if (currentDate == festivalOfIce)
                                {
                                    eventCommandUsed = true;
                                    iceFishingAvailable = true;
                                }
                                else if (currentDate == feastOfWinterStar)
                                {
                                    eventCommandUsed = true;
                                    winterFeastAvailable = true;
                                }
                            }
                            else
                            {
                                this.SendChatMessage(Helper.Translation.Get("host.noFestival"));
                            }
                        }
                        if (lastFragment == leaveKeyword)
                        {
                            if (Game1.CurrentEvent != null && Game1.CurrentEvent.isFestival)
                            {
                                this.SendChatMessage(Helper.Translation.Get("host.leaveFestival"));
                                this.LeaveFestival();
                            }
                            else
                            {
                                this.SendChatMessage(Helper.Translation.Get("host.noFestival"));
                            }
                        }
                        if (lastFragment == unstickKeyword)
                        {
                            if (Game1.player.currentLocation is FarmHouse)
                            {
                                this.SendChatMessage(Helper.Translation.Get("warp.farm"));
                                Game1.warpFarmer("Farm", 64, 15, false);
                            }
                            else
                            {
                                this.SendChatMessage(Helper.Translation.Get("warp.house"));
                                getBedCoordinates();
                                Game1.warpFarmer("Farmhouse", bedX, bedY, false);
                            }
                        }

                    }
                }
            }
        }


        /// <summary>Raised after the game state is updated (≈60 times per second).</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
        {
            if (IsEnabled)
            {
                // If shipping menu is on the screen, click OK
                if (shippingMenuActive)
                {
                    //Wait shippingMenuDelay before clicking
                    if (shippingMenuTicksUntilClick > 0)
                    {
                        shippingMenuTicksUntilClick--;
                    }
                    else
                    {
                        // Check if the active menu is the ShippingMenu
                        if (Game1.activeClickableMenu is ShippingMenu shippingMenu)
                        {
                            this.Monitor.Log("Shipping Menu detected", LogLevel.Info);

                            // Get the OK button from the ShippingMenu
                            var okButton = shippingMenu.okButton;

                            // If the button is available, simulate the click
                            if (okButton != null)
                            {
                                this.Monitor.Log("Clicking OK on shipping menu", LogLevel.Info);

                                // Simulate a left-click action on the OK button
                                shippingMenu.receiveLeftClick(okButton.bounds.X, okButton.bounds.Y, true);

                                // Reset the shipping menu click delay
                                shippingMenuTicksUntilClick = shippingMenuClickDelay;

                            }
                        }
                    }
                }
            
                //lockPlayerChests
                if (this.Config.lockPlayerChests)
                {
                    foreach (Farmer farmer in Game1.getOnlineFarmers())
                    {
                        if (farmer.currentLocation is Cabin cabin && farmer != cabin.owner)
                        {
                            //locks player inventories
                            NetMutex playerinventory = this.Helper.Reflection.GetField<NetMutex>(cabin, "inventoryMutex").GetValue();
                            playerinventory.RequestLock();

                            //locks all chests
                            foreach (SObject x in cabin.objects.Values)
                            {
                                if (x is Chest chest)
                                {
                                    //removed, the game stores color id's strangely, other colored chests randomly unlocking
                                    /*if (chest.playerChoiceColor.Value.Equals(unlockedChestColor)) 
                                    {
                                        return;
                                    }*/
                                    //else
                                    {
                                        chest.mutex.RequestLock();
                                    }
                                }
                            }
                            //locks fridge
                            cabin.fridge.Value.mutex.RequestLock();
                        }
                    }

                }


                //petchoice
                if (!Game1.player.hasPet())
                {
                    this.Helper.Reflection.GetMethod(new Event(), "namePet").Invoke(this.Config.petname.Substring(0));
                }
                if (Game1.player.hasPet() && Game1.getCharacterFromName(Game1.player.getPetName()) is Pet pet)
                {
                    pet.Name = this.Config.petname.Substring(0);
                    pet.displayName = this.Config.petname.Substring(0);
                }
                //cave choice unlock 
                if (!Game1.player.eventsSeen.Contains("65"))
                {
                    Game1.player.eventsSeen.Add("65");


                    if (this.Config.farmcavechoicemushrooms)
                    {
                        Game1.MasterPlayer.caveChoice.Value = 2;
                        ((FarmCave)Game1.getLocationFromName("FarmCave")).setUpMushroomHouse();
                    }
                    else
                    {
                        Game1.MasterPlayer.caveChoice.Value = 1;
                    }
                }
                //community center unlock
                if (!Game1.player.eventsSeen.Contains("611439"))
                {

                    Game1.player.eventsSeen.Add("611439");
                    Game1.MasterPlayer.mailReceived.Add("ccDoorUnlock");
                }
                if (this.Config.upgradeHouse != 0 && Game1.player.HouseUpgradeLevel != this.Config.upgradeHouse)
                {
                    Game1.player.HouseUpgradeLevel = this.Config.upgradeHouse;
                }
                // just turns off server mod if the game gets exited back to title screen
                if (Game1.activeClickableMenu is TitleMenu)
                {
                    IsEnabled = false;
                }
            }
        }


        /// <summary>Raised after the in-game clock time changes.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        public void OnTimeChanged(object? sender, TimeChangedEventArgs e)
        {
            // auto-sleep and Holiday code
            currentTime = Game1.timeOfDay;
            currentDate = SDate.Now();
            eggFestival = new SDate(13, "spring");
            dayAfterEggFestival = new SDate(14, "spring");
            flowerDance = new SDate(24, "spring");
            luau = new SDate(11, "summer");
            danceOfJellies = new SDate(28, "summer");
            stardewValleyFair = new SDate(16, "fall");
            spiritsEve = new SDate(27, "fall");
            festivalOfIce = new SDate(8, "winter");
            feastOfWinterStar = new SDate(25, "winter");
            grampasGhost = new SDate(1, "spring", 3);
            if (IsEnabled)
            {
                gameClockTicks += 1;

                if (gameClockTicks >= 3)
                {
                    if (currentDate == eggFestival && (numPlayers >= 1 || debug))   //set back to 1 after testing~
                    {
                        FestivalsToggle();

                        if (currentTime >= 600 && currentTime <= 630)
                        {
                            this.SendChatMessage(Helper.Translation.Get("festival.egg"));
                            this.SendChatMessage(Helper.Translation.Get("festival.inBed", new { time = "2:00 P.M." }));

                        }
                        EggFestival();
                    }


                    //flower dance message changed to disabled bc it causes crashes
                    else if (currentDate == flowerDance && numPlayers >= 1)
                    {
                        FestivalsToggle();

                        if (currentTime >= 600 && currentTime <= 630)
                        {
                            this.SendChatMessage(Helper.Translation.Get("festival.flowerDance"));
                            this.SendChatMessage(Helper.Translation.Get("festival.inBed", new { time = "2:00 P.M." }));

                        }
                        FlowerDance();
                    }

                    else if (currentDate == luau && numPlayers >= 1)
                    {
                        FestivalsToggle();

                        if (currentTime >= 600 && currentTime <= 630)
                        {
                            this.SendChatMessage(Helper.Translation.Get("festival.luau"));
                            this.SendChatMessage(Helper.Translation.Get("festival.inBed", new { time = "2:00 P.M." }));
                        }
                        Luau();
                    }

                    else if (currentDate == danceOfJellies && numPlayers >= 1)
                    {
                        FestivalsToggle();

                        if (currentTime >= 600 && currentTime <= 630)
                        {
                            this.SendChatMessage(Helper.Translation.Get("festival.jellyDance"));
                            this.SendChatMessage(Helper.Translation.Get("festival.inBed", new { time = "12:00 A.M." }));
                        }
                        DanceOfTheMoonlightJellies();
                    }

                    else if (currentDate == stardewValleyFair && numPlayers >= 1)
                    {
                        FestivalsToggle();

                        if (currentTime >= 600 && currentTime <= 630)
                        {
                            this.SendChatMessage(Helper.Translation.Get("festival.fair"));
                            this.SendChatMessage(Helper.Translation.Get("festival.inBed", new { time = "3:00 P.M." }));
                        }
                        StardewValleyFair();
                    }

                    else if (currentDate == spiritsEve && numPlayers >= 1)
                    {
                        FestivalsToggle();

                        if (currentTime >= 600 && currentTime <= 630)
                        {
                            this.SendChatMessage(Helper.Translation.Get("festival.spiritsEve"));
                            this.SendChatMessage(Helper.Translation.Get("festival.inBed", new { time = "12:00 A.M." }));
                        }
                        SpiritsEve();
                    }

                    else if (currentDate == festivalOfIce && numPlayers >= 1)
                    {
                        FestivalsToggle();

                        if (currentTime >= 600 && currentTime <= 630)
                        {
                            this.SendChatMessage(Helper.Translation.Get("festival.ice"));
                            this.SendChatMessage(Helper.Translation.Get("festival.inBed", new { time = "2:00 P.M." }));
                        }
                        FestivalOfIce();
                    }

                    else if (currentDate == feastOfWinterStar && numPlayers >= 1)
                    {
                        FestivalsToggle();

                        if (currentTime >= 600 && currentTime <= 630)
                        {
                            this.SendChatMessage(Helper.Translation.Get("festival.winterStar"));
                            this.SendChatMessage(Helper.Translation.Get("festival.inBed", new { time = "2:00 P.M." }));
                        }
                        FeastOfWinterStar();
                    }

                    else if (currentTime >= this.Config.timeOfDayToSleep && numPlayers >= 1)  //turn back to 1 after testing
                    {
                        GoToBed();
                    }

                    gameClockTicks = 0;
                }
            }

            //handles various events that the host normally has to click through
            if (IsEnabled)
            {

                if (currentDate != grampasGhost && currentDate != eggFestival && currentDate != flowerDance && currentDate != luau && currentDate != danceOfJellies && currentDate != stardewValleyFair && currentDate != spiritsEve && currentDate != festivalOfIce && currentDate != feastOfWinterStar)
                {
                    if (currentTime == 620)
                    {
                        //check mail 10 a day
                        for (int i = 0; i < 10; i++)
                        {
                            this.Helper.Reflection.GetMethod(Game1.currentLocation, "mailbox").Invoke();
                        }
                    }
                    if (currentTime == 630)
                    {

                        //rustkey-sewers unlock
                        if (!Game1.player.hasRustyKey)
                        {
                            int museumItemCount = Game1.netWorldState.Value.MuseumPieces.Length;
                            this.Monitor.Log("Checking museum items: " + museumItemCount.ToString(), LogLevel.Info);
                            if (museumItemCount >= 60)
                            {
                                Game1.player.eventsSeen.Add("295672");
                                Game1.player.eventsSeen.Add("66");
                                Game1.player.hasRustyKey = true;
                            }
                        }


                        //community center complete
                        if (this.Config.communitycenterrun)
                        {
                            if (!Game1.player.eventsSeen.Contains("191393") && Game1.player.mailReceived.Contains("ccCraftsRoom") && Game1.player.mailReceived.Contains("ccVault") && Game1.player.mailReceived.Contains("ccFishTank") && Game1.player.mailReceived.Contains("ccBoilerRoom") && Game1.player.mailReceived.Contains("ccPantry") && Game1.player.mailReceived.Contains("ccBulletin"))
                            {
                                CommunityCenter locationFromName = (CommunityCenter)Game1.getLocationFromName("CommunityCenter");
                                for (int index = 0; index < locationFromName.areasComplete.Count; ++index)
                                    locationFromName.areasComplete[index] = true;
                                Game1.player.eventsSeen.Add("191393");

                            }
                        }
                        //Joja run 
                        if (!this.Config.communitycenterrun)
                        {
                            if (Game1.player.Money >= 10000 && !Game1.player.mailReceived.Contains("JojaMember"))
                            {
                                Game1.player.Money -= 5000;
                                Game1.player.mailReceived.Add("JojaMember");
                                this.SendChatMessage(Helper.Translation.Get("jjrun.membership"));

                            }

                            if (Game1.player.Money >= 30000 && !Game1.player.mailReceived.Contains("jojaBoilerRoom"))
                            {
                                Game1.player.Money -= 15000;
                                Game1.player.mailReceived.Add("ccBoilerRoom");
                                Game1.player.mailReceived.Add("jojaBoilerRoom");
                                this.SendChatMessage(Helper.Translation.Get("jjrun.minecarts"));

                            }

                            if (Game1.player.Money >= 40000 && !Game1.player.mailReceived.Contains("jojaFishTank"))
                            {
                                Game1.player.Money -= 20000;
                                Game1.player.mailReceived.Add("ccFishTank");
                                Game1.player.mailReceived.Add("jojaFishTank");
                                this.SendChatMessage(Helper.Translation.Get("jjrun.panning"));

                            }

                            if (Game1.player.Money >= 50000 && !Game1.player.mailReceived.Contains("jojaCraftsRoom"))
                            {
                                Game1.player.Money -= 25000;
                                Game1.player.mailReceived.Add("ccCraftsRoom");
                                Game1.player.mailReceived.Add("jojaCraftsRoom");
                                this.SendChatMessage(Helper.Translation.Get("jjrun.bridge"));

                            }

                            if (Game1.player.Money >= 70000 && !Game1.player.mailReceived.Contains("jojaPantry"))
                            {
                                Game1.player.Money -= 35000;
                                Game1.player.mailReceived.Add("ccPantry");
                                Game1.player.mailReceived.Add("jojaPantry");
                                this.SendChatMessage(Helper.Translation.Get("jjrun.greenhouse"));

                            }

                            if (Game1.player.Money >= 80000 && !Game1.player.mailReceived.Contains("jojaVault"))
                            {
                                Game1.player.Money -= 40000;
                                Game1.player.mailReceived.Add("ccVault");
                                Game1.player.mailReceived.Add("jojaVault");
                                this.SendChatMessage(Helper.Translation.Get("jjrun.bus"));
                                Game1.player.eventsSeen.Add("502261");
                            }
                        }

                    }
                    //go outside
                    if (currentTime == 640)
                    {
                        Game1.warpFarmer("Farm", 64, 15, false);
                    }
                    //get fishing rod (standard spam clicker will get through cutscene)
                    if (currentTime == 900 && !Game1.player.eventsSeen.Contains("739330"))
                    {
                        Game1.player.increaseBackpackSize(1);
                        Game1.warpFarmer("Beach", 1, 20, 1);
                    }
                }
            }
        }

        public void EggFestival()
        {
            if (currentTime >= 900 && currentTime <= 1400)
            {



                //teleports to egg festival
                Game1.netReady.SetLocalReady("festivalStart", true);
                Game1.activeClickableMenu = new ReadyCheckDialog("festivalStart", true, who =>
                {
                    Game1.exitActiveMenu();
                    Game1.warpFarmer("Town", 1, 20, 1);

                });

                eggHuntAvailable = true;

            }
            else if (currentTime >= 1410)
            {

                eggHuntAvailable = false;
                Game1.options.setServerMode("online");
                eggHuntCountDown = 0;
                festivalTicksForReset = 0;
                GoToBed();
            }
        }


        public void FlowerDance()
        {
            if (currentTime >= 900 && currentTime <= 1400)
            {

                Game1.netReady.SetLocalReady("festivalStart", true);
                Game1.activeClickableMenu = new ReadyCheckDialog("festivalStart", true, who =>
                {
                    Game1.exitActiveMenu();
                    Game1.warpFarmer("Forest", 1, 20, 1);
                });

                flowerDanceAvailable = true;

            }
            else if (currentTime >= 1410 && currentTime >= this.Config.timeOfDayToSleep)
            {

                flowerDanceAvailable = false;
                Game1.options.setServerMode("online");
                flowerDanceCountDown = 0;
                festivalTicksForReset = 0;
                GoToBed();
            }
        }

        public void Luau()
        {

            if (currentTime >= 900 && currentTime <= 1400)
            {

                Game1.netReady.SetLocalReady("festivalStart", true);
                Game1.activeClickableMenu = new ReadyCheckDialog("festivalStart", true, who =>
                {
                    Game1.exitActiveMenu();
                    Game1.warpFarmer("Beach", 1, 20, 1);

                });

                luauSoupAvailable = true;

            }
            else if (currentTime >= 1410)
            {

                luauSoupAvailable = false;
                Game1.options.setServerMode("online");
                luauSoupCountDown = 0;
                festivalTicksForReset = 0;
                GoToBed();
            }
        }

        public void DanceOfTheMoonlightJellies()
        {


            if (currentTime >= 2200 && currentTime <= 2400)
            {


                Game1.netReady.SetLocalReady("festivalStart", true);
                Game1.activeClickableMenu = new ReadyCheckDialog("festivalStart", true, who =>
                {
                    Game1.exitActiveMenu();
                    Game1.warpFarmer("Beach", 1, 20, 1);

                });

                jellyDanceAvailable = true;

            }
            else if (currentTime >= 2410)
            {

                jellyDanceAvailable = false;
                Game1.options.setServerMode("online");
                jellyDanceCountDown = 0;
                festivalTicksForReset = 0;
                GoToBed();
            }
        }

        public void StardewValleyFair()
        {
            if (currentTime >= 900 && currentTime <= 1500)
            {



                Game1.netReady.SetLocalReady("festivalStart", true);
                Game1.activeClickableMenu = new ReadyCheckDialog("festivalStart", true, who =>
                {
                    Game1.exitActiveMenu();
                    Game1.warpFarmer("Town", 1, 20, 1);

                });

                grangeDisplayAvailable = true;

            }
            else if (currentTime >= 1510)
            {

                Game1.displayHUD = true;
                grangeDisplayAvailable = false;
                Game1.options.setServerMode("online");
                grangeDisplayCountDown = 0;
                festivalTicksForReset = 0;

                GoToBed();
            }
        }

        public void SpiritsEve()
        {


            if (currentTime >= 2200 && currentTime <= 2350)
            {



                Game1.netReady.SetLocalReady("festivalStart", true);
                Game1.activeClickableMenu = new ReadyCheckDialog("festivalStart", true, who =>
                {
                    Game1.exitActiveMenu();
                    Game1.warpFarmer("Town", 1, 20, 1);

                });

                goldenPumpkinAvailable = true;

            }
            else if (currentTime >= 2400)
            {

                Game1.displayHUD = true;
                goldenPumpkinAvailable = false;
                Game1.options.setServerMode("online");
                goldenPumpkinCountDown = 0;
                festivalTicksForReset = 0;
                GoToBed();
            }
        }

        public void FestivalOfIce()
        {
            if (currentTime >= 900 && currentTime <= 1400)
            {


                Game1.netReady.SetLocalReady("festivalStart", true);
                Game1.activeClickableMenu = new ReadyCheckDialog("festivalStart", true, who =>
                {
                    Game1.exitActiveMenu();
                    Game1.warpFarmer("Forest", 1, 20, 1);

                });

                iceFishingAvailable = true;

            }
            else if (currentTime >= 1410)
            {

                iceFishingAvailable = false;
                Game1.options.setServerMode("online");
                iceFishingCountDown = 0;
                festivalTicksForReset = 0;
                GoToBed();
            }
        }

        public void FeastOfWinterStar()
        {
            if (currentTime >= 900 && currentTime <= 1400)
            {


                Game1.netReady.SetLocalReady("festivalStart", true);
                Game1.activeClickableMenu = new ReadyCheckDialog("festivalStart", true, who =>
                {
                    Game1.exitActiveMenu();
                    Game1.warpFarmer("Town", 1, 20, 1);

                });

                winterFeastAvailable = true;

            }
            else if (currentTime >= 1410)
            {

                winterFeastAvailable = false;
                Game1.options.setServerMode("online");
                winterFeastCountDown = 0;
                festivalTicksForReset = 0;
                GoToBed();
            }
        }






        private void getBedCoordinates()
        {
            int houseUpgradeLevel = Game1.player.HouseUpgradeLevel;
            if (houseUpgradeLevel == 0)
            {
                bedX = 9;
                bedY = 9;
            }
            else if (houseUpgradeLevel == 1)
            {
                bedX = 21;
                bedY = 4;
            }
            else
            {
                bedX = 27;
                bedY = 13;
            }
        }

        private void GoToBed()
        {
            getBedCoordinates();

            Game1.warpFarmer("Farmhouse", bedX, bedY, false);

            this.Helper.Reflection.GetMethod(Game1.currentLocation, "startSleep").Invoke();
            Game1.displayHUD = true;
        }


        // Handles closing the shipping menu after going to bed (shipping menu appears before OnSaving is called in the game)
        private void OnDayEnding(object? sender, EventArgs e)
        {
            // Log to check if we're entering the DayEnding event
            this.Monitor.Log("Day Ending - Closing Shipping Menu", LogLevel.Info);
            // At end of day trigger onUpdateTicked to check for the shipping menu
            shippingMenuActive = true;
        }

        /// <summary>Raised before the game begins writes data to the save file (except the initial save creation).</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void OnSaving(object? sender, SavingEventArgs e)
        {
            // After shipping menu is clicked, set to false to stop clicking after game is saved
            shippingMenuActive = false;
            if (!IsEnabled) // server toggle
                return;
        }

        /// <summary>Raised after the game state is updated (≈60 times per second), regardless of normal SMAPI validation. This event is not thread-safe and may be invoked while game logic is running asynchronously. Changes to game state in this method may crash the game or corrupt an in-progress save. Do not use this event unless you're fully aware of the context in which your code will be run. Mods using this event will trigger a stability warning in the SMAPI console.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void OnUnvalidatedUpdateTick(object? sender, UnvalidatedUpdateTickedEventArgs e)
        {
            //resets server connection after certain amount of time end of day
            if (Game1.timeOfDay >= this.Config.timeOfDayToSleep || Game1.timeOfDay == 600 && currentDateForReset != danceOfJelliesForReset && currentDateForReset != spiritsEveForReset && this.Config.endofdayTimeOut != 0)
            {

                timeOutTicksForReset += 1;
                var countdowntoreset = (2600 - this.Config.timeOfDayToSleep) * .01 * 6 * 7 * 60;
                if (timeOutTicksForReset >= (countdowntoreset + (this.Config.endofdayTimeOut * 60)))
                {
                    Game1.options.setServerMode("offline");
                }
            }
            if (currentDateForReset == danceOfJelliesForReset || currentDateForReset == spiritsEveForReset && this.Config.endofdayTimeOut != 0)
            {
                if (Game1.timeOfDay >= 2400 || Game1.timeOfDay == 600)
                {

                    timeOutTicksForReset += 1;
                    if (timeOutTicksForReset >= (5040 + (this.Config.endofdayTimeOut * 60)))
                    {
                        Game1.options.setServerMode("offline");
                    }
                }

            }
            if (shippingMenuActive && this.Config.endofdayTimeOut != 0)
            {

                shippingMenuTimeoutTicks += 1;
                if (shippingMenuTimeoutTicks >= this.Config.endofdayTimeOut * 60)
                {
                    Game1.options.setServerMode("offline");
                }

            }

            if (Game1.timeOfDay == 610)
            {
                shippingMenuActive = false;
                Game1.player.difficultyModifier = this.Config.profitmargin * .01f;

                Game1.options.setServerMode("online");
                timeOutTicksForReset = 0;
                shippingMenuTimeoutTicks = 0;
            }

            if (Game1.timeOfDay == 2600)
            {
                Game1.paused = false;
            }
        }

        /// <summary>Send a chat message.</summary>
        /// <param name="message">The message text.</param>
        private void SendChatMessage(string message)
        {
            Game1.chatBox.activate();
            Game1.chatBox.setText(message);
            Game1.chatBox.chatBox.RecieveCommandInput('\r');
        }

        /// <summary>Leave the current festival, if any.</summary>
        private void LeaveFestival()
        {
            Game1.netReady.SetLocalReady("festivalEnd", true);
            Game1.activeClickableMenu = new ReadyCheckDialog("festivalEnd", true, who =>
            {
                getBedCoordinates();
                Game1.exitActiveMenu();
                Game1.warpFarmer("Farmhouse", bedX, bedY, false);
                Game1.timeOfDay = currentDate == spiritsEve ? 2400 : 2200;
                Game1.shouldTimePass();
            });
        }
    }
}
