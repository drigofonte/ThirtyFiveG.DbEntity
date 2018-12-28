using System;

namespace ThirtyFiveG.DbEntity.Event
{
    public class DbEntityPropertyChangedEventArgs : EventArgs
    {
        public DbEntityPropertyChangedEventArgs(string propertyName, object before, object after)
        {
            PropertyName = propertyName;
            Before = before;
            After = after;
        }

        public string PropertyName { get; private set; }
        public object Before { get; private set; }
        public object After { get; private set; }
    }
}
