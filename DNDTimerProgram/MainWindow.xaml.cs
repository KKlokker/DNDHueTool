using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Q42.HueApi;
using Q42.HueApi.ColorConverters.HSB;
using Q42.HueApi.Interfaces;
using Q42.HueApi.Models;
using Q42.HueApi.Streaming;
using System.Timers;
using System.ComponentModel;
using System.Drawing;
using System.Media;
using System.IO;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using System.Linq.Expressions;

namespace DNDTimerProgram
{
    public partial class MainWindow : Window
    {
        List<string> Lamp = new List<string>();
        string Ip = "";
        List<string> LightNumbers = new List<string>();
        List<string> LightsWithoutLamp = new List<string>() {};
        List<string> MonthName = new List<string>() {};
        List<string> DayName = new List<string>() {};
        string AppKey;
        bool OutsideBool = false;
        bool OutsideMusic = false;
        bool InnBool = false;
        bool LampBool = false;
        bool CaveBool = false;
        bool RainBool = false;
        bool RainChange = false;
        int SecondsInMin, MinInHour, HourInDay, DayInMonth, MonthInYear;
        int timeOfDayHour = 0;
        int timeOfDayMinut = 0;
        int DayNumber = 9;
        int DayOfWeek = 1;
        int Month = 1;
        int Year = 1;
        int RainLight;
        int TimeCounter = 0;
        string LastPlayed;

        private SoundPlayer Player1 = new SoundPlayer();
        SoundPlayer spMusic = new SoundPlayer();

        void SetFalse()
        {
            OutsideBool = false;
            InnBool = false;
            CaveBool = false;
            OutsideMusic = false;
        }

        void LoadWorldSettings()
        {
            StreamReader sr = new StreamReader("WorldSettings.txt", true);
            string line;
            int CounterI = 0;
            List<string> lineList = new List<string>();
            while ((line = sr.ReadLine()) != null)
            {
                switch (CounterI)
                {
                    case 0:
                        SecondsInMin = Int32.Parse(line);
                        break;
                    case 1:
                        MinInHour = Int32.Parse(line);
                        break;
                    case 2:
                        HourInDay = Int32.Parse(line);
                        break;
                    case 3:
                        DayInMonth = Int32.Parse(line);
                        break;
                    case 4:
                        MonthInYear = Int32.Parse(line);
                        break;
                    case 5:
                        DayName = LightStringToList(line);
                        break;
                    case 6:
                        MonthName = LightStringToList(line);
                        break;
                }
                CounterI++;

            }
        }

        void SaveWorldSettings()
        {
            using (StreamWriter writetext = new StreamWriter("WorldSettings.txt"))
            {
                if (SecInMinutTxt.Text != "")  SecondsInMin = Int32.Parse(SecInMinutTxt.Text);
                if (MinInHourTxt.Text != "") MinInHour = Int32.Parse(MinInHourTxt.Text);
                if (HourInDayTxt.Text != "") HourInDay = Int32.Parse(HourInDayTxt.Text);
                if (DayInMonthTxt.Text != "") DayInMonth = Int32.Parse(DayInMonthTxt.Text);
                if (MonthInYearTxt.Text != "") MonthInYear = Int32.Parse(MonthInYearTxt.Text);
                if (DayNamesTxt.Text != "") { DayName.Clear(); DayName = LightStringToList(DayNamesTxt.Text); }
                if (MonthNamesTxt.Text != "") { MonthName.Clear();  MonthName = LightStringToList(MonthNamesTxt.Text); }

                writetext.WriteLine(SecondsInMin);
                writetext.WriteLine(MinInHour);
                writetext.WriteLine(HourInDay);
                writetext.WriteLine(DayInMonth);
                writetext.WriteLine(MonthInYear);
                foreach (var i in DayName)
                {
                    writetext.Write(i);
                    writetext.Write(" ");
                }
                writetext.Write('\n');
                foreach (var i in MonthName)
                {
                    writetext.Write(i);
                    writetext.Write(" ");
                }
                writetext.Write('\n');
            }

        }

        void ConnectBridge()
        {
            IBridgeLocator locator = new HttpBridgeLocator();
            var bridges = locator.LocateBridgesAsync(TimeSpan.FromSeconds(5));
            bridges.Wait();
            TestText.Text = "";
            foreach (var Elight in bridges.Result)
            {
                TestText.Text += Elight.IpAddress;
                TestText.Text += "\n";
                
            }
            if (bridges.Result.Count() > 1)
            {
                Wanted_Ip.Visibility = Visibility.Visible;
                BridgeIpBox.Visibility = Visibility.Visible;
                SaveBridge.Visibility = Visibility.Visible;

            }
            else
            {
                Ip = TestText.Text;
                RegisterBridge();
            }

        }

