package hu.beosztasvarazslo.app.data

import androidx.room.withTransaction
import hu.beosztasvarazslo.app.logic.GROUP_TYPE_GENERAL
import hu.beosztasvarazslo.app.logic.GROUP_TYPE_OFFICE
import hu.beosztasvarazslo.app.logic.HungarianHolidays
import hu.beosztasvarazslo.app.logic.ScheduleCalculator
import org.json.JSONArray
import org.json.JSONObject

/** Magas szintű adatelérés: a Room DAO-k fölé épített, a UI által használt műveletek. */
class AppRepository(private val db: AppDatabase) {

    private val groupDao = db.workGroupDao()
    private val shiftTypeDao = db.shiftTypeDao()
    private val employeeDao = db.employeeDao()
    private val scheduleDao = db.scheduleDao()
    private val carryOverDao = db.carryOverDao()
    private val monthHoursDao = db.monthHoursDao()
    private val holidayDao = db.holidayDao()

    companion object {
        val MONTH_NAMES = listOf(
            "Január", "Február", "Március", "Április", "Május", "Június",
            "Július", "Augusztus", "Szeptember", "Október", "November", "December"
        )

        val DEFAULT_MONTH_HOURS: Map<Int, Pair<Double, Int>> = mapOf(
            1 to (176.0 to 22), 2 to (168.0 to 21), 3 to (152.0 to 19), 4 to (168.0 to 21),
            5 to (168.0 to 21), 6 to (160.0 to 20), 7 to (184.0 to 23), 8 to (168.0 to 21),
            9 to (168.0 to 21), 10 to (176.0 to 22), 11 to (160.0 to 20), 12 to (160.0 to 20)
        )
    }

    // ---------- Kezdeti feltöltés ----------

    suspend fun ensureSeeded() {
        if (monthHoursDao.count() == 0) resetMonthHoursToDefault()
        if (groupDao.count() == 0) seedDefaultGroups()
    }

    private suspend fun seedDefaultGroups() {
        val apolok = groupDao.insert(WorkGroupEntity(name = "Ápolók", type = GROUP_TYPE_GENERAL, dailyHours = 12.0, staffPerShift = 2))
        shiftTypeDao.insertAll(listOf(
            ShiftTypeEntity(groupId = apolok, code = "N", label = "Nappal", hours = 12.0),
            ShiftTypeEntity(groupId = apolok, code = "É", label = "Éjszaka", hours = 12.0)
        ))
        val takarito = groupDao.insert(WorkGroupEntity(name = "Takarítók", type = GROUP_TYPE_GENERAL, dailyHours = 8.0, staffPerShift = 0))
        shiftTypeDao.insertAll(listOf(ShiftTypeEntity(groupId = takarito, code = "M", label = "Munka", hours = 8.0)))

        val reszmunka = groupDao.insert(WorkGroupEntity(name = "Részmunkaidősök", type = GROUP_TYPE_GENERAL, dailyHours = 4.0, staffPerShift = 0))
        shiftTypeDao.insertAll(listOf(ShiftTypeEntity(groupId = reszmunka, code = "M", label = "Munka", hours = 4.0)))

        val iroda = groupDao.insert(WorkGroupEntity(name = "Irodai dolgozók", type = GROUP_TYPE_OFFICE, dailyHours = 8.0, staffPerShift = 0))
        shiftTypeDao.insertAll(listOf(ShiftTypeEntity(groupId = iroda, code = "M", label = "Munka", hours = 8.0)))
    }

    suspend fun resetMonthHoursToDefault() {
        monthHoursDao.upsertAll(DEFAULT_MONTH_HOURS.map { (m, hd) -> MonthHoursEntity(m, hd.first, hd.second) })
    }

    suspend fun resetAll() {
        db.clearAllTables()
        ensureSeeded()
    }

    // ---------- Munkacsoportok ----------

    suspend fun getGroupsWithShiftTypes(): List<GroupWithShiftTypes> =
        groupDao.getAll().map { GroupWithShiftTypes(it, shiftTypeDao.getForGroup(it.id)) }

    suspend fun getGroupWithShiftTypes(groupId: Long): GroupWithShiftTypes? {
        val g = groupDao.getById(groupId) ?: return null
        return GroupWithShiftTypes(g, shiftTypeDao.getForGroup(groupId))
    }

