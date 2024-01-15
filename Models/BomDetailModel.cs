namespace Deestone.Models
{
  public class BomDetailModel
  {
    public string BOMID { get; set; }
    public string BOMNAME { get; set; }
    public string ITEMID { get; set; }
    public string ITEMNAME { get; set; }
    public string UNITID { get; set; }
    public double BOMQTY { get; set; }
    public double BOMQTYSERIE { get; set; }
    public string OLD_ITEMID { get; set; }
    public string OLD_ITEMNAME { get; set; }
    public string OLD_UNITID { get; set; }
    public double OLD_BOMQTY { get; set; }
    public double OLD_BOMQTYSERIE { get; set; }
    public int AX_STATUS { get; set; }
  }
}