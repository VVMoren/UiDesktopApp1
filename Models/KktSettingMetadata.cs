namespace UiDesktopApp1.Models
{
    public class KktSettingMetadata
    {
        public string Category { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public List<ValueText> Options { get; set; } = new(); // 👈 вот это обязательно!
    }
}