        async void RegisterBridge()
        {
            ClickBridgeLabel.Visibility = Visibility.Visible;
            BridgeCountdown.Visibility = Visibility.Visible;
            for (int i = 0; i < 10; i++)
            {
                BridgeCountdown.Text = (10-i).ToString();
                await Task.Delay(1000);

            }
            AppKeyFunc();
            ClickBridgeLabel.Visibility = Visibility.Hidden;
        }

        void AppKeyFunc()
        {
            ILocalHueClient client = new LocalHueClient(Ip);
            try { AppKey = Task.Run(() => client.RegisterAsync("DNDHueLight", "User")).Result;
                BridgeCountdown.Visibility = Visibility.Hidden;
                BridgeCountdown.Text = "Succes";
                SaveHueSettings();
            }
            catch { BridgeCountdown.Text = "Error"; }
            
        }

        private System.Timers.Timer _timer;

        public void timerFunc()
        {
            this.Dispatcher.Invoke(() =>
            {

                double timer = 500;
                _timer = new Timer(timer);
                _timer.Elapsed += new ElapsedEventHandler(OnTimedEvent);
            });
        }

        void SetLight(double lightLevel, double xValue, double yValue, string Lamps)
        {
            ILocalHueClient client = new LocalHueClient(Ip);
            client.Initialize(AppKey);
            var command = new LightCommand();
            command.SetColor(xValue, yValue);
            command.Brightness = Convert.ToByte(Math.Round(lightLevel * 2.06)); 
            if (Lamps == "Lamp"){  client.SendCommandAsync(command, Lamp);}
            else if (LampBool && Lamps != "Force"){     client.SendCommandAsync(command, LightsWithoutLamp);}
            else{                   client.SendCommandAsync(command, LightNumbers);}
        }

        void OutsideBrightness()
        {
            double TimeOfDayDecimal = timeOfDayHour + timeOfDayMinut * (1/MinInHour);
            double LightLevel = 99 - Month * Math.Pow((TimeOfDayDecimal - MonthInYear), 2) - RainLight;
            byte ColorV;
            if (LightLevel < 10)
            {
                if (OutsideMusic)
                    Music("ForestNight.wav");

                if (LightLevel < 1)
                    LightLevel = 0;
                SetLight(LightLevel, 0.31273, 0.32902, "All");
            }
            else
            {
                ColorV = Convert.ToByte(Math.Round(LightLevel * 2.56));
                SetLight(LightLevel, 0.57 - ColorV / 1500.0, 0.30 + ColorV / 2000.0, "All");
            }
        }

        void InnScene()
        {
            double TimeOfDayDecimal = timeOfDayHour + timeOfDayMinut * (1/MinInHour);
            double LightLevel = 99 - Month * Math.Pow((TimeOfDayDecimal - MonthInYear), 2) - RainLight;
            if (LightLevel >= 20 )
            {
                LightLevel += 30;
                if (LightLevel >= 99)
                    LightLevel = 99;
                SetLight(LightLevel, 0.57 - LightLevel / 1000.0, 0.30 + LightLevel / 1000.0, "All");
            }
            else
            {
                LightLevel = 80;
                SetLight(LightLevel, 0.52409, 0.41359, "All");
            }
        }

        void CaveScene()
        {
            SetLight(0, 0.31273, 0.32902, "All");
        }

        void DayOfWeekCount()
        {
            YearText.Text = Year.ToString();
            DayOfWeek++;
            if (DayOfWeek % DayName.Count+1 == 0)
                DayOfWeek -= DayName.Count + 1;
            UgeDagTekst.Text = DayName[DayOfWeek];
        }

        void Music(string Location)
        {
            if (SoundC.IsChecked == true)
            {
                if (LastPlayed != Location || RainChange)
                {
                    RainChange = false;
                    spMusic.Stop();
                    string SoundLocation;
                    if (RainBool && Month < 9)
                    {
                        SoundLocation = System.AppDomain.CurrentDomain.BaseDirectory + "Music\\" + "Rain" + Location;
                    }
                    else
                    {
                        SoundLocation = System.AppDomain.CurrentDomain.BaseDirectory + "Music\\" + Location;
                    }
                    spMusic.SoundLocation = SoundLocation;
                    spMusic.PlayLooping();
                    LastPlayed = Location;
                }
            }
        }
        
