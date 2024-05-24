using SqlOrganize;
using Utils;

namespace SqlOrganizeMy
{
    public class EntitySqlMy : EntitySql
    {

        public EntitySqlMy(Db db, string entityName) : base(db, entityName)
        {
        }

        protected override string SqlLimit()
        {
            if (size == 0) return "";
            page = page == 0 ? 1 : page;
            return "LIMIT " + size + " OFFSET " + ((page - 1) * size) + @"
";
        }

        protected override string SqlOrder()
        {
            if (order.IsNullOrEmpty())
            {
                var o = Db.Entity(entityName).orderDefault;
                order = o.IsNullOrEmpty() ? "" : string.Join(", ", o.Select(x => "$" + x));
            }

            return (order.IsNullOrEmpty()) ? "" : "ORDER BY " + Traduce(order!) + @"
";
        }

        public override EntitySql Clone()
        {
            var eq = new EntitySqlMy(Db, entityName);
            return _Clone(eq);
        }
		
		protected override SqlFields()
		{

            var f = _SqlFieldsInit();
            return f + @"
";
        }
    }

    public override EntitySql SelectMaxValueCast(string fieldName, string sqlType)
    {
        select += "CAST ( ISNULL( MAX($" + fieldName + "), 0) AS " + sqlType + ")";
        return this;
    }

}
