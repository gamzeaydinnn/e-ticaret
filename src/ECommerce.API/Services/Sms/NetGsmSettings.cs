namespace ECommerce.API.Services.Sms;

/// <summary>
/// NetGSM SMS API yapılandırması
/// </summary>
public class NetGsmSettings
{
    public string UserCode { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string MsgHeader { get; set; } = string.Empty;
    public string AppName { get; set; } = "GolkoyGurme";
}
