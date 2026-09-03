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
import androidx.room.migration.Migration
import androidx.sqlite.db.SupportSQLiteDatabase

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

@Entity(tableName = "work_report_drafts", primaryKeys = ["username", "workId"])
data class WorkReportDraftEntity(
    val username: String,
    val workId: Int,
    val payloadJson: String,
    val updatedAt: Long,
    val status: String = "DRAFT",
    val submissionId: String = "",
    val confirmedAt: Long? = null,
    val sentAt: Long? = null,
    val attempts: Int = 0,
    val lastError: String = ""
)

@Entity(tableName = "mobile_catalog", primaryKeys = ["type", "reference"])
data class MobileCatalogEntity(
    val type: String,
    val reference: String,
    val description: String,
    val category: String,
    val unit: String,
    val price: Double,
    val price1: Double?,
    val price2: Double?,
    val price3: Double?,
    val price4: Double?,
    val price5: Double?,
    val price6: Double?,
    val barcodes: String,
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

    @Query("DELETE FROM cached_works WHERE username = :username AND plannedOn <> '' AND substr(plannedOn, 1, 10) < :cutoff")
    suspend fun deleteOlderThan(username: String, cutoff: String)

    @Query("UPDATE cached_works SET detailJson = :json, synchronizedAt = :synchronizedAt WHERE username = :username AND workId = :workId")
    suspend fun updateDetail(username: String, workId: Int, json: String, synchronizedAt: Long)

    @Transaction
    suspend fun replaceForUser(username: String, items: List<CachedWorkEntity>) {
        deleteForUser(username)
        if (items.isNotEmpty()) insertAll(items)
    }
}

@Dao
interface WorkReportDraftDao {
    @Query("SELECT * FROM work_report_drafts WHERE username = :username AND workId = :workId")
    suspend fun draft(username: String, workId: Int): WorkReportDraftEntity?

    @Insert(onConflict = OnConflictStrategy.REPLACE)
    suspend fun save(draft: WorkReportDraftEntity)

    @Query("SELECT * FROM work_report_drafts WHERE username = :username AND status = 'PENDING' ORDER BY confirmedAt, updatedAt")
    suspend fun pending(username: String): List<WorkReportDraftEntity>

    @Query("DELETE FROM work_report_drafts WHERE username = :username AND workId NOT IN (SELECT workId FROM cached_works WHERE username = :username)")
    suspend fun deleteWithoutCachedWork(username: String)
}

@Dao
interface MobileCatalogDao {
    @Query("SELECT * FROM mobile_catalog WHERE type = :type ORDER BY description, reference")
    suspend fun items(type: String): List<MobileCatalogEntity>

    @Insert(onConflict = OnConflictStrategy.REPLACE)
    suspend fun insertAll(items: List<MobileCatalogEntity>)

    @Query("DELETE FROM mobile_catalog")
    suspend fun deleteAll()

    @Transaction
    suspend fun replaceAll(items: List<MobileCatalogEntity>) {
        deleteAll()
        if (items.isNotEmpty()) insertAll(items)
    }
}

@Database(entities = [CachedWorkEntity::class, WorkReportDraftEntity::class, MobileCatalogEntity::class], version = 6, exportSchema = false)
abstract class SkyLabDatabase : RoomDatabase() {
    abstract fun cachedWorks(): CachedWorkDao
    abstract fun workReportDrafts(): WorkReportDraftDao
    abstract fun mobileCatalog(): MobileCatalogDao

    companion object {
        @Volatile private var instance: SkyLabDatabase? = null

        fun get(context: Context): SkyLabDatabase = instance ?: synchronized(this) {
            instance ?: Room.databaseBuilder(
                context.applicationContext,
                SkyLabDatabase::class.java,
                "skylab-mobile.db"
            ).addMigrations(MIGRATION_1_2, MIGRATION_2_3, MIGRATION_3_4, MIGRATION_4_5, MIGRATION_5_6).build().also { instance = it }
        }

        private val MIGRATION_1_2 = object : Migration(1, 2) {
            override fun migrate(db: SupportSQLiteDatabase) {
                db.execSQL("CREATE TABLE IF NOT EXISTS work_report_drafts (username TEXT NOT NULL, workId INTEGER NOT NULL, payloadJson TEXT NOT NULL, updatedAt INTEGER NOT NULL, PRIMARY KEY(username, workId))")
            }
        }

        private val MIGRATION_2_3 = object : Migration(2, 3) {
            override fun migrate(db: SupportSQLiteDatabase) {
                db.execSQL("CREATE TABLE IF NOT EXISTS mobile_catalog (type TEXT NOT NULL, reference TEXT NOT NULL, description TEXT NOT NULL, category TEXT NOT NULL, unit TEXT NOT NULL, price REAL NOT NULL, synchronizedAt INTEGER NOT NULL, PRIMARY KEY(type, reference))")
            }
        }
        private val MIGRATION_3_4 = object : Migration(3, 4) {
            override fun migrate(db: SupportSQLiteDatabase) {
                db.execSQL("ALTER TABLE mobile_catalog ADD COLUMN price1 REAL")
                db.execSQL("ALTER TABLE mobile_catalog ADD COLUMN price2 REAL")
                db.execSQL("ALTER TABLE mobile_catalog ADD COLUMN price3 REAL")
                db.execSQL("ALTER TABLE mobile_catalog ADD COLUMN price4 REAL")
                db.execSQL("ALTER TABLE mobile_catalog ADD COLUMN price5 REAL")
                db.execSQL("ALTER TABLE mobile_catalog ADD COLUMN price6 REAL")
            }
        }
        private val MIGRATION_4_5 = object : Migration(4, 5) {
            override fun migrate(db: SupportSQLiteDatabase) {
                db.execSQL("ALTER TABLE mobile_catalog ADD COLUMN barcodes TEXT NOT NULL DEFAULT ''")
            }
        }
        private val MIGRATION_5_6 = object : Migration(5, 6) {
            override fun migrate(db: SupportSQLiteDatabase) {
                db.execSQL("ALTER TABLE work_report_drafts ADD COLUMN status TEXT NOT NULL DEFAULT 'DRAFT'")
                db.execSQL("ALTER TABLE work_report_drafts ADD COLUMN submissionId TEXT NOT NULL DEFAULT ''")
                db.execSQL("ALTER TABLE work_report_drafts ADD COLUMN confirmedAt INTEGER")
                db.execSQL("ALTER TABLE work_report_drafts ADD COLUMN sentAt INTEGER")
                db.execSQL("ALTER TABLE work_report_drafts ADD COLUMN attempts INTEGER NOT NULL DEFAULT 0")
                db.execSQL("ALTER TABLE work_report_drafts ADD COLUMN lastError TEXT NOT NULL DEFAULT ''")
            }
        }
    }
}