    suspend fun employeeCountForGroup(groupId: Long): Int = employeeDao.countForGroup(groupId)

    /** Létrehoz vagy frissít egy munkacsoportot a hozzá tartozó műszaktípusokkal együtt. */
    suspend fun saveGroup(group: WorkGroupEntity, shiftTypes: List<ShiftTypeEntity>): Long {
        val groupId = if (group.id == 0L) groupDao.insert(group) else {
            groupDao.update(group); group.id
        }
        shiftTypeDao.deleteForGroup(groupId)
        shiftTypeDao.insertAll(shiftTypes.map { it.copy(id = 0, groupId = groupId) })
        return groupId
    }

    /** @return true ha törölve lett, false ha vannak hozzá rendelt dolgozók. */
    suspend fun deleteGroup(group: WorkGroupEntity): Boolean {
        if (employeeDao.countForGroup(group.id) > 0) return false
        groupDao.delete(group)
        return true
    }

    // ---------- Dolgozók ----------

    suspend fun getEmployees(): List<EmployeeEntity> = employeeDao.getAll()

    suspend fun getEmployeesForGroup(groupId: Long): List<EmployeeEntity> = employeeDao.getForGroup(groupId)

    suspend fun saveEmployee(employee: EmployeeEntity): Long =
        if (employee.id == 0L) employeeDao.insert(employee) else { employeeDao.update(employee); employee.id }

    suspend fun deleteEmployee(employee: EmployeeEntity) = employeeDao.delete(employee)

    // ---------- Havi óraszám tábla ----------

    suspend fun getMonthHours(): Map<Int, MonthHoursEntity> = monthHoursDao.getAll().associateBy { it.month }

    suspend fun setMonthHours(month: Int, hours: Double, days: Int) {
        monthHoursDao.upsertAll(listOf(MonthHoursEntity(month, hours, days)))
    }

    // ---------- Ünnepnapok ----------

    suspend fun getHolidayMap(year: Int): Map<String, String> {
        val auto = HungarianHolidays.defaultHolidays(year).toMutableMap()
        holidayDao.getRemovedForYear(year).forEach { auto.remove(it) }
        holidayDao.getExtraForYear(year).forEach { auto[it.date] = it.name }
        return auto
    }

    suspend fun addExtraHoliday(year: Int, date: String, name: String) =
        holidayDao.insertExtra(HolidayExtraEntity(year = year, date = date, name = name))

    suspend fun deleteExtraHoliday(year: Int, date: String) = holidayDao.deleteExtra(year, date)

    suspend fun setHolidayRemoved(year: Int, date: String, removed: Boolean) {
        if (removed) holidayDao.insertRemoved(HolidayRemovedEntity(year = year, date = date))
        else holidayDao.deleteRemoved(year, date)
    }

    suspend fun getRemovedHolidays(year: Int): Set<String> = holidayDao.getRemovedForYear(year).toSet()
    suspend fun getExtraHolidays(year: Int): List<HolidayExtraEntity> = holidayDao.getExtraForYear(year)

    // ---------- Beosztás ----------

    suspend fun getMonthCodes(employeeId: Long, year: Int, month: Int): Map<Int, String> =
        scheduleDao.getForEmployeeMonth(employeeId, year, month).associate { it.day to it.code }

    /** employeeId -> (day -> code), egy adott hónapra, az összes dolgozóra (lefedettség-számításhoz). */
    suspend fun getMonthCodesForAll(year: Int, month: Int): Map<Long, Map<Int, String>> =
        scheduleDao.getForMonth(year, month).groupBy { it.employeeId }
            .mapValues { (_, list) -> list.associate { it.day to it.code } }

    suspend fun setCell(employeeId: Long, year: Int, month: Int, day: Int, code: String) {
        if (code.isEmpty()) scheduleDao.deleteCell(employeeId, year, month, day)
        else scheduleDao.upsert(ScheduleEntryEntity(employeeId = employeeId, year = year, month = month, day = day, code = code))
    }

    suspend fun getCarryIn(employeeId: Long, year: Int, month: Int): Double =
        carryOverDao.getForEmployeeMonth(employeeId, year, month)?.hours ?: 0.0