        void LampScene()
        {
            int LightLevel = 80;
            SetLight(LightLevel, 0.52409, 0.41359, "Lampe");
        }

        void RainingFunc()
        {
            if (!RainBool)
            {
                double RainChanche = -0.1 * Math.Pow((Month - 5), 4.0) + 4.4 * Math.Pow(Month, 2.0) - 42.1 * Month + 100;
                Random rnd = new Random();
                double Chance = rnd.Next(1, 101);
                if (RainC.IsChecked == true) { Chance = 0;}
                if (RainChanche > Chance && NoRainC.IsChecked == false)
                {
                    RainBool = true;
                    RainLight = 20;
                    RainChange = true;
                }
            }
            else {
                Random rnd = new Random();
                double Chance = rnd.Next(1, 5);
                if (2 == Chance)
                {
                    RainBool = false;
                    RainLight = 0;
                }
                RainChange = true;
            }
        }

        private void OnTimedEvent(object source, ElapsedEventArgs e)
        {
            this.Dispatcher.Invoke(() =>
            {
                TimeCounter++;
                double TimeModder;
                if (TimeMod.Text == "")
                    TimeModder = 1;
                else 
                    TimeModder = Convert.ToDouble(TimeMod.Text);
                if (TimeCounter % TimeModder == 0)
                {
                TimeCounter = 0;
                Minut.Text = timeOfDayMinut.ToString();
                timeOfDayMinut++;
                DagText.Text = DayNumber.ToString();

                    if (timeOfDayMinut % 1 == 0)
                    {
                        if (LysC.IsChecked != true && MørkeC.IsChecked != true && RoedC.IsChecked != true)
                        {
                            if (InnBool)
                                InnScene();
                            if (OutsideBool)
                                OutsideBrightness();
                            if (LampBool)
                                LampScene();
                            if (CaveBool)
                                CaveScene();
                        }
                        else if (LysC.IsChecked == true){   SetLight(100, 0.35, 0.35, "Force");}
                        else if (MørkeC.IsChecked == true){ SetLight(0, 0.35, 0.35, "Force");}
                        else if (RoedC.IsChecked == true){  SetLight(100, 0.8, 0.25, "Force");}
                        Hour.Text = timeOfDayHour.ToString();
                    }
                if (timeOfDayMinut >= MinInHour)
                {
                    timeOfDayHour++;
                    timeOfDayMinut -= MinInHour;
                }
                if (timeOfDayHour >= HourInDay)
                {
                    DayNumber++;
                    DayOfWeekCount();
                    timeOfDayHour -= HourInDay;
                    RainingFunc();
                }
                if (DayNumber >= DayInMonth)
                {
                    Month++;
                    DayNumber -= DayInMonth-1;
                }
                if (Month >= MonthInYear)
                {
                    Year++;
                    Month  -= MonthInYear;
                }
                ManedText.Text = MonthName[Month];
            }
            }
            );
        }

        public MainWindow()
        {
            InitializeComponent();
            timerFunc();
            LoadWorldSettings();
            LoadHueSettings();
            var enabled = _timer.Enabled;
            _timer.Enabled = true;
            if (!LightsWithoutLamp.Any() || !LightNumbers.Any() || !Lamp.Any())
            {
                LoadHueSettings();
                if (LightsWithoutLamp.Any())
                {
                    foreach (var i in LightsWithoutLamp)
                        WantedLights.Text += i + " ";
                }
                else
                    WantedLights.Text = "Ex. 1 4 6";
                if (LightNumbers.Any())
                {
                    foreach (var i in Lamp)
                        FlashlightText.Text += i + " ";
                }
                else
                    FlashlightText.Text = "Ex. 2 5";
            }
        }
        
        private void Button_Cave(object sender, RoutedEventArgs e)
        {
            SetFalse();
            CaveBool = true;
            Music("Cave.wav");
        }

