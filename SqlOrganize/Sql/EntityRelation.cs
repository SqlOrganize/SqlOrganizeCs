namespace SqlOrganize.Sql
{
    public class EntityRelation
    {
        public string fieldName { get; set; }
        public string refEntityName { get; set; }
        public string refFieldName { get; set; } = "id";
        public string? parentId { get; set; }
    }
}
