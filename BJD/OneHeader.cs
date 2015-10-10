namespace Bjd
{
    public class OneHeader
    {
        public string Key { get; set; }
        public byte[] Val { get; set; }
        public OneHeader(string key, byte[] val)
        {
            Key = key;
            Val = val;
        }
    }
}