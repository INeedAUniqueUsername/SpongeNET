using System;
using System.Collections.Generic;
using System.Text;

namespace SpongeNET
{
    class TimeString
    {
        public readonly int endHour;
        public readonly string[] str;

        public TimeString(int endHour, string[] str)
        {
            this.endHour = endHour;
            this.str = str;
        }
    }
    class Constants
    {
        public static readonly TimeString[] TIME_OF_DAY_STRINGS = new[]
            {
                new TimeString(
                    endHour: 2,
                    str: new[] {"It is the middle of the night", "It's around midnight", "Midnight is upon you" }
                ),
                new TimeString(
                    endHour: 5,
                    str: new[] {"It is the wee hours", "Morning comes soon", "Night is changing to day" }
                ),
                new TimeString(
                    endHour: 8,
                    str: new[] {"The sun is rising","Another day is beginning","Day is breaking"}
		        ),
		        new TimeString(
                    endHour: 10,
                    str: new[] { "It is early in the morning","The morning is young","It is just after daybreak"}
		        ),
		        new TimeString(
			        endHour: 12,
			        str: new[] {"It is mid-morning","It's the middle of morning","Morning grows on" }
		        ),
		        new TimeString(
			        endHour: 14,
			        str: new[] {"The sun is high in the sky","It is around noon","It's midday" }
		        ),
		        new TimeString(
			        endHour: 17,
			        str: new[] {"It's afternoon", "It is the afternoon", "It is the main part of the day" }
		        ),
		        new TimeString(
			        endHour: 20,
			        str: new[] {"The sun has gone down", "The sun has sunk low", "It's around sunset" }
		        ),
                new TimeString(
                    endHour: 23,
			        str: new[] {"It's late evening", "The night has just begun", "The night is young" }
		        )
	        };

        public static readonly string[] MONTHS = new[] { "Archuary", "Fooshuary", "Keplembler", "Wael", "Skarl", "Nicholaseptember", "Squishuary" };
        public static readonly int SUNRISE = 6,
            SUNSET = 18,
            DAYS_IN_YEAR = 360,
            TICKS_PER_HOUR = 10,
            HOURS_IN_DAY = 24,
            TICKS_IN_DAY = TICKS_PER_HOUR * HOURS_IN_DAY,
            DAYS_PER_MONTH = DAYS_IN_YEAR / MONTHS.Length;
    }
}
