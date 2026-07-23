package hu.beosztasvarazslo.app.logic

import hu.beosztasvarazslo.app.data.ShiftTypeEntity
import java.time.DayOfWeek
import java.time.LocalDate
import java.time.YearMonth

enum class CellKind { WORK, VACATION, ABSENCE, REST, EMPTY, NONWORK }

data class CellInfo(val hours: Double, val kind: CellKind, val code: String? = null)

data class MonthSummary(
    val baseHours: Double,
    val employmentFactor: Double,
    val requiredFull: Double,
    val vacationDays: Int,
    val absenceDays: Int,
    val workDays: Int,
    val vacationHours: Double,
    val absenceHours: Double,
    val carryIn: Double,
    val effectiveRequired: Double,
    val actualHours: Double,
    val balance: Double
)

const val GROUP_TYPE_OFFICE = "IRODA"
const val GROUP_TYPE_GENERAL = "ALTALANOS"

/** Óraszám-számítási motor: kötelező havi óraszám, ledolgozott óra, maradvány (a web app calc.js-ének Kotlin megfelelője). */
object ScheduleCalculator {
    const val CODE_VACATION = "SZ"
    const val CODE_SICK = "BSZ"
    const val CODE_ABSENCE = "H"
    const val CODE_REST = "P"

    fun daysInMonth(year: Int, month: Int): Int = YearMonth.of(year, month).lengthOfMonth()

    fun isWeekend(year: Int, month: Int, day: Int): Boolean {
        val dow = LocalDate.of(year, month, day).dayOfWeek
        return dow == DayOfWeek.SATURDAY || dow == DayOfWeek.SUNDAY
    }

    fun isoDate(year: Int, month: Int, day: Int): String = "%04d-%02d-%02d".format(year, month, day)

    fun isHoliday(holidayMap: Map<String, String>, year: Int, month: Int, day: Int): Boolean =
        holidayMap.containsKey(isoDate(year, month, day))

    fun isOfficeWorkday(holidayMap: Map<String, String>, year: Int, month: Int, day: Int): Boolean =
        !isWeekend(year, month, day) && !isHoliday(holidayMap, year, month, day)

    /** Egy nap (dolgozó, csoport) órahatása és jellege. */
    fun cellHours(
        groupType: String,
        groupDailyHours: Double,
        shiftTypes: List<ShiftTypeEntity>,
        code: String?,
        isOfficeWorkdayFlag: Boolean
    ): CellInfo {
        if (groupType == GROUP_TYPE_OFFICE) {
            if (!isOfficeWorkdayFlag) return CellInfo(0.0, CellKind.NONWORK)
            return when (code) {
                CODE_VACATION -> CellInfo(0.0, CellKind.VACATION)
                CODE_ABSENCE, CODE_SICK -> CellInfo(0.0, CellKind.ABSENCE)
                else -> CellInfo(groupDailyHours, CellKind.WORK, "M")
            }
        }
        if (code.isNullOrEmpty()) return CellInfo(0.0, CellKind.EMPTY)
        return when (code) {
            CODE_VACATION -> CellInfo(0.0, CellKind.VACATION)
            CODE_ABSENCE, CODE_SICK -> CellInfo(0.0, CellKind.ABSENCE)
            CODE_REST -> CellInfo(0.0, CellKind.REST)
            else -> {
                val st = shiftTypes.find { it.code == code }
                if (st != null) CellInfo(st.hours, CellKind.WORK, st.code) else CellInfo(0.0, CellKind.EMPTY)
            }
        }
    }

    /** Egy dolgozó adott havi összesítése (bejövő kézi órával csökkentve a kötelező óraszámot). */
    fun summarizeMonth(
        groupType: String,
        groupDailyHours: Double,
        shiftTypes: List<ShiftTypeEntity>,
        employmentFactor: Double,
        baseMonthHours: Double,
        codesByDay: Map<Int, String>,
        holidayMap: Map<String, String>,
        carryIn: Double,
        year: Int,
        month: Int
    ): MonthSummary {
        val dim = daysInMonth(year, month)
        var actualHours = 0.0
        var vacationDays = 0
        var absenceDays = 0
        var workDays = 0
        for (d in 1..dim) {
            val code = codesByDay[d]
            val officeWorkday = isOfficeWorkday(holidayMap, year, month, d)
            val info = cellHours(groupType, groupDailyHours, shiftTypes, code, officeWorkday)
            when (info.kind) {
                CellKind.WORK -> { actualHours += info.hours; workDays++ }
                CellKind.VACATION -> vacationDays++
                CellKind.ABSENCE -> absenceDays++
                else -> {}
            }
        }
        val requiredFull = baseMonthHours * employmentFactor
        val vacationHours = vacationDays * groupDailyHours
        val absenceHours = absenceDays * groupDailyHours
        val effectiveRequired = requiredFull - vacationHours - absenceHours - carryIn
        val balance = actualHours - effectiveRequired
        return MonthSummary(
            baseMonthHours, employmentFactor, requiredFull, vacationDays, absenceDays, workDays,
            vacationHours, absenceHours, carryIn, effectiveRequired, actualHours, balance
        )
    }

    /** Egy adott napon, adott műszaktípus-kódonként hány dolgozó van beosztva. */
    fun shiftCoverage(shiftTypes: List<ShiftTypeEntity>, codesOfEmployeesThatDay: Collection<String?>): Map<String, Int> {
        val counts = shiftTypes.associate { it.code to 0 }.toMutableMap()
        codesOfEmployeesThatDay.forEach { code ->
            if (code != null && counts.containsKey(code)) counts[code] = counts.getValue(code) + 1
        }
        return counts
    }

    /** A cellára kattintva választható kódok (kód, megjelenített címke) párokban. */
    fun cellOptions(groupType: String, shiftTypes: List<ShiftTypeEntity>): List<Pair<String, String>> {
        if (groupType == GROUP_TYPE_OFFICE) {
            return listOf(
                "" to "Munka", CODE_VACATION to "SZ – Szabadság",
                CODE_SICK to "BSZ – Beteg szabadság", CODE_ABSENCE to "H – Hiányzás"
            )
        }
        val opts = mutableListOf("" to "—")
        shiftTypes.forEach { opts.add(it.code to (it.code + " – " + it.label)) }
        opts.add(CODE_VACATION to "SZ – Szabadság")
        opts.add(CODE_SICK to "BSZ – Beteg szabadság")
        opts.add(CODE_ABSENCE to "H – Hiányzás")
        opts.add(CODE_REST to "P – Pihenőnap")
        return opts
    }
}
