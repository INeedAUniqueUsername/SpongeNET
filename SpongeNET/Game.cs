using System;
using System.Collections.Generic;
using System.Text;
using DSharpPlus.EventArgs;
using static SpongeNET.Constants;
using Message = DSharpPlus.EventArgs.MessageCreateEventArgs;
namespace SpongeNET
{
    class WorldTime
    {
        public int ticks;
        public TimeInstant Calculate() => Calculate(ticks);
        public TimeInstant Calculate(int t)
        {
            int year = t / (TICKS_IN_DAY * DAYS_IN_YEAR);
            int left = t - (year * TICKS_IN_DAY * DAYS_IN_YEAR);
            int month = left / (TICKS_IN_DAY * DAYS_PER_MONTH);
            left = left - (month * TICKS_IN_DAY * DAYS_PER_MONTH);
            int day = left / TICKS_IN_DAY;
            left = left - TICKS_IN_DAY * day;
            int hour = left / TICKS_PER_HOUR;
            left = left - TICKS_PER_HOUR * hour;
            return new TimeInstant(year, month, day, hour, left);
        }
    }
    class TimeInstant
    {
        public readonly int year, month, day, hour, remain;

        public TimeInstant(int year, int month, int day, int hour, int remain)
        {
            this.year = year;
            this.month = month;
            this.day = day;
            this.hour = hour;
            this.remain = remain;
        }
    }
    class Game
    {
        WorldTime time;
        Dictionary<string, ICommand> commands;
        public Game()
        {
            commands = new Dictionary<string, ICommand> {
                { "time", new Command(
                        Name: "time",
                        Help: "Get info on the current MUD world date and time.",
                        Invoke: Time
                        )
                }
            };
        }
        public void Time(Message m)
        {
            CommandString s = new CommandString(m.Message.Content);
            TimeInstant date;
            string reply = "";
            if(s.GetArg(0, out string arg))
            {
                if(int.TryParse(arg, out int t))
                {
                    date = time.Calculate(t);
                    goto Done;
                }
            }
            reply = "That would be on ";
            date = time.Calculate();

            Done:

            var found = false;
            var strBucket = 0;
            for (var strNum = 0; strNum < TIME_OF_DAY_STRINGS.Length && !found; strNum++)
            {
                if (date.hour < TIME_OF_DAY_STRINGS[strNum].endHour)
                {
                    found = true;
                    strBucket = strNum;
                }
            }
            string timeStr;
            string[] timeStrArr;
            int flavorNum;
            // timeStr = `hour ${time.hour}`;
            timeStrArr = TIME_OF_DAY_STRINGS[strBucket].str;
            flavorNum = (player.id + time.day) % timeStrArr.length;
            timeStr = timeStrArr[flavorNum];

            outP += `${ timeStr}
            on day ${ time.day + 1}
            of the month of ${ cons.MONTHS[time.month]}, year ${ time.year}.`;
            outP += `\n\nThere are ${ cons.DAYS_IN_YEAR}
            days in a year. There are ${ daysPerMonth}
            days`;
            outP += `  in each of the ${ cons.MONTHS.length}
            months`;
            if (extraDays) { outP += `, except for ${ cons.MONTHS[cons.MONTHS.length - 1]}, which has ${ extraDays} extra.`; }
            outP += `\nA worldtick happens every ${ cons.WORLDTICKLENGTH / 1000}
            seconds, `;
            outP += `and there are ${ cons.TICKS_IN_DAY}
            ticks in a day, or ~${ parseFloat(cons.TICKS_IN_DAY / 24, 2)}
            per MUD hour.`;

            ut.chSend(message, outP);


        }
    }
    interface ICommand {
        string Name { get; }
        string Help { get; }
        Action<Message> Invoke { get; }
    }
    class Command : ICommand {
        public readonly string Name;
        public readonly string Help;
        public readonly Action<Message> Invoke;
        public Command(string Name, string Help, Action<Message> Invoke)
        {
            this.Name = Name;
            this.Help = Help;
            this.Invoke = Invoke;
        }

        string ICommand.Name => throw new NotImplementedException();

        string ICommand.Help => throw new NotImplementedException();

        Action<Message> ICommand.Invoke => throw new NotImplementedException();
    }
}
