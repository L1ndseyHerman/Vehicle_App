using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Data.SQLite;
using System.Text.RegularExpressions;
using System.Globalization;

namespace VehicleApp
{
    public partial class WebForm1 : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {

            var con = ConnectToAndOpenDatabase();

            if (!Page.IsPostBack)
            {
                var cmd = new SQLiteCommand(con)
                {
                    CommandText = @"CREATE TABLE IF NOT EXISTS Vehicles(UniqueID INTEGER PRIMARY KEY, Year INT, Make TEXT, Model TEXT, ImpoundDate DATETIME NOT NULL, AuctionDate DATETIME NOT NULL, DateTimeCreated DATETIME NOT NULL, IsDeleted BOOLEAN NOT NULL)"
                };
                cmd.ExecuteNonQuery();
            }

            BindGridViewAndCloseDatabase(con);
        }

        protected SQLiteConnection ConnectToAndOpenDatabase()
        {
            //  Sorry, I was strugging to get a relative path. Set this to wherever you want it on your computer.
            //  I changed the path name for GitHub because the real path had the name of the 
            //  company as a folder name.
            string cs = @"URI=file:C:\FakePath\vehicles.db";

            var con = new SQLiteConnection(cs);
            con.Open();

            return con;
        }
        protected void BindGridViewAndCloseDatabase(SQLiteConnection con)
        {
            string sql = "SELECT UniqueID, Year, Make, Model, datetime(ImpoundDate,'localtime') as ImpoundDate, datetime(AuctionDate,'localtime') as AuctionDate, DateTimeCreated, IsDeleted from Vehicles";
            SQLiteCommand newCmd = new SQLiteCommand(sql, con);
            SQLiteDataReader reader = newCmd.ExecuteReader();
            GridView1.DataSource = reader;
            GridView1.DataBind();

            con.Close();
        }

