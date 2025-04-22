using SharedProject.Enums;

namespace GameDatabase.Entities;

public class DanBorder
{
    public uint DanId { get; set; }
    public uint BorderIdx { get; set; }
    public DanConditionType OdaiType { get; set; }
    public DanBorderType BorderType { get; set; }
    public uint RedBorderTotal { get; set; }
    public uint GoldBorderTotal { get; set; }
    public DanInfo? DanInfo { get; set; }
}
