package hu.beosztasvarazslo.app.data

/** Munkacsoport a hozzá tartozó műszaktípusokkal együtt. */
data class GroupWithShiftTypes(
    val group: WorkGroupEntity,
    val shiftTypes: List<ShiftTypeEntity>
)
