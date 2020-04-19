namespace CML
{
    public abstract class CmlListener
    {
        protected CmlListener()
        {
            Program.ActiveListeners.Add(this);
        }

        public abstract void Listen();

        public abstract void Close();
    }
}
