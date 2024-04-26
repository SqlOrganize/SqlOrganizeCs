namespace SqlOrganize
{

    /// <summary>
    /// Ejecucion de multiples instancias de persist
    /// </summary>
    internal class PersistStore 
    {
        public Db Db { get; }
        public List<EntityPersist> persists = new();

        public PersistStore(Db db)
        {
            Db = db;
        }

        public PersistStore Add(EntityPersist persist)
        {
            persists.Add(persist);
            return this;
        }

        public PersistStore Exec()
        {
            var q = Db.Query();
            using var conn = q.NewConnection();
            q.connection = conn;
            foreach (EntityPersist persist in persists)
            {
                q.sql = persist.sql;
                q.parameters = persist.parameters;
                q.Exec();
            }
            return this;
        }

        public PersistStore Transaction()
        {
            var q = Db.Query();
            using var conn = q.NewConnection();
            using var tran = conn.BeginTransaction();
            q.connection = conn;
            q.transaction = tran;
            foreach(EntityPersist persist in persists) { 
                q.sql = persist.sql;
                q.parameters = persist.parameters;
                q.Exec();
            }
            tran.Commit();
            return this;
        }

        public PersistStore RemoveCache()
        {
            if (persists.Count > 0)
                persists[0].RemoveCacheQueries();
            
            foreach (EntityPersist persist in persists)
                persist.RemoveCacheDetail();
            
            return this;
        }



    }
}