        private void AddTimeButton_Click(object sender, RoutedEventArgs e)
        {
            timeOfDayMinut += Int32.Parse(AddTimeAmountMinut.Text);
            timeOfDayHour += Int32.Parse(AddTimeAmountHour.Text);
        }
        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            var enabled = _timer.Enabled;
            if (enabled)
            {
                PlayButton.Content = "Play";
                _timer.Enabled = false;
            }
            else
            {
                PlayButton.Content = "Pause";
                _timer.Enabled = true;
            }
        }
        private void SaveTimeButton_Click(object sender, RoutedEventArgs e)
        {
            using (StreamWriter writetext = new StreamWriter("TimeFile.txt"))
            {
                writetext.WriteLine(timeOfDayHour.ToString());
                writetext.WriteLine(timeOfDayMinut.ToString());
                writetext.WriteLine(DayNumber.ToString());
                writetext.WriteLine(Month.ToString());
                writetext.WriteLine(Year.ToString());
            }
        }
        private void LoadTimeButton_Click(object sender, RoutedEventArgs e)
        {
            StreamReader sr = new StreamReader("TimeFile.txt",true);
            string line;
            List<string> lineList = new List<string>();
            while ((line = sr.ReadLine()) != null)
            {
                lineList.Add(line);
            }
            timeOfDayHour = Int32.Parse(lineList[0]);
            timeOfDayMinut = Int32.Parse(lineList[1]);
            DayNumber = Int32.Parse(lineList[2]);
            Month = Int32.Parse(lineList[3]);
            Year = Int32.Parse(lineList[4]);
            DayOfWeekCount();
        }

        private void HouseButt_Click(object sender, RoutedEventArgs e)
        {
            SetFalse();
            Music("House.wav");
            InnBool = true;
        }

        private void OceanButt_Click(object sender, RoutedEventArgs e)
        {
            SetFalse();
            Music("Ocean.wav");
            OutsideBool = true;
        }

        private void DungeonButt_Click(object sender, RoutedEventArgs e)
        {
            SetFalse();
            CaveBool = true;
            Music("Cave.wav");

        }

        private void TownButt_Click(object sender, RoutedEventArgs e)
        {
            SetFalse();
            Music("Town.wav");
            OutsideBool = true;
        }

        private void ExtremeCreepyButt_Click(object sender, RoutedEventArgs e)
        {
            SetFalse();
            CaveBool = true;
            Music("ExtremeCreepy.wav");
        }

        private void Inn_crowded_Click(object sender, RoutedEventArgs e)
        {
            SetFalse();
            Music("Tavern.wav");
            InnBool = true;
        }

        private void CreepyButt_Click_1(object sender, RoutedEventArgs e)
        {
            SetFalse();
            CaveBool = true;
            Music("Creepy.wav");
        }

        private void NatureButt_Click(object sender, RoutedEventArgs e)
        {
            SetFalse();
            OutsideMusic = true;
            OutsideBool = true;
        }
        private void InnButtonCrowd_Click(object sender, RoutedEventArgs e)
        {
            SetFalse();
            Music("Tavern.wav");
            InnBool = true;
        }

        private void InnButtonNotCrowd_Click(object sender, RoutedEventArgs e)
        {
            SetFalse();
            Music("TavernNotCrowded.wav");
            InnBool = true;
        }

        private void SoundC_Click(object sender, RoutedEventArgs e)
        {
            if (SoundC.IsChecked == false)
            {
                spMusic.Stop();
            }
            else
            {
                RainChange = true;
                spMusic.PlayLooping();
            }
        }

        private void RainC_Checked(object sender, RoutedEventArgs e)
        {
            RainChange = true;
            RainingFunc();
        }

        private void NoRainC_Checked(object sender, RoutedEventArgs e)
        {
            RainChange = true;
            RainBool = false;
        }

        private void LampeC_Checked(object sender, RoutedEventArgs e)
        {
            LampBool = LampeC.IsChecked ?? false;
        }

        private void LoadLight_Click(object sender, RoutedEventArgs e)
        {
            TestText.Text = "";
            ILocalHueClient client = new LocalHueClient(Ip);
            client.Initialize(AppKey);
            var command = new LightCommand();
            var lights = client.GetLightsAsync();
            lights.Wait();
            foreach (var Elight in lights.Result)
            {
                TestText.Text += Elight.Id;
                TestText.Text += " ";
                TestText.Text += Elight.Name;
                TestText.Text += "\n";
            }
        }

        private void WantedLights_GotFocus(object sender, RoutedEventArgs e)
        {
            if (WantedLights.Text == "Ex. 1 4 6")
            {
                WantedLights.Text = "";
            }

        }

        private void WantedLights_LostFocus(object sender, RoutedEventArgs e)
        {
            if (WantedLights.Text == "")
            {
                WantedLights.Text = "Ex. 1 4 6";
            }
        }

        static List<string> LightStringToList(string Text)
        {
            List<string> TheList = new List<string>();
            string AddToList = "";
            for (int i = 0; i != Text.Length; i++)
            {
                if (Text[i] == ' ' || Text[i] == '\n')
                {
                    TheList.Add(AddToList);
                    AddToList = "";
                }
                else if (Text.Length == i + 1)
                {
                    AddToList += Text[i];
                    TheList.Add(AddToList);
                    AddToList = "";
                }
                else
                    AddToList += Text[i];
            }
            return TheList;
        }