    suspend fun setCarryIn(employeeId: Long, year: Int, month: Int, hours: Double) {
        carryOverDao.upsert(CarryOverEntity(employeeId = employeeId, year = year, month = month, hours = hours))
    }

    suspend fun yearVacationUsed(employeeId: Long, year: Int): Int =
        scheduleDao.getForEmployeeYear(employeeId, year).count { it.code == ScheduleCalculator.CODE_VACATION }

    /**
     * Hirtelen beteg szabadság esetén automatikus helyettes-keresés: az adott napon szabad
     * (aznapra még be nem osztott) csoporttagok közül azt választja, akinek eddig a legkevesebb
     * ledolgozott órája van ebben a hónapban, és őt állítja be a beteg dolgozó műszakjára.
     * @return a kiválasztott helyettesítő, vagy null, ha nincs elérhető szabad dolgozó.
     */
    suspend fun findSickSubstitute(
        group: WorkGroupEntity,
        employees: List<EmployeeEntity>,
        year: Int,
        month: Int,
        day: Int,
        sickEmployeeId: Long,
        shiftCode: String
    ): EmployeeEntity? {
        val holidayMap = getHolidayMap(year)
        val baseHours = getMonthHours()[month]?.hours ?: 0.0

        var best: EmployeeEntity? = null
        var bestHours = Double.MAX_VALUE
        for (emp in employees) {
            if (emp.id == sickEmployeeId) continue
            if (emp.excludedShiftCodeList().contains(shiftCode)) continue // kérésre nem osztható be ebbe a műszaktípusba
            val codes = getMonthCodes(emp.id, year, month)
            if (!codes[day].isNullOrEmpty()) continue // aznap már be van osztva valamire

            val carryIn = getCarryIn(emp.id, year, month)
            val summary = ScheduleCalculator.summarizeMonth(
                group.type, group.dailyHours, shiftTypeDao.getForGroup(group.id), emp.employmentFactor,
                baseHours, codes, holidayMap, carryIn, year, month
            )
            if (summary.actualHours < bestHours) {
                bestHours = summary.actualHours
                best = emp
            }
        }
        if (best != null) setCell(best.id, year, month, day, shiftCode)
        return best
    }

    /** Egy hiányzó lefedettség egy adott napon/műszakon, automatikus kitöltés után. */
    data class AutoFillShortfall(val groupName: String, val day: Int, val shiftLabel: String, val needed: Int, val assigned: Int)

    /** Az automatikus kitöltés eredménye. */
    data class AutoFillResult(val filledCells: Int, val restCellsMarked: Int, val shortfalls: List<AutoFillShortfall>)

