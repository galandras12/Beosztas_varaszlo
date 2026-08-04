package hu.beosztasvarazslo.app.data

/** Munkacsoport a hozzá tartozó műszaktípusokkal együtt. */
data class GroupWithShiftTypes(
    val group: WorkGroupEntity,
    val shiftTypes: List<ShiftTypeEntity>
)

/** A dolgozó kérésre kizárt műszaktípus-kódjai listaként (lásd EmployeeEntity.excludedShiftCodes). */
fun EmployeeEntity.excludedShiftCodeList(): List<String> =
    excludedShiftCodes.split(",").map { it.trim() }.filter { it.isNotEmpty() }

fun List<String>.toExcludedShiftCodesString(): String = joinToString(",")
