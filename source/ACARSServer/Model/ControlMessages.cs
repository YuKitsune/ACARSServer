using ACARSServer.Contracts;

namespace ACARSServer.Model;

public static class ControlMessages
{
    public static bool IsLogonRequest(CpdlcDownlink cpdlcDownlink)
    {
        return cpdlcDownlink.Content == "REQUEST LOGON";
    }

    public static bool IsLogoffNotice(CpdlcDownlink cpdlcDownlink)
    {
        return cpdlcDownlink.Content == "LOGOFF";
    }
}