    /**
     * Automatikus beosztás-kitöltő. Csak ÜRES cellákba ír - meglévő (kézzel beírt vagy korábban
     * generált) kódokat sosem ír felül.
     * - Irodai (hétfő-péntek) csoportoknál: minden munkanapon minden dolgozóhoz "M" kódot ír.
     * - Egymást váltó, létszám-figyelt (staffPerShift > 0) csoportoknál: napról napra,
     *   műszaktípusonként annyi szabad, a csoport `minRestHours` pihenőidejét betartó, a
     *   kérésre kizárt műszaktípusokat (excludedShiftCodes) figyelmen kívül hagyó dolgozót
     *   jelöl ki, ameddig a szükséges létszám meg nem telik - a legkevesebb eddig ledolgozott
     *   órájú (méltányos terheléselosztás), egyformaság esetén névsor szerinti dolgozókat
     *   részesítve előnyben. Ha nincs elég szabad/pihent dolgozó, annyit oszt be, amennyi van,
     *   és a hiányt jelzi.
     * - Hónapváltás: ha egy dolgozó az előző hónap utolsó napjaiban dolgozott (a csoport
     *   műszaktípusai közül valamelyiket), a pihenőidőt a hónaphatáron át is figyelembe veszi,
     *   és a hónap elején még szükséges pihenőnapokat explicit "P" kóddal jelöli.
     */
    suspend fun autoFillMonth(year: Int, month: Int): AutoFillResult {
        val dim = ScheduleCalculator.daysInMonth(year, month)
        val holidayMap = getHolidayMap(year)
        var filledCells = 0
        var restCellsMarked = 0
        val shortfalls = mutableListOf<AutoFillShortfall>()

        var prevYear = year
        var prevMonth = month - 1
        if (prevMonth < 1) { prevMonth = 12; prevYear -= 1 }
        val prevDim = ScheduleCalculator.daysInMonth(prevYear, prevMonth)

        for (gws in getGroupsWithShiftTypes()) {
            val group = gws.group
            val groupEmployees = getEmployeesForGroup(group.id)
            if (groupEmployees.isEmpty()) continue

            if (group.type == GROUP_TYPE_OFFICE) {
                for (emp in groupEmployees) {
                    val codes = getMonthCodes(emp.id, year, month)
                    for (d in 1..dim) {
                        if (!ScheduleCalculator.isOfficeWorkday(holidayMap, year, month, d)) continue
                        if (!codes[d].isNullOrEmpty()) continue
                        setCell(emp.id, year, month, d, "M")
                        filledCells++
                    }
                }
                continue
            }

            if (group.staffPerShift > 0 && gws.shiftTypes.isNotEmpty()) {
                val minRestDays = Math.ceil(group.minRestHours / 24.0).toInt()
                val codesByEmployee = groupEmployees.associate { it.id to getMonthCodes(it.id, year, month).toMutableMap() }
                val prevCodesByEmployee = groupEmployees.associate { it.id to getMonthCodes(it.id, prevYear, prevMonth) }
                val lastWorkedDay = HashMap<Long, Int>()
                val shiftCount = HashMap<Long, Int>()
                groupEmployees.forEach { emp ->
                    lastWorkedDay[emp.id] = Int.MIN_VALUE
                    shiftCount[emp.id] = 0

                    // Hónaphatáron átnyúló pihenőidő: az előző hónap utolsó, ebben a csoportban
                    // ledolgozott napja "0" (utolsó nap), "-1" (utolsó előtti) stb. relatív napot kap.
                    val prevCodes = prevCodesByEmployee.getValue(emp.id)
                    for (pd in 1..prevDim) {
                        val prevCode = prevCodes[pd]
                        if (prevCode != null && gws.shiftTypes.any { it.code == prevCode }) {
                            lastWorkedDay[emp.id] = pd - prevDim
                        }
                    }

                    val codes = codesByEmployee.getValue(emp.id)
                    for (d in 1..dim) {
                        val code = codes[d]
                        if (code != null && gws.shiftTypes.any { it.code == code }) {
                            lastWorkedDay[emp.id] = d
                            shiftCount[emp.id] = shiftCount.getValue(emp.id) + 1
                        }
                    }
                }

                // Az előző havi utolsó műszak miatt még kötelező pihenőnapokat explicit "P" kóddal jelöljük.
                groupEmployees.forEach { emp ->
                    val last = lastWorkedDay.getValue(emp.id)
                    if (last == Int.MIN_VALUE || last > 0) return@forEach
                    val codes = codesByEmployee.getValue(emp.id)
                    var d = 1
                    while (d <= dim && (d - last) <= minRestDays) {
                        if (codes[d].isNullOrEmpty()) {
                            setCell(emp.id, year, month, d, "P")
                            codes[d] = "P"
                            restCellsMarked++
                            filledCells++
                        }
                        d++
                    }
                }

                for (d in 1..dim) {
                    val assignedToday = HashSet<Long>()
                    groupEmployees.forEach { emp ->
                        if (!codesByEmployee.getValue(emp.id)[d].isNullOrEmpty()) assignedToday.add(emp.id)
                    }

                    for (st in gws.shiftTypes) {
                        val existingCount = groupEmployees.count { codesByEmployee.getValue(it.id)[d] == st.code }
                        val needed = (group.staffPerShift - existingCount).coerceAtLeast(0)
                        if (needed == 0) continue

                        val candidates = groupEmployees.filter { emp ->
                            if (emp.excludedShiftCodeList().contains(st.code)) return@filter false
                            if (assignedToday.contains(emp.id)) return@filter false
                            if (!codesByEmployee.getValue(emp.id)[d].isNullOrEmpty()) return@filter false
                            val last = lastWorkedDay.getValue(emp.id)
                            last == Int.MIN_VALUE || (d - last) > minRestDays
                        }.sortedWith(compareBy({ shiftCount.getValue(it.id) }, { it.name }))

                        val toAssign = candidates.take(needed)
                        toAssign.forEach { emp ->
                            setCell(emp.id, year, month, d, st.code)
                            codesByEmployee.getValue(emp.id)[d] = st.code
                            assignedToday.add(emp.id)
                            lastWorkedDay[emp.id] = d
                            shiftCount[emp.id] = shiftCount.getValue(emp.id) + 1
                            filledCells++
                        }

                        if (toAssign.size < needed) {
                            shortfalls.add(
                                AutoFillShortfall(group.name, d, "${st.label} (${st.code})", group.staffPerShift, existingCount + toAssign.size)
                            )
                        }
                    }
                }
            }
        }

        return AutoFillResult(filledCells, restCellsMarked, shortfalls)
    }

