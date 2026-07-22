package hu.beosztasvarazslo.app.data

import androidx.room.Dao
import androidx.room.Delete
import androidx.room.Insert
import androidx.room.OnConflictStrategy
import androidx.room.Query
import androidx.room.Update

@Dao
interface WorkGroupDao {
    @Query("SELECT * FROM work_groups ORDER BY name COLLATE NOCASE")
    suspend fun getAll(): List<WorkGroupEntity>

    @Query("SELECT * FROM work_groups WHERE id = :id")
    suspend fun getById(id: Long): WorkGroupEntity?

    @Insert
    suspend fun insert(group: WorkGroupEntity): Long

    @Update
    suspend fun update(group: WorkGroupEntity)

    @Delete
    suspend fun delete(group: WorkGroupEntity)

    @Query("SELECT COUNT(*) FROM work_groups")
    suspend fun count(): Int
}

@Dao
interface ShiftTypeDao {
    @Query("SELECT * FROM shift_types WHERE groupId = :groupId ORDER BY id")
    suspend fun getForGroup(groupId: Long): List<ShiftTypeEntity>

    @Insert
    suspend fun insertAll(shiftTypes: List<ShiftTypeEntity>)

    @Query("DELETE FROM shift_types WHERE groupId = :groupId")
    suspend fun deleteForGroup(groupId: Long)
}

@Dao
interface EmployeeDao {
    @Query("SELECT * FROM employees ORDER BY name COLLATE NOCASE")
    suspend fun getAll(): List<EmployeeEntity>

    @Query("SELECT * FROM employees WHERE groupId = :groupId ORDER BY name COLLATE NOCASE")
    suspend fun getForGroup(groupId: Long): List<EmployeeEntity>

    @Query("SELECT COUNT(*) FROM employees WHERE groupId = :groupId")
    suspend fun countForGroup(groupId: Long): Int

    @Insert
    suspend fun insert(employee: EmployeeEntity): Long

    @Update
    suspend fun update(employee: EmployeeEntity)

    @Delete
    suspend fun delete(employee: EmployeeEntity)
}

@Dao
interface ScheduleDao {
    @Query("SELECT * FROM schedule_entries")
    suspend fun getAll(): List<ScheduleEntryEntity>

    @Query("SELECT * FROM schedule_entries WHERE employeeId = :employeeId AND year = :year AND month = :month")
    suspend fun getForEmployeeMonth(employeeId: Long, year: Int, month: Int): List<ScheduleEntryEntity>

    @Query("SELECT * FROM schedule_entries WHERE year = :year AND month = :month")
    suspend fun getForMonth(year: Int, month: Int): List<ScheduleEntryEntity>

    @Query("SELECT * FROM schedule_entries WHERE employeeId = :employeeId AND year = :year")
    suspend fun getForEmployeeYear(employeeId: Long, year: Int): List<ScheduleEntryEntity>

    @Insert(onConflict = OnConflictStrategy.REPLACE)
    suspend fun upsert(entry: ScheduleEntryEntity)

    @Query("DELETE FROM schedule_entries WHERE employeeId = :employeeId AND year = :year AND month = :month AND day = :day")
    suspend fun deleteCell(employeeId: Long, year: Int, month: Int, day: Int)
}

@Dao
interface CarryOverDao {
    @Query("SELECT * FROM carry_overs")
    suspend fun getAll(): List<CarryOverEntity>

    @Query("SELECT * FROM carry_overs WHERE employeeId = :employeeId AND year = :year AND month = :month LIMIT 1")
    suspend fun getForEmployeeMonth(employeeId: Long, year: Int, month: Int): CarryOverEntity?

    @Insert(onConflict = OnConflictStrategy.REPLACE)
    suspend fun upsert(entry: CarryOverEntity)
}

@Dao
interface MonthHoursDao {
    @Query("SELECT * FROM month_hours ORDER BY month")
    suspend fun getAll(): List<MonthHoursEntity>

    @Insert(onConflict = OnConflictStrategy.REPLACE)
    suspend fun upsertAll(rows: List<MonthHoursEntity>)

    @Query("SELECT COUNT(*) FROM month_hours")
    suspend fun count(): Int
}

@Dao
interface HolidayDao {
    @Query("SELECT * FROM holiday_extra")
    suspend fun getAllExtra(): List<HolidayExtraEntity>

    @Query("SELECT * FROM holiday_removed")
    suspend fun getAllRemoved(): List<HolidayRemovedEntity>

    @Query("SELECT * FROM holiday_extra WHERE year = :year")
    suspend fun getExtraForYear(year: Int): List<HolidayExtraEntity>

    @Insert(onConflict = OnConflictStrategy.REPLACE)
    suspend fun insertExtra(entry: HolidayExtraEntity)

    @Query("DELETE FROM holiday_extra WHERE year = :year AND date = :date")
    suspend fun deleteExtra(year: Int, date: String)

    @Query("SELECT date FROM holiday_removed WHERE year = :year")
    suspend fun getRemovedForYear(year: Int): List<String>

    @Insert(onConflict = OnConflictStrategy.REPLACE)
    suspend fun insertRemoved(entry: HolidayRemovedEntity)

    @Query("DELETE FROM holiday_removed WHERE year = :year AND date = :date")
    suspend fun deleteRemoved(year: Int, date: String)
}
