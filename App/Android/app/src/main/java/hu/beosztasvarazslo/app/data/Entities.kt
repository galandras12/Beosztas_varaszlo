package hu.beosztasvarazslo.app.data

import androidx.room.Entity
import androidx.room.ForeignKey
import androidx.room.Index
import androidx.room.PrimaryKey

/** Munkacsoport (munkaág): pl. Ápolók, Takarítók, Részmunkaidősök, Irodai dolgozók. */
@Entity(tableName = "work_groups")
data class WorkGroupEntity(
    @PrimaryKey(autoGenerate = true) val id: Long = 0,
    val name: String,
    /** "ALTALANOS" (bármely nap, akár váltásos) vagy "IRODA" (hétfő-péntek, ünnepnap kivétel). */
    val type: String,
    val dailyHours: Double,
    /** Egy műszakban szükséges létszám; 0 = nincs figyelve. */
    val staffPerShift: Int,
    /** Automatikus kitöltésnél megkövetelt min. pihenőidő (óra) egy műszak után. */
    val minRestHours: Int = 24
)

/** Egy munkacsoporton belüli műszaktípus (pl. Nappal/Éjszaka az ápolóknál). */
@Entity(
    tableName = "shift_types",
    foreignKeys = [ForeignKey(
        entity = WorkGroupEntity::class,
        parentColumns = ["id"],
        childColumns = ["groupId"],
        onDelete = ForeignKey.CASCADE
    )],
    indices = [Index("groupId")]
)
data class ShiftTypeEntity(
    @PrimaryKey(autoGenerate = true) val id: Long = 0,
    val groupId: Long,
    val code: String,
    val label: String,
    val hours: Double
)

/** Dolgozó. */
@Entity(
    tableName = "employees",
    foreignKeys = [ForeignKey(
        entity = WorkGroupEntity::class,
        parentColumns = ["id"],
        childColumns = ["groupId"],
        onDelete = ForeignKey.RESTRICT
    )],
    indices = [Index("groupId")]
)
data class EmployeeEntity(
    @PrimaryKey(autoGenerate = true) val id: Long = 0,
    val name: String,
    val groupId: Long,
    /** 1.0 = teljes munkaidő, 0.5 = fél (pl. napi 4 óra), stb. */
    val employmentFactor: Double,
    /** Kötelezően kitöltendő: az adott dolgozónak évente kiadható szabadságnapok max. száma. */
    val maxVacationDays: Int,
    val notes: String = ""
)

/** Egy dolgozó egy napjának beosztási kódja (pl. "N", "É", "M", "SZ", "H", "P"). */
@Entity(
    tableName = "schedule_entries",
    foreignKeys = [ForeignKey(
        entity = EmployeeEntity::class,
        parentColumns = ["id"],
        childColumns = ["employeeId"],
        onDelete = ForeignKey.CASCADE
    )],
    indices = [Index(value = ["employeeId", "year", "month", "day"], unique = true)]
)
data class ScheduleEntryEntity(
    @PrimaryKey(autoGenerate = true) val id: Long = 0,
    val employeeId: Long,
    val year: Int,
    val month: Int,
    val day: Int,
    val code: String
)

/** Egy dolgozó adott havi kézzel beírt "bejövő" (előző hónapról áthozott) óraszáma. */
@Entity(
    tableName = "carry_overs",
    foreignKeys = [ForeignKey(
        entity = EmployeeEntity::class,
        parentColumns = ["id"],
        childColumns = ["employeeId"],
        onDelete = ForeignKey.CASCADE
    )],
    indices = [Index(value = ["employeeId", "year", "month"], unique = true)]
)
data class CarryOverEntity(
    @PrimaryKey(autoGenerate = true) val id: Long = 0,
    val employeeId: Long,
    val year: Int,
    val month: Int,
    val hours: Double
)

/** Havi kötelező óraszám / munkanap alapérték (1..12 hónap). */
@Entity(tableName = "month_hours")
data class MonthHoursEntity(
    @PrimaryKey val month: Int,
    val hours: Double,
    val days: Int
)

/** Kézzel hozzáadott egyedi ünnepnap egy adott évben. */
@Entity(tableName = "holiday_extra", indices = [Index(value = ["year", "date"], unique = true)])
data class HolidayExtraEntity(
    @PrimaryKey(autoGenerate = true) val id: Long = 0,
    val year: Int,
    val date: String, // ISO "YYYY-MM-DD"
    val name: String
)

/** Egy automatikusan számolt (fix vagy húsvéthez kötött) ünnepnap kikapcsolása egy adott évben. */
@Entity(tableName = "holiday_removed", indices = [Index(value = ["year", "date"], unique = true)])
data class HolidayRemovedEntity(
    @PrimaryKey(autoGenerate = true) val id: Long = 0,
    val year: Int,
    val date: String
)