    // ---------- JSON export / import (biztonsági mentés, hordozhatóság) ----------
    // A tényleges "adatbázis" maga a Room által kezelt, titkosítatlan SQLite fájl
    // (lásd AppDatabase.databaseFile) - ez a JSON csak kényelmi export/import formátum.

    suspend fun exportToJson(): String {
        val root = JSONObject()
        root.put("version", 1)

        val groupsArr = JSONArray()
        getGroupsWithShiftTypes().forEach { gws ->
            val g = gws.group
            val jg = JSONObject()
                .put("id", g.id).put("name", g.name).put("type", g.type)
                .put("dailyHours", g.dailyHours).put("staffPerShift", g.staffPerShift)
                .put("minRestHours", g.minRestHours)
            val stArr = JSONArray()
            gws.shiftTypes.forEach { st ->
                stArr.put(JSONObject().put("code", st.code).put("label", st.label).put("hours", st.hours))
            }
            jg.put("shiftTypes", stArr)
            groupsArr.put(jg)
        }
        root.put("groups", groupsArr)

        val empArr = JSONArray()
        getEmployees().forEach { e ->
            empArr.put(
                JSONObject().put("id", e.id).put("name", e.name).put("groupId", e.groupId)
                    .put("employmentFactor", e.employmentFactor).put("maxVacationDays", e.maxVacationDays)
                    .put("notes", e.notes).put("excludedShiftCodes", e.excludedShiftCodes)
            )
        }
        root.put("employees", empArr)

        val monthHoursArr = JSONArray()
        getMonthHours().values.forEach { mh ->
            monthHoursArr.put(JSONObject().put("month", mh.month).put("hours", mh.hours).put("days", mh.days))
        }
        root.put("monthHours", monthHoursArr)

        val scheduleArr = JSONArray()
        scheduleDao.getAll().forEach { s ->
            scheduleArr.put(
                JSONObject().put("employeeId", s.employeeId).put("year", s.year)
                    .put("month", s.month).put("day", s.day).put("code", s.code)
            )
        }
        root.put("schedule", scheduleArr)

        val carryArr = JSONArray()
        carryOverDao.getAll().forEach { c ->
            carryArr.put(
                JSONObject().put("employeeId", c.employeeId).put("year", c.year)
                    .put("month", c.month).put("hours", c.hours)
            )
        }
        root.put("carryOvers", carryArr)

        val extraArr = JSONArray()
        holidayDao.getAllExtra().forEach { h ->
            extraArr.put(JSONObject().put("year", h.year).put("date", h.date).put("name", h.name))
        }
        root.put("holidayExtra", extraArr)

        val removedArr = JSONArray()
        holidayDao.getAllRemoved().forEach { h ->
            removedArr.put(JSONObject().put("year", h.year).put("date", h.date))
        }
        root.put("holidayRemoved", removedArr)

        return root.toString(2)
    }

