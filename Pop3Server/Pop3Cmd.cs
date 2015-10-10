namespace Pop3Server
{
    enum Pop3Cmd{
        Quit,
        Noop,
        User,
        Pass,
        Stat,
        List,
        Retr,
        Dele,
        Top,
        Uidl,
        Rset,
        Apop,
        Chps,
        Unknown
    }
}