using Microsoft.Data.Sqlite;

namespace ZooTycoonManager
{
    public interface ISaveable
    {
        void Save(SqliteTransaction transaction);
    }
} 