    /**
     * Teljesen felülírja az adatbázist a JSON tartalmával.
     * @return null ha sikerült, egyébként a hibaüzenet.
     */
    suspend fun importFromJson(json: String): String? {
        return try {
            val root = JSONObject(json)
            if (!root.has("groups") || !root.has("employees")) {
                return "A fájl formátuma nem megfelelő (hiányzó munkacsoport/dolgozó lista)."
            }
            db.withTransaction {
                db.clearAllTables()

                // régi (JSON-beli) csoport-id -> új, adatbázisban generált id
                val groupIdMap = HashMap<Long, Long>()
                val groupsArr = root.getJSONArray("groups")
                for (i in 0 until groupsArr.length()) {
                    val jg = groupsArr.getJSONObject(i)
                    val oldId = jg.getLong("id")
                    val newId = groupDao.insert(
                        WorkGroupEntity(
                            name = jg.getString("name"),
                            type = jg.getString("type"),
                            dailyHours = jg.getDouble("dailyHours"),
                            staffPerShift = jg.optInt("staffPerShift", 0),
                            minRestHours = jg.optInt("minRestHours", 24)
                        )
                    )
                    groupIdMap[oldId] = newId
                    val stArr = jg.getJSONArray("shiftTypes")
                    val shiftTypes = (0 until stArr.length()).map { j ->
                        val jst = stArr.getJSONObject(j)
                        ShiftTypeEntity(groupId = newId, code = jst.getString("code"), label = jst.getString("label"), hours = jst.getDouble("hours"))
                    }
                    if (shiftTypes.isNotEmpty()) shiftTypeDao.insertAll(shiftTypes)
                }

                val employeeIdMap = HashMap<Long, Long>()
                val empArr = root.getJSONArray("employees")
                for (i in 0 until empArr.length()) {
                    val je = empArr.getJSONObject(i)
                    val oldId = je.getLong("id")
                    val oldGroupId = je.getLong("groupId")
                    val newGroupId = groupIdMap[oldGroupId] ?: continue
                    val newId = employeeDao.insert(
                        EmployeeEntity(
                            name = je.getString("name"),
                            groupId = newGroupId,
                            employmentFactor = je.optDouble("employmentFactor", 1.0),
                            maxVacationDays = je.getInt("maxVacationDays"),
                            notes = je.optString("notes", ""),
                            excludedShiftCodes = je.optString("excludedShiftCodes", "")
                        )
                    )
                    employeeIdMap[oldId] = newId
                }

                if (root.has("monthHours")) {
                    val mhArr = root.getJSONArray("monthHours")
                    val rows = (0 until mhArr.length()).map { i ->
                        val jm = mhArr.getJSONObject(i)
                        MonthHoursEntity(jm.getInt("month"), jm.getDouble("hours"), jm.getInt("days"))
                    }
                    if (rows.isNotEmpty()) monthHoursDao.upsertAll(rows) else resetMonthHoursToDefault()
                } else resetMonthHoursToDefault()

                if (root.has("schedule")) {
                    val schArr = root.getJSONArray("schedule")
                    for (i in 0 until schArr.length()) {
                        val js = schArr.getJSONObject(i)
                        val newEmpId = employeeIdMap[js.getLong("employeeId")] ?: continue
                        scheduleDao.upsert(
                            ScheduleEntryEntity(
                                employeeId = newEmpId, year = js.getInt("year"),
                                month = js.getInt("month"), day = js.getInt("day"), code = js.getString("code")
                            )
                        )
                    }
                }

                if (root.has("carryOvers")) {
                    val coArr = root.getJSONArray("carryOvers")
                    for (i in 0 until coArr.length()) {
                        val jc = coArr.getJSONObject(i)
                        val newEmpId = employeeIdMap[jc.getLong("employeeId")] ?: continue
                        carryOverDao.upsert(
                            CarryOverEntity(employeeId = newEmpId, year = jc.getInt("year"), month = jc.getInt("month"), hours = jc.getDouble("hours"))
                        )
                    }
                }

                if (root.has("holidayExtra")) {
                    val heArr = root.getJSONArray("holidayExtra")
                    for (i in 0 until heArr.length()) {
                        val jh = heArr.getJSONObject(i)
                        holidayDao.insertExtra(HolidayExtraEntity(year = jh.getInt("year"), date = jh.getString("date"), name = jh.getString("name")))
                    }
                }
                if (root.has("holidayRemoved")) {
                    val hrArr = root.getJSONArray("holidayRemoved")
                    for (i in 0 until hrArr.length()) {
                        val jh = hrArr.getJSONObject(i)
                        holidayDao.insertRemoved(HolidayRemovedEntity(year = jh.getInt("year"), date = jh.getString("date")))
                    }
                }

                if (groupDao.count() == 0) seedDefaultGroups()
            }
            null
        } catch (e: Exception) {
            "Hiba a betöltéskor: ${e.message}"
        }
    }
}