        private void CloseWorldBuilding_Click(object sender, RoutedEventArgs e)
        {
            WorldSettingsGrid.Visibility = Visibility.Hidden;
        }

        private void CloseLightBtn_Click(object sender, RoutedEventArgs e)
        {

            LightSettings.Visibility = Visibility.Hidden;
        }

        private void OpenWorldBtn_Click(object sender, RoutedEventArgs e)
        {

            SecInMinutTxt.Text = SecondsInMin.ToString();
            MinInHourTxt.Text = MinInHour.ToString();
            HourInDayTxt.Text = HourInDay.ToString();
            DayInMonthTxt.Text = DayInMonth.ToString();
            MonthInYearTxt.Text = MonthInYear.ToString();
            DayNamesTxt.Text = "";
            foreach (var item in DayName)
            {
                DayNamesTxt.Text += item;
                DayNamesTxt.Text += " ";
            }
            MonthNamesTxt.Text = "";
            foreach (var item in MonthName)
            {
                MonthNamesTxt.Text += item;
                MonthNamesTxt.Text += " ";
            }

            WorldSettingsGrid.Visibility = Visibility.Visible;
        }

        private void OpenLightBtn_Click(object sender, RoutedEventArgs e)
        {
            LoadHueSettings();
            LightSettings.Visibility = Visibility.Visible;

        }

        private void SaveWorldSettings2_Click(object sender, RoutedEventArgs e)
        {
            SaveWorldSettings();
        }

        private void SaveBridge_Click(object sender, RoutedEventArgs e)
        {
            Ip = BridgeIpBox.Text;
            RegisterBridge();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            LightsWithoutLamp.Clear();
            Lamp.Clear();
            Lamp = LightStringToList(FlashlightText.Text);
            string AddToList = "";
            for (int i = 0; i != WantedLights.Text.Length; i++)
            {
                if (WantedLights.Text[i] == ' ' || WantedLights.Text[i] == '\n')
                {
                    if (!FlashlightText.Text.Contains(AddToList))
                        LightsWithoutLamp.Add(AddToList);
                    AddToList = "";
                }
                else if(WantedLights.Text.Length == i + 1)
                {
                    AddToList += WantedLights.Text[i];
                    if (!FlashlightText.Text.Contains(AddToList))
                        LightsWithoutLamp.Add(AddToList);
                    AddToList = "";

                }
                else
                    AddToList += WantedLights.Text[i];
            }
            LightNumbers.Clear();
            foreach (var i in Lamp)
            {
                LightNumbers.Add(i);
            }
            foreach (var i in LightsWithoutLamp)
            {
                LightNumbers.Add(i);
            }
            SaveHueSettings();
        }

        void SaveHueSettings()
        {
            using (StreamWriter writetext = new StreamWriter("HueSettings.txt"))
            {
                foreach (var i in LightsWithoutLamp)
                {
                    writetext.Write(i);
                    writetext.Write(" ");
                }
                writetext.Write('\n');
                foreach (var i in Lamp)
                {
                    writetext.Write(i);
                    writetext.Write(" ");
                }
                writetext.Write('\n');
                foreach (var i in LightNumbers)
                {
                    writetext.Write(i);
                    writetext.Write(" ");
                }
                writetext.Write('\n');
                writetext.WriteLine(Ip);
                writetext.WriteLine(AppKey);
            }
        }

        void LoadHueSettings()
        {
            StreamReader sr = new StreamReader("HueSettings.txt", true);
            string line;
            int CounterI = 0;
            List<string> lineList = new List<string>();
            while ((line = sr.ReadLine()) != null)
            {
                switch (CounterI)
                {
                    case 0:
                        LightsWithoutLamp = LightStringToList(line);
                        break;
                    case 1:
                        Lamp = LightStringToList(line);
                        break;
                    case 2:
                        LightNumbers = LightStringToList(line);
                        break;
                    case 3:
                        Ip = line;
                        break;
                    case 4:
                        AppKey = line;
                        break;
                }
                CounterI++;
            }
            WantedLights.Text = "";
            foreach (var item in LightsWithoutLamp)
            {
                WantedLights.Text += item;
                WantedLights.Text += " ";
            }
            FlashlightText.Text = "";
            foreach (var item in Lamp)
            {
                FlashlightText.Text += item;
                FlashlightText.Text += " ";
            }
            BridgeIpBox.Text = Ip;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            ConnectBridge();
        }
    }
}
