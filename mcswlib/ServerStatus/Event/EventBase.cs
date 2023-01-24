namespace mcswlib.ServerStatus.Event
{
    public abstract class EventBase
    {
        protected EventMessages Messages;

        internal EventBase(EventMessages msg)
        {
            Messages = msg;
        }

        /// <summary>
        ///     This function needs to be overwritten to return the event-specific message
        /// </summary>
        /// <returns></returns>
        public override string ToString() { return GetType().FullName!; }
    }
}