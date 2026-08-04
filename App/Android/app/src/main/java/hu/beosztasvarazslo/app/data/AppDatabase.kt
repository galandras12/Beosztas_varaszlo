package hu.beosztasvarazslo.app.data

import android.content.Context
import androidx.room.Database
import androidx.room.Room
import androidx.room.RoomDatabase

/**
 * Egyetlen, titkosítatlan SQLite fájl (app-specifikus tárhelyen), szerver nélkül -
 * ez a program "belső adatbázisa".
 */
@Database(
    entities = [
        WorkGroupEntity::class, ShiftTypeEntity::class, EmployeeEntity::class,
        ScheduleEntryEntity::class, CarryOverEntity::class, MonthHoursEntity::class,
        HolidayExtraEntity::class, HolidayRemovedEntity::class
    ],
    version = 3,
    exportSchema = false
)
abstract class AppDatabase : RoomDatabase() {
    abstract fun workGroupDao(): WorkGroupDao
    abstract fun shiftTypeDao(): ShiftTypeDao
    abstract fun employeeDao(): EmployeeDao
    abstract fun scheduleDao(): ScheduleDao
    abstract fun carryOverDao(): CarryOverDao
    abstract fun monthHoursDao(): MonthHoursDao
    abstract fun holidayDao(): HolidayDao

    companion object {
        private const val DB_NAME = "beosztas_varazslo.db"

        @Volatile
        private var instance: AppDatabase? = null

        fun getInstance(context: Context): AppDatabase =
            instance ?: synchronized(this) {
                instance ?: Room.databaseBuilder(context.applicationContext, AppDatabase::class.java, DB_NAME)
                    // Nincs éles, publikált adat, amit meg kellene őrizni verzióváltáskor -
                    // sémaváltozás esetén egyszerűen újra létrejön az adatbázis.
                    .fallbackToDestructiveMigration()
                    .build()
                    .also { instance = it }
            }

        fun databaseFile(context: Context) = context.getDatabasePath(DB_NAME)
    }
}
