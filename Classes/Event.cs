namespace WebAPI.Classes
{
    public class Event
    {
        public short ID;                    // Row ID 0-23
        public short StartHour = 08;        // 24 hour clock
        public short StartMinute = 30;      //
        public short EndHour = 20;          // 24 hour clock
        public short EndMinute = 30;        //
        public short DayOfWeek = 0x7F;      //  MN TU WE TH FR SA SU  (0=OFF / 1=ON) - 1111100  (off for sat and sunday and on for MN-FR)
        public short FanSpeed = 1;          //
        public short WorkSeconds = 15;      //
        public short PauseSeconds = 60;     //
        public short LevelIndex = 2;        //

        public override string ToString()
        {
            return  "sh=" + StartHour +
                    "|sm=" + StartMinute +
                    "|eh=" + EndHour +
                    "|em=" + EndMinute +
                    "|dw=" + DayOfWeek +
                    "|fn=" + FanSpeed +
                    "|ws=" + WorkSeconds +
                    "|ps=" + PauseSeconds +
                    "|lv=" + LevelIndex;
        }
    }
}
