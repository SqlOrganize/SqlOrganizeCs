namespace SqlOrganize.Sql
{
    public class EntityFieldId
    {
        public Db db { get; }
        public string entityName { get; }

        public string? fieldId { get; set; }

        public EntityFieldId(Db _db, string _entityName, string? _fieldId = null)
        {
            db = _db;
            entityName = _entityName;
            fieldId = _fieldId;
        }

        public string Pf()
        {
            return (!fieldId.IsNoE()) ? fieldId! + "-" : "";
        }

        public string Pt()
        {
            return (!fieldId.IsNoE()) ? fieldId! : db.Entity(entityName).alias;
        }

       

    }
}
