namespace LogisticsPlatform.Testing.Extensions.Models
{
    public class SectionOptions
    {
        public string Name { get; set; }
        public List<FieldOptions> Fields { get; set; }
        public string KeyFieldName { get; set; }
        public string IdentifierFieldName { get; set; }
    }
}
