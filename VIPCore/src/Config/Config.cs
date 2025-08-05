using CounterStrikeSharp.API.Core;

namespace VIPCore;

public class PluginConfig : BasePluginConfig
{
    public Database_Config Database { get; set; } = new();
    public Commands_Config Commands { get; set; } = new();
    public Dictionary<string, VIPGroup_Config> Groups { get; set; } = new()
    {
        {
            "ADMIN", new VIPGroup_Config
            {
                Features = new Dictionary<string, object>
                {
                    { "Health", 110 },
                    { "Armor", 110 },

                }
            }
        },
        {
            "SILVER-VIP", new VIPGroup_Config
            {
                Features = new Dictionary<string, object>
                {
                    { "Health", 110 },
                    { "Armor", 110 },
                    { "Bhop", true },
                }
            }
        },
        {
            "GOLD-VIP", new VIPGroup_Config
            {
                Features = new Dictionary<string, object>
                {
                    { "Health", 115 },
                    { "Armor", 115 },
                    { "Bhop", true },
                }
            }
        },
        {
            "PLATINUM-VIP", new VIPGroup_Config
            {
                Features = new Dictionary<string, object>
                {
                    { "Health", 125 },
                    { "Armor", 125 },
                    { "Bhop", true },
                }
            }
        },
        {
            "SPECIAL-VIP", new VIPGroup_Config
            {
                Features = new Dictionary<string, object>
                {
                    { "Health", 150 },
                    { "Armor", 150 },
                    { "Bhop", true },
                }
            }
        }
    };
}

public class Database_Config
{
    public string Host { get; set; } = "localhost";
    public string Name { get; set; } = "name";
    public string User { get; set; } = "user";
    public string Pass { get; set; } = "password";
    public uint Port { get; set; } = 3306;
    public string SslMode { get; set; } = "None";
}
public class Commands_Config
{
    public List<string> AddVip { get; set; } = ["addvip"]; // adauga vip ✅
    public List<string> RemoveVip { get; set; } = ["removevip"]; // inlatura vip ✅
    public List<string> UpdateVip { get; set; } = ["updatevip"]; // actualizeaza vip ✅
    public List<string> ListVips { get; set; } = ["vip"]; // list vips ✅
    public List<string> VipMenu { get; set; } = ["vmenu"]; // vip menu ✅
    public List<string> BuyVip { get; set; } = ["buyvip"]; // cumpara vip ✅
    public List<string> FreeVip { get; set; } = ["freevip"]; // free vip menu ✅
    public List<string> SVip { get; set; } = ["svip"]; // vip models ❌ 
    public List<string> HelpVip { get; set; } = ["helpvip"]; // apare toate comenzile de sus. ❌ 
}
public class VIPGroup_Config
{
    public Dictionary<string, object> Features { get; set; } = new();
}