        protected void AddButton_Click(object sender, EventArgs e)
        {
            bool isValidInput = true;

            string yearToSendToDatabase = YearTextBox.Text;

            if (yearToSendToDatabase == "")
            {
                yearToSendToDatabase = "-1";
                YearErrorMessageLabel.Text = "";
            }
            else
            {
                // Regex that makes sure the year contains four numbers of reasonable years.
                string pattern = @"(^19[0-9][0-9]$)|(^20[0-1][0-9]$)|(^202[0-1]$)";
                Regex rgx = new Regex(pattern);
                if (rgx.IsMatch(YearTextBox.Text))
                {
                    YearErrorMessageLabel.Text = "";
                }
                else
                {
                    isValidInput = false;
                    YearErrorMessageLabel.Text = "Please enter a four-digit year between 1900 and 2021.";
                }
            }

            //  In MySQL/MSSQL, there is a way to set the length of a VarChar, but I don't think there is in SQLite? 
            //  So I'm checking lengths here. I don't think there are any car makes or models longer than 30 char?
            //  I am also allowing non-letters because of "Ford F150" and other such makes/models 
            //  that may exist in the future.
            int numMakeChars = MakeTextBox.Text.ToString().Length;
            if (numMakeChars > 30)
            {
                isValidInput = false;
                MakeErrorMessageLabel.Text = "Make should be 30 characters or less.";
            }
            else
            {
                MakeErrorMessageLabel.Text = "";
            }

            int numModelChars = ModelTextBox.Text.ToString().Length;
            if (numModelChars > 30)
            {
                isValidInput = false;
                ModelErrorMessageLabel.Text = "Model should be 30 characters or less.";
            }
            else
            {
                ModelErrorMessageLabel.Text = "";
            }

            if (isValidInput)
            {
                string localDateString = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                var con = ConnectToAndOpenDatabase();
                try
                {
                    //  Midnight in EST is 5AM in UTC.
                    //  I'm not sure when you want the time to be, I'm assuming you don't want the user
                    //  to type in the time, just the date? There needs to be some time to convert to UTC though....
                    string dateWithTimeNow = ImpoundTextBox.Text + " 00:00:00";
                    DateTime impoundDate = DateTime.ParseExact(dateWithTimeNow, "yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture);

                    ImpoundErrorMessageLabel.Text = impoundDate.ToString();
                    DateTime utcImpoundDate = impoundDate.ToUniversalTime();
                    string utcDateString = utcImpoundDate.ToString("yyyy-MM-dd HH:mm:ss");

                    //  The try/catch will catch invalid dates, like a day of "32". 
                    //  However, years like "1000" are valid even though that's long before cars were invented,
                    //  so this Regex is just to prevent years like that.
                    string utcPattern = @"((^19[0-9][0-9])|(^20[0-1][0-9])|(^202[0-1]))-[0-9][0-9]-[0-9][0-9] [0-9][0-9]:[0-9][0-9]:[0-9][0-9]$";
                    Regex utcRegex = new Regex(utcPattern);
                    if (utcRegex.IsMatch(utcDateString))
                    {
                        ImpoundErrorMessageLabel.Text = "";

                        //  5 business days per week means 6 weeks to get to 30 + 12 non-business days = 42.
                        int daysToAddToImpoundDate = 42;
                        int extraHolidayOrWeekendDays = 0;

                        /*
                        That takes care of weekends, now here are the holidays:
                        I did a quick google search for "State of Florida holidays" and it came back with these.
                        I added Juneteenth since that wasn't on there, probably because it is a recently-added
                        federal holiday. 

                        I wasn't sure if I should move holidays to another day if they are on the weekend,
                        for example, if the 4th of July is a Saturday, move the non-business day to the 3rd.

                        I'm also not sure if impound lots consider all of these holidays to be non-business days.
                        I know many retail companies do not bother to close for minor holidays, such as President's Day,
                        and may even have sales on those days.

                        Hopefully you get the general idea that I know how to code holidays, so that if I'm 
                        missing one (Should Easter or Columbus Day be on here?), or have extra, you can tell
                        it would just take a quick adjustment. 
                        
                        Also, if you're wondering why I'm starting with Vetran's Day instead of New Year's Day,
                        it's because the ordering of some holidays effects others. For example, if Christmas is 
                        exactly 43 days after the impound date, it seems like it doesn't need to be checked, 
                        but Thanksgiving is also in that range, so you have to add 1-3 days because of 
                        Thanksgiving, and now Christmas IS in the range! I made sure the last holiday, Labor Day,
                        doesn't affect any other holidays, so it's safe to be at the end. 
                        */

                        //  Vetran's Day:
                        if ((utcImpoundDate.Month == 9 && utcImpoundDate.Day >= 30 - extraHolidayOrWeekendDays) || (utcImpoundDate.Month == 10) || (utcImpoundDate.Month == 11 && utcImpoundDate.Day <= 11))
                        {
                            string veteransDayString = utcImpoundDate.Year + "-11-11" + " 01:01:01";
                            DateTime veteransDay = DateTime.ParseExact(veteransDayString, "yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture);
                            //  If the holiday falls on a weekend, it's already not a business day, so don't re-count it.
                            if ((veteransDay.DayOfWeek != DayOfWeek.Saturday) && (veteransDay.DayOfWeek != DayOfWeek.Sunday))
                            {
                                extraHolidayOrWeekendDays++;
                                extraHolidayOrWeekendDays = AddWeekendDays(utcImpoundDate, daysToAddToImpoundDate, extraHolidayOrWeekendDays);
                            }
                        }
                        
                        //  Thanksgiving Day:
                        //  4th Thursday of the month = btw 22-28
                        int thanksgivingDayDay = -1;
                        for (int index = 22; index <= 28; index++)
                        {
                            string dateStringToCheck = utcImpoundDate.Year + "-11-" + index + " 01:01:01";
                            DateTime dateToCheck = DateTime.ParseExact(dateStringToCheck, "yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture);
                            if (dateToCheck.DayOfWeek == DayOfWeek.Thursday)
                            {
                                thanksgivingDayDay = dateToCheck.Day;
                                break;
                            }
                        }

                        int daysPastTheEleventh = thanksgivingDayDay - 22;

                        if ((utcImpoundDate.Month == 10 && utcImpoundDate.Day >= 11 - extraHolidayOrWeekendDays + daysPastTheEleventh) || (utcImpoundDate.Month == 11 && utcImpoundDate.Day <= thanksgivingDayDay))
                        {
                            extraHolidayOrWeekendDays++;
                            extraHolidayOrWeekendDays = AddWeekendDays(utcImpoundDate, daysToAddToImpoundDate, extraHolidayOrWeekendDays);
                        }
                       
                        //  Christmas Day:
                        if ((utcImpoundDate.Month == 11 && utcImpoundDate.Day >= 13 - extraHolidayOrWeekendDays) || (utcImpoundDate.Month == 12 && utcImpoundDate.Day <= 25))
                        {
                            string christmasDayString = utcImpoundDate.Year + "-12-25" + " 01:01:01";
                            DateTime christmasDay = DateTime.ParseExact(christmasDayString, "yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture);

                            if ((christmasDay.DayOfWeek != DayOfWeek.Saturday) && (christmasDay.DayOfWeek != DayOfWeek.Sunday))
                            {
                                extraHolidayOrWeekendDays++;
                                extraHolidayOrWeekendDays = AddWeekendDays(utcImpoundDate, daysToAddToImpoundDate, extraHolidayOrWeekendDays);
                            }
                        }

                        //  New Year's Day:
                        if ((utcImpoundDate.Month == 11 && utcImpoundDate.Day >= 20 - extraHolidayOrWeekendDays) || (utcImpoundDate.Month == 12) || (utcImpoundDate.Month == 1 && utcImpoundDate.Day == 1))
                        {
                            //  This is the tricky one. May need to check next year if date is in December or late November:
                            string newYearsDayString = "";
                            if (utcImpoundDate.Month == 1)
                            {
                                newYearsDayString = utcImpoundDate.Year + "-01-01" + " 01:01:01";
                            }
                            else
                            {
                                newYearsDayString = utcImpoundDate.Year+1 + "-01-01" + " 01:01:01";
                            }
                            DateTime newYearsDay = DateTime.ParseExact(newYearsDayString, "yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture);
                            
                            if ((newYearsDay.DayOfWeek != DayOfWeek.Saturday) && (newYearsDay.DayOfWeek != DayOfWeek.Sunday))
                            {
                                extraHolidayOrWeekendDays++;
                                extraHolidayOrWeekendDays = AddWeekendDays(utcImpoundDate, daysToAddToImpoundDate, extraHolidayOrWeekendDays);
                            }
                        }

                        //  MLK Day:
                        //  3rd Monday of the month = btw 15-21
                        int mlkDayDay = -1;
                        for (int index = 15; index <= 21; index++)
                        {
                            string dateStringToCheck = utcImpoundDate.Year + "-01-" + index + " 01:01:01";
                            DateTime dateToCheck = DateTime.ParseExact(dateStringToCheck, "yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture);
                            if (dateToCheck.DayOfWeek == DayOfWeek.Monday)
                            {
                                mlkDayDay = dateToCheck.Day;
                                break;
                            }
                        }

                        int daysPastTheTenth = mlkDayDay - 15;

                        if ((utcImpoundDate.Month == 12 && utcImpoundDate.Day >= 4 - extraHolidayOrWeekendDays + daysPastTheTenth) || (utcImpoundDate.Month == 01 && utcImpoundDate.Day <= mlkDayDay))
                        {
                            extraHolidayOrWeekendDays++;
                            extraHolidayOrWeekendDays = AddWeekendDays(utcImpoundDate, daysToAddToImpoundDate, extraHolidayOrWeekendDays);
                        }

                        //  Susan B. Anthony Day/Washington Day/President's Day:
                        if ((utcImpoundDate.Month == 01 && utcImpoundDate.Day >= 4 - extraHolidayOrWeekendDays) || (utcImpoundDate.Month == 02 && utcImpoundDate.Day <= 15))
                        {
                            string presidentsDayString = utcImpoundDate.Year + "-02-15" + " 01:01:01";
                            DateTime presidentsDay = DateTime.ParseExact(presidentsDayString, "yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture);
                            
                            if ((presidentsDay.DayOfWeek != DayOfWeek.Saturday) && (presidentsDay.DayOfWeek != DayOfWeek.Sunday))
                            {
                                extraHolidayOrWeekendDays++;
                                extraHolidayOrWeekendDays = AddWeekendDays(utcImpoundDate, daysToAddToImpoundDate, extraHolidayOrWeekendDays);
                            }
                        }

                        //  Pascua Florida, a Florida state holiday:
                        //  Might need to look back to the 20th or the 21st of February depending on if it's a leap year or not.
                        int dayToLookBackTo = 20;
                        bool isLeapYear = false;
                        if ((utcImpoundDate.Year % 4 == 0 && utcImpoundDate.Year % 100 == 0 && utcImpoundDate.Year % 400 == 0) || (utcImpoundDate.Year % 4 == 0 && utcImpoundDate.Year % 100 != 0))
                        {
                            isLeapYear = true;
                        }

                        if (isLeapYear)
                        {
                            dayToLookBackTo++;
                        }

                        if ((utcImpoundDate.Month == 02 && utcImpoundDate.Day >= dayToLookBackTo - extraHolidayOrWeekendDays) || (utcImpoundDate.Month == 3) || (utcImpoundDate.Month == 4 && utcImpoundDate.Day == 2))
                        {
                            string pascuaFloridaDayString = utcImpoundDate.Year + "-04-02" + " 01:01:01";
                            DateTime pascuaFloridaDay = DateTime.ParseExact(pascuaFloridaDayString, "yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture);
                            
                            if ((pascuaFloridaDay.DayOfWeek != DayOfWeek.Saturday) && (pascuaFloridaDay.DayOfWeek != DayOfWeek.Sunday))
                            {
                                extraHolidayOrWeekendDays++;
                                extraHolidayOrWeekendDays = AddWeekendDays(utcImpoundDate, daysToAddToImpoundDate, extraHolidayOrWeekendDays);
                            }
                        }

                        //  Good Friday:
                        //  I copied-n-pasted this code from https://stackoverflow.com/questions/2192533/function-to-return-date-of-easter-for-the-given-year .
                        //  It's the VB .NET one that I converted to C#.NET.
                        //  I'm not sure what these variables stand for, so Idk what else to name them besides these letters,
                        //  sorry.
                        int easterMonth = 3;
                        int a = utcImpoundDate.Year % 19 + 1;
                        int b = utcImpoundDate.Year / 100 + 1;
                        int c = (3 * b) / 4 - 12;
                        int d = (8 * b + 5) / 25 - 5;
                        //  "e" conflicted with the pre-coded one
                        int e2 = (5 * utcImpoundDate.Year) / 4 - c - 10;
                        int f = (11 * a + 20 + d - c) % 30;
                        if (f == 24)
                        {
                            f += 1;
                        }
                        if ((f == 25) && (a > 11))
                        {
                            f += 1;
                        }
                        int g = 44 - f;
                        if (g < 21)
                        {
                            g += 30;
                        }
                        int easterDay = (g + 7) - ((e2 + g) % 7);
                        if (easterDay > 31)
                        {
                            easterDay -= 31;
                            easterMonth = 4;
                        }
                        int goodFridayDay = easterDay - 2;
                        int goodFridayMonth = easterMonth;
                        if (goodFridayDay < 1)
                        {
                            goodFridayDay += 31;
                            goodFridayMonth = 3;
                        }

                        int daysLeft = 42;
                        bool coversThreeMonths = false;

                        if (goodFridayMonth == 4)
                        {
                            daysLeft -= 30 - goodFridayDay;
                            if (daysLeft > 31)
                            {
                                coversThreeMonths = true;
                                daysLeft -= 31;
                            }

                            if (coversThreeMonths)
                            {
                                isLeapYear = false;
                                if ((utcImpoundDate.Year % 4 == 0 && utcImpoundDate.Year % 100 == 0 && utcImpoundDate.Year % 400 == 0) || (utcImpoundDate.Year % 4 == 0 && utcImpoundDate.Year % 100 != 0))
                                {
                                    isLeapYear = true;
                                }

                                int lastDay = 28;

                                if (isLeapYear)
                                {
                                    lastDay = 29;
                                }

                                if ((utcImpoundDate.Month == 02 && utcImpoundDate.Day >= lastDay - extraHolidayOrWeekendDays - daysLeft) || (utcImpoundDate.Month == 03) || (utcImpoundDate.Month == 04 && utcImpoundDate.Day <= goodFridayDay))
                                {
                                    extraHolidayOrWeekendDays++;
                                    extraHolidayOrWeekendDays = AddWeekendDays(utcImpoundDate, daysToAddToImpoundDate, extraHolidayOrWeekendDays);
                                }
                            }
                            else
                            {
                                if ((utcImpoundDate.Month == 03 && utcImpoundDate.Day >= 31 - extraHolidayOrWeekendDays - daysLeft) || (utcImpoundDate.Month == 04 && utcImpoundDate.Day <= goodFridayDay))
                                {
                                    extraHolidayOrWeekendDays++;
                                    extraHolidayOrWeekendDays = AddWeekendDays(utcImpoundDate, daysToAddToImpoundDate, extraHolidayOrWeekendDays);
                                }
                            }
                        }
                        else if (goodFridayMonth == 3)
                        {
                            isLeapYear = false;
                            if ((utcImpoundDate.Year % 4 == 0 && utcImpoundDate.Year % 100 == 0 && utcImpoundDate.Year % 400 == 0) || (utcImpoundDate.Year % 4 == 0 && utcImpoundDate.Year % 100 != 0))
                            {
                                isLeapYear = true;
                            }

                            daysLeft -= 31 - goodFridayDay;
                            int lastDay = 28;

                            if (isLeapYear)
                            {
                                lastDay = 29;
                                if (daysLeft > 29)
                                {
                                    coversThreeMonths = true;
                                    daysLeft -= 29;
                                }
                                else
                                {
                                    coversThreeMonths = false;
                                }
                            }
                            else
                            {
                                if (daysLeft > 28)
                                {
                                    coversThreeMonths = true;
                                    daysLeft -= 28;
                                }
                                else
                                {
                                    coversThreeMonths = false;
                                }
                            }

                            if (coversThreeMonths)
                            {
                                if ((utcImpoundDate.Month == 01 && utcImpoundDate.Day >= extraHolidayOrWeekendDays - daysLeft) || (utcImpoundDate.Month == 02) || (utcImpoundDate.Month == 03 && utcImpoundDate.Day <= goodFridayDay))
                                {
                                    extraHolidayOrWeekendDays++;
                                    extraHolidayOrWeekendDays = AddWeekendDays(utcImpoundDate, daysToAddToImpoundDate, extraHolidayOrWeekendDays);
                                }
                            }
                            else
                            {
                                if ((utcImpoundDate.Month == 02 && utcImpoundDate.Day >= lastDay - extraHolidayOrWeekendDays - daysLeft) || (utcImpoundDate.Month == 03 && utcImpoundDate.Day <= goodFridayDay))
                                {
                                    extraHolidayOrWeekendDays++;
                                    extraHolidayOrWeekendDays = AddWeekendDays(utcImpoundDate, daysToAddToImpoundDate, extraHolidayOrWeekendDays);
                                }
                            }
                        }
                        
                        //  Memorial Day:
                        //  Check the last week in May to see which day is a Monday: 
                        int memorialDayDay = -1;
                        for (int index = 25; index <= 31; index++)
                        {
                            string dateStringToCheck = utcImpoundDate.Year + "-05-" + index + " 01:01:01";
                            DateTime dateToCheck = DateTime.ParseExact(dateStringToCheck, "yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture);
                            if (dateToCheck.DayOfWeek == DayOfWeek.Monday)
                            {
                                memorialDayDay = dateToCheck.Day;
                                break;
                            }
                        }

                        int daysPastTheThirteenth= memorialDayDay - 25;

                        if ((utcImpoundDate.Month == 4 && utcImpoundDate.Day >= 13 - extraHolidayOrWeekendDays + daysPastTheThirteenth) || (utcImpoundDate.Month == 5 && utcImpoundDate.Day <= memorialDayDay))
                        {
                            extraHolidayOrWeekendDays++;
                            extraHolidayOrWeekendDays = AddWeekendDays(utcImpoundDate, daysToAddToImpoundDate, extraHolidayOrWeekendDays);
                        }

                        //  New holiday Juneteenth:
                        if ((utcImpoundDate.Month == 5 && utcImpoundDate.Day >= 8 - extraHolidayOrWeekendDays) || (utcImpoundDate.Month == 6 && utcImpoundDate.Day <= 19))
                        {
                            string juneteenthString = utcImpoundDate.Year + "-06-19" + " 01:01:01";
                            DateTime juneteenth = DateTime.ParseExact(juneteenthString, "yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture);
                            
                            if ((juneteenth.DayOfWeek != DayOfWeek.Saturday) && (juneteenth.DayOfWeek != DayOfWeek.Sunday))
                            {
                                extraHolidayOrWeekendDays++;
                                extraHolidayOrWeekendDays = AddWeekendDays(utcImpoundDate, daysToAddToImpoundDate, extraHolidayOrWeekendDays);
                            }
                        }


                        //  Independence Day:
                        if ((utcImpoundDate.Month == 5 && utcImpoundDate.Day >= 23 - extraHolidayOrWeekendDays) || (utcImpoundDate.Month == 6) || (utcImpoundDate.Month == 7 && utcImpoundDate.Day <= 4))
                        {
                            string independenceDayString = utcImpoundDate.Year + "-07-04" + " 01:01:01";
                            DateTime independenceDay = DateTime.ParseExact(independenceDayString, "yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture);
                            
                            if ((independenceDay.DayOfWeek != DayOfWeek.Saturday) && (independenceDay.DayOfWeek != DayOfWeek.Sunday))
                            {
                                extraHolidayOrWeekendDays++;
                                extraHolidayOrWeekendDays = AddWeekendDays(utcImpoundDate, daysToAddToImpoundDate, extraHolidayOrWeekendDays);
                            }
                        }

                        //  Labor Day:
                        //  First Monday in September
                        int laborDayDay = -1;
                        for (int index=1; index<=7; index++)
                        {
                            string dateStringToCheck = utcImpoundDate.Year + "-09-0" + index + " 01:01:01";
                            DateTime dateToCheck = DateTime.ParseExact(dateStringToCheck, "yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture);
                            if ((int)dateToCheck.DayOfWeek == 1)
                            {
                                laborDayDay = dateToCheck.Day;
                                break;
                            }
                        }

                        if ((utcImpoundDate.Month == 7 && laborDayDay < 7 && utcImpoundDate.Day >= 19 -  + laborDayDay) || (utcImpoundDate.Month == 7 && laborDayDay == 7 && utcImpoundDate.Day >= 31 - extraHolidayOrWeekendDays + 1) || (utcImpoundDate.Month == 8) || (utcImpoundDate.Month == 9 && utcImpoundDate.Day <= laborDayDay))
                        {
                            extraHolidayOrWeekendDays++;
                            extraHolidayOrWeekendDays = AddWeekendDays(utcImpoundDate, daysToAddToImpoundDate, extraHolidayOrWeekendDays);
                        }

                        daysToAddToImpoundDate += extraHolidayOrWeekendDays;

                        DateTime auctionDate = utcImpoundDate.AddDays(daysToAddToImpoundDate);
                        string auctionDateString = auctionDate.ToString("yyyy-MM-dd HH:mm:ss");

                        var cmd = new SQLiteCommand(con)
                        {
                            CommandText = "INSERT INTO Vehicles(Year, Make, Model, ImpoundDate, AuctionDate, DateTimeCreated, IsDeleted) VALUES(" + yearToSendToDatabase + ", '" + MakeTextBox.Text + "', '" + ModelTextBox.Text + "', '" + utcDateString + "', '" + auctionDateString + "', '" + localDateString + "', 0)"
                        };
                        cmd.ExecuteNonQuery();
                    }
                    else
                    {
                        ImpoundErrorMessageLabel.Text = "Please enter a four-digit year between 1900 and 2021.";
                    }
                }
                catch
                {
                    ImpoundErrorMessageLabel.Text = "The impound date is in the wrong format.";
                }

                BindGridViewAndCloseDatabase(con);
            }
        }

        protected int AddWeekendDays(DateTime utcImpoundDate, int daysToAddToImpoundDate, int extraHolidayOrWeekendDays)
        {
            //  The day this code adds to the end of the 42 days to make up for the holiday
            //  could be a weekend. If it is, add 1-2 more days depending on if it's a Saturday or Sunday.
            if (utcImpoundDate.AddDays(daysToAddToImpoundDate + extraHolidayOrWeekendDays).DayOfWeek == DayOfWeek.Saturday)
            {
                extraHolidayOrWeekendDays += 2;
            }
            else if (utcImpoundDate.AddDays(daysToAddToImpoundDate + extraHolidayOrWeekendDays).DayOfWeek == DayOfWeek.Sunday)
            {
                extraHolidayOrWeekendDays++;
            }

            return extraHolidayOrWeekendDays;
        }


        protected void GridView1_RowDeleting(object sender, GridViewDeleteEventArgs e)
        {
            int index = Convert.ToInt32(e.RowIndex);
            //  SQLite starts with 1, not 0, so need to add 1:
            index++;

            var con = ConnectToAndOpenDatabase();

            var cmd = new SQLiteCommand(con)
            {
                //  An actual delete:
                //CommandText = "DELETE FROM Vehicles WHERE UniqueID = " + index + ""
                CommandText = "UPDATE Vehicles SET IsDeleted = 1 WHERE UniqueID = " + index + ""
            };
            cmd.ExecuteNonQuery();

            BindGridViewAndCloseDatabase(con);
        }
    }
    /*
     * TESTING
     * _______
     * I decided to go with a .NET webform instead of .NET core early on in this take-home coding assignment
     * because it is what I worked with the most in school, so I figured I could get it done the fastest 
     * using that framework. Unfortunately, I realized later on in the process that it is difficult to write classes for or
     * Unit Test, and it is getting late in the week to start over, so here is an explanation of how I tested this:
     * 
     * Delete Buttons:
     * _______________
     * 1) Add a few rows to the database, delete two at random, and verify that the expected rows have "IsDeleted" set to "True".
     * 
     * Add Form
     * ________
     * Make/Model
     * __________
     * 1) Test an empty textbox, which should be valid.
     * 2) Test a random valid length, like say 7 characters long.
     * 3) Test the boundary length of 30 characters, which should be valid.
     * 4) Test one past the boundary, 31 char, which should not be valid. The error message should display.
     * 5) Test a random length past the boundary, such as 40 char, to make sure that is also invalid.
     * 
     * Year
     * ____
     * 1) Test that an empty textbox is valid and saves the year as -1.
     * 2) Test a few years in the first part of the RegEx, like 1900 and 1959 to make sure they are valid.
     * 3) Test a few years in the middle part of the RegEx, such as 2009 and 2012 (valid).
     * 4) Test 2020 and 2021 (valid).
     * 5) Test some non-valid years that are close to valid, like 1000, 2200, 2022, and 
     *      make sure they aren't valid and the error message displays.
     * 6) Test a string that's shorter (200) and longer (202020) to make sure invalid.
     * 7) Test a letter string that's the right length (abcd) and a special char string (/*.?) to make sure invalid.
     * 
     * Impound Date
     * ____________
     * 1) Test a date with a format of yyyy-mm-dd and a year between 1900-2021 and verify that it's valid.
     * 2) Test an invalid format, like mm-dd-yyyy and verify that "The impound date is in the wrong format." 
     *      shows up as the error message.
     * 3) Test a year not in the YearTextbox regex (this uses the same regex) and verify that the "Please enter a 
     *      four-digit year between 1900 and 2021." error message shows up.
     *      
     * Holidays
     * ________
     * There are 3 types of holidays:
     * 1) Holidays on specific day numbers.
     * 2) Holidays on specific day of week.
     * 3) The moon- and solstice-based weirdness that is Good Friday.
     * 
     * Test at least one of each individually (with other holidays commented-out), then test two that both occur 
     *      in a 42-day period to make sure they add together the way they should.
     *      
     * Holidays on Specific Day Numbers
     * ________________________________
     * Test Veterans's Day
     * ___________________
     * 1) Test the day of Veteran's Day (the boundary)(valid).
     * 2) Test the day after Veteran's Day (invalid).
     * 3) Test another day in November before Veteran's day (valid).
     * 4) Test a random day in October (valid).
     * 5) Test a day in September after Veteran's Day - 42 days... oh wait, that's the 30th, the end of the month, 
     *      so just test that (boundary)(valid).
     * 6) Test Sept 29th (invalid).
     * 7) Now try enough years to verify at least one time when 1 day is added, one time when it isn't (Veteran's Day
     *      falls on a Saturday or Sunday), and one where 2 or 3 days are added (43 days after the inpound date
     *      is a weekend).
     *      
     * Test Thanksgiving Day
     * _____________________
     * 1) Test that Thanksgiving Day is valid.
     * 2) Test that the day after it is not valid.
     * 3) Test that a random day in November before Thanksgiving is valid.
     * 4) Test that a random day in October after the start date is valid.
     * 5) Test that the start date in October is valid.
     * 6) Test that the day before the start date in October is not valid.
     * 7) Repeat the six above tests with a different year where Thanksgiving is on a different date.
     * 
     * Test Good Friday
     * ________________
     * 1) Follow the same testing rules as Thanksgiving.
     * 2) In addition, test one of each of these situations:
     *      a) Good Friday is in April, the 42+ days cover 3 months, leap year.
     *      b) Good Friday is in April, the 42+ days cover 3 months, not a leap year.
     *      c) Good Friday is in April, the 42+ days do not cover 3 months, doesn't matter if it's a leap year or not, no Feb.
     *      d) Good Friday is in March, the 42+ days cover 3 months, leap year.
     *      e) Good Friday is in March, the 42+ days cover 3 months, not a leap year.
     *      f) Good Friday is in March, the 42+ days do not cover 3 months, leap year.
     *      g) Good Friday is in March, the 42+ days do not cover 3 months, not a leap year.
     *      
     * Test Two Holidays to See If They Add On To Each Other
     * _____________________________________________________
     * 1) Test Veteran's day and Thankgiving at the same time.
     */
}