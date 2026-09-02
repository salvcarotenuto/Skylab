package it.skylab.mobile

import android.content.Context
import androidx.room.Dao
import androidx.room.Database
import androidx.room.Entity
import androidx.room.Insert
import androidx.room.OnConflictStrategy
import androidx.room.Query
import androidx.room.Room
import androidx.room.RoomDatabase
import androidx.room.Transaction

@Entity(tableName = "cached_works", primaryKeys = ["username", "workId"])
data class CachedWorkEntity(
    val username: String,
    val workId: Int,
    val number: String,
    val plannedOn: String,
    val plannedAt: String,
    val customer: String,
    val site: String,
    val summary: String,
    val status: String,
    val detailJson: String?,
    val synchronizedAt: Long
)

@Dao
interface CachedWorkDao {
    @Query("SELECT * FROM cached_works WHERE username = :username ORDER BY plannedOn, plannedAt, workId")
    suspend fun works(username: String): List<CachedWorkEntity>

    @Query("SELECT detailJson FROM cached_works WHERE username = :username AND workId = :workId")
    suspend fun detail(username: String, workId: Int): String?

    @Query("SELECT MAX(synchronizedAt) FROM cached_works WHERE username = :username")
    suspend fun lastSynchronization(username: String): Long?

    @Insert(onConflict = OnConflictStrategy.REPLACE)
    suspend fun insertAll(items: List<CachedWorkEntity>)

    @Query("DELETE FROM cached_works WHERE username = :username")
    suspend fun deleteForUser(username: String)

    @Query("UPDATE cached_works SET detailJson = :json, synchronizedAt = :synchronizedAt WHERE username = :username AND workId = :workId")
    suspend fun updateDetail(username: String, workId: Int, json: String, synchronizedAt: Long)

    @Transaction
    suspend fun replaceForUser(username: String, items: List<CachedWorkEntity>) {
        deleteForUser(username)
        if (items.isNotEmpty()) insertAll(items)
    }
}

@Database(entities = [CachedWorkEntity::class], version = 1, exportSchema = false)
abstract class SkyLabDatabase : RoomDatabase() {
    abstract fun cachedWorks(): CachedWorkDao

    companion object {
        @Volatile private var instance: SkyLabDatabase? = null

        fun get(context: Context): SkyLabDatabase = instance ?: synchronized(this) {
            instance ?: Room.databaseBuilder(
                context.applicationContext,
                SkyLabDatabase::class.java,
                "skylab-mobile.db"
            ).build().also { instance = it }
        }
    }
}
