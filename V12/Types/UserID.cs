namespace V12.Types
{
    public class UserID
    {
        public string ID { get; set; }
        public UserID(string id)
        {
            ID = id;
        }
        public override string ToString()
        {
            return ID;
        }
    }
}
