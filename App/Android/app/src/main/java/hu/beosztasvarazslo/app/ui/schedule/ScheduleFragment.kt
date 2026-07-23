package hu.beosztasvarazslo.app.ui.schedule

import android.graphics.Color
import android.os.Bundle
import android.view.Gravity
import android.view.LayoutInflater
import android.view.View
import android.view.ViewGroup
import android.widget.Button
import android.widget.EditText
import android.widget.LinearLayout
import android.widget.TextView
import androidx.appcompat.app.AlertDialog
import androidx.core.content.ContextCompat
import androidx.fragment.app.Fragment
import androidx.lifecycle.lifecycleScope
import hu.beosztasvarazslo.app.R
import hu.beosztasvarazslo.app.data.AppRepository
import hu.beosztasvarazslo.app.data.EmployeeEntity
import hu.beosztasvarazslo.app.data.GroupWithShiftTypes
import hu.beosztasvarazslo.app.databinding.FragmentScheduleBinding
import hu.beosztasvarazslo.app.logic.GROUP_TYPE_OFFICE
import hu.beosztasvarazslo.app.logic.ScheduleCalculator
import hu.beosztasvarazslo.app.ui.common.repo
import hu.beosztasvarazslo.app.ui.common.toast
import kotlinx.coroutines.launch
import java.util.Calendar
import java.util.Locale

class ScheduleFragment : Fragment() {

    private var _binding: FragmentScheduleBinding? = null
    private val binding get() = _binding!!

    private var selYear = Calendar.getInstance().get(Calendar.YEAR)
    private var selMonth = Calendar.getInstance().get(Calendar.MONTH) + 1

    private lateinit var monthButtons: List<Button>

    private val dowLetters = arrayOf("V", "H", "K", "Sze", "Cs", "P", "Szo")

    override fun onCreateView(inflater: LayoutInflater, container: ViewGroup?, savedInstanceState: Bundle?): View {
        _binding = FragmentScheduleBinding.inflate(inflater, container, false)
        return binding.root
    }

    override fun onViewCreated(view: View, savedInstanceState: Bundle?) {
        super.onViewCreated(view, savedInstanceState)
        binding.etYear.setText(selYear.toString())

        monthButtons = listOf(
            binding.btnMonth1, binding.btnMonth2, binding.btnMonth3, binding.btnMonth4,
            binding.btnMonth5, binding.btnMonth6, binding.btnMonth7, binding.btnMonth8,
            binding.btnMonth9, binding.btnMonth10, binding.btnMonth11, binding.btnMonth12
        )
        monthButtons.forEachIndexed { idx, btn ->
            btn.setOnClickListener { selMonth = idx + 1; highlightMonthButtons(); rebuildGrid() }
        }
        binding.etYear.setOnFocusChangeListener { _, hasFocus ->
            if (!hasFocus) {
                selYear = binding.etYear.text.toString().toIntOrNull() ?: selYear
                rebuildGrid()
            }
        }
        binding.btnCopyPrevCarry.setOnClickListener { copyPreviousMonthCarry() }

        highlightMonthButtons()
        rebuildGrid()
    }

    override fun onResume() {
        super.onResume()
        rebuildGrid()
    }

    private fun highlightMonthButtons() {
        monthButtons.forEachIndexed { idx, btn ->
            val selected = (idx + 1) == selMonth
            btn.setTextColor(ContextCompat.getColor(requireContext(), if (selected) R.color.primary else R.color.text_muted))
        }
    }

    private fun dp(value: Int): Int = (value * resources.displayMetrics.density).toInt()

    private val nameColWidth get() = dp(150)
    private val dayColWidth get() = dp(34)
    private val statColWidth get() = dp(78)
    private val rowHeight get() = dp(40)

    private fun cellTextView(text: String, widthPx: Int, bgColorRes: Int? = null, bold: Boolean = false): TextView {
        val tv = TextView(requireContext())
        tv.text = text
        tv.gravity = Gravity.CENTER
        tv.textSize = 11f
        tv.setTextColor(ContextCompat.getColor(requireContext(), R.color.text_primary))
        if (bold) tv.setTypeface(tv.typeface, android.graphics.Typeface.BOLD)
        if (bgColorRes != null) tv.setBackgroundColor(ContextCompat.getColor(requireContext(), bgColorRes))
        tv.layoutParams = LinearLayout.LayoutParams(widthPx, rowHeight)
        return tv
    }

    private fun rebuildGrid() {
        val binding = _binding ?: return
        binding.tvScheduleTitle.text =
            "Beosztás – ${AppRepository.MONTH_NAMES[selMonth - 1]} $selYear ($selMonth. hónap)"

        lifecycleScope.launch {
            val groups = repo().getGroupsWithShiftTypes().sortedBy { it.group.name.lowercase(Locale.ROOT) }
            val employees = repo().getEmployees()
            val holidayMap = repo().getHolidayMap(selYear)
            val monthHours = repo().getMonthHours()[selMonth]
            val baseHours = monthHours?.hours ?: 0.0
            val dim = ScheduleCalculator.daysInMonth(selYear, selMonth)

            val b = _binding ?: return@launch
            b.namesColumn.removeAllViews()
            b.daysColumn.removeAllViews()

            val totalRowWidth = dim * dayColWidth + 5 * statColWidth

            // Fejléc sor
            b.namesColumn.addView(cellTextView("Dolgozó", nameColWidth, R.color.schedule_header_bg, bold = true).also { it.gravity = Gravity.CENTER_VERTICAL or Gravity.START })
            val headerRow = LinearLayout(requireContext()).apply { orientation = LinearLayout.HORIZONTAL }
            for (d in 1..dim) {
                val dow = dowLetters[java.time.LocalDate.of(selYear, selMonth, d).dayOfWeek.value % 7]
                headerRow.addView(cellTextView("$d\n$dow", dayColWidth, R.color.schedule_header_bg))
            }
            listOf("Köt.ó", "Ledolg.ó", "Szab.nap", "Bejövő ó.", "Egyenleg").forEach {
                headerRow.addView(cellTextView(it, statColWidth, R.color.schedule_header_bg, bold = true))
            }
            b.daysColumn.addView(headerRow)

            for (gws in groups) {
                val groupEmployees = employees.filter { it.groupId == gws.group.id }.sortedBy { it.name.lowercase(Locale.ROOT) }
                if (groupEmployees.isEmpty()) continue

                b.namesColumn.addView(cellTextView(gws.group.name, nameColWidth, R.color.warning, bold = true).also {
                    it.gravity = Gravity.CENTER_VERTICAL or Gravity.START
                    it.setTextColor(Color.WHITE)
                })
                val groupHeaderRow = LinearLayout(requireContext()).apply { orientation = LinearLayout.HORIZONTAL }
                groupHeaderRow.addView(TextView(requireContext()).apply {
                    text = "  " + gws.group.name
                    gravity = Gravity.CENTER_VERTICAL or Gravity.START
                    setTextColor(Color.WHITE)
                    setTypeface(typeface, android.graphics.Typeface.BOLD)
                    layoutParams = LinearLayout.LayoutParams(totalRowWidth, rowHeight)
                    setBackgroundColor(ContextCompat.getColor(requireContext(), R.color.warning))
                })
                b.daysColumn.addView(groupHeaderRow)

                for (emp in groupEmployees) {
                    addEmployeeRow(b, emp, gws, holidayMap, baseHours, dim)
                }
            }

            rebuildCoverage(groups, employees)
        }
    }

    private suspend fun addEmployeeRow(
        b: FragmentScheduleBinding,
        emp: EmployeeEntity,
        gws: GroupWithShiftTypes,
        holidayMap: Map<String, String>,
        baseHours: Double,
        dim: Int
    ) {
        val codes = repo().getMonthCodes(emp.id, selYear, selMonth)
        val carryIn = repo().getCarryIn(emp.id, selYear, selMonth)
        val summary = ScheduleCalculator.summarizeMonth(
            gws.group.type, gws.group.dailyHours, gws.shiftTypes, emp.employmentFactor, baseHours,
            codes, holidayMap, carryIn, selYear, selMonth
        )

        b.namesColumn.addView(cellTextView(emp.name, nameColWidth).also { it.gravity = Gravity.CENTER_VERTICAL or Gravity.START })

        val row = LinearLayout(requireContext()).apply { orientation = LinearLayout.HORIZONTAL }
        for (d in 1..dim) {
            val isOfficeWorkday = ScheduleCalculator.isOfficeWorkday(holidayMap, selYear, selMonth, d)
            val isWeekend = ScheduleCalculator.isWeekend(selYear, selMonth, d)
            val isHoliday = ScheduleCalculator.isHoliday(holidayMap, selYear, selMonth, d)

            if (gws.group.type == GROUP_TYPE_OFFICE && !isOfficeWorkday) {
                val bg = if (isHoliday) R.color.holiday_bg else R.color.weekend_bg
                row.addView(cellTextView(if (isHoliday) "Ü" else "·", dayColWidth, bg))
                continue
            }
            val code = codes[d] ?: ""
            val bg = when (code) {
                ScheduleCalculator.CODE_VACATION -> R.color.vacation_bg
                ScheduleCalculator.CODE_SICK -> R.color.sick_bg
                ScheduleCalculator.CODE_ABSENCE -> R.color.absence_bg
                ScheduleCalculator.CODE_REST -> R.color.rest_bg
                else -> if (isHoliday) R.color.holiday_bg else if (isWeekend) R.color.weekend_bg else null
            }
            val cell = cellTextView(code, dayColWidth, bg)
            cell.setOnClickListener { showCellPicker(emp, gws, d, code) }
            row.addView(cell)
        }
        row.addView(cellTextView(fmt(summary.requiredFull), statColWidth))
        row.addView(cellTextView(fmt(summary.actualHours), statColWidth))
        row.addView(cellTextView(summary.vacationDays.toString(), statColWidth))

        val carryEdit = EditText(requireContext())
        carryEdit.setText(fmt(carryIn))
        carryEdit.textSize = 11f
        carryEdit.gravity = Gravity.CENTER
        carryEdit.layoutParams = LinearLayout.LayoutParams(statColWidth, rowHeight)
        carryEdit.setOnFocusChangeListener { _, hasFocus ->
            if (!hasFocus) {
                val newVal = carryEdit.text.toString().toDoubleOrNull() ?: 0.0
                lifecycleScope.launch {
                    repo().setCarryIn(emp.id, selYear, selMonth, newVal)
                    rebuildGrid()
                }
            }
        }
        row.addView(carryEdit)

        val balanceColorRes = if (summary.balance < 0) R.color.danger else R.color.success
        row.addView(cellTextView(fmt(summary.balance), statColWidth, bold = true).also {
            it.setTextColor(ContextCompat.getColor(requireContext(), balanceColorRes))
        })

        b.daysColumn.addView(row)
    }

    private fun fmt(d: Double): String = String.format(Locale.getDefault(), "%.1f", d)

    private fun showCellPicker(employee: EmployeeEntity, gws: GroupWithShiftTypes, day: Int, oldCode: String) {
        val options = ScheduleCalculator.cellOptions(gws.group.type, gws.shiftTypes)
        val labels = options.map { it.second }.toTypedArray()
        AlertDialog.Builder(requireContext())
            .setTitle("${employee.name} – $day. nap")
            .setItems(labels) { _, which ->
                val newCode = options[which].first
                lifecycleScope.launch {
                    if (newCode == ScheduleCalculator.CODE_VACATION && oldCode != ScheduleCalculator.CODE_VACATION) {
                        val used = repo().yearVacationUsed(employee.id, selYear)
                        if (used + 1 > employee.maxVacationDays) {
                            toast("${employee.name} már elérte a max. kiadható szabadság napok számát (${employee.maxVacationDays} nap, $selYear).")
                            return@launch
                        }
                    }
                    repo().setCell(employee.id, selYear, selMonth, day, newCode)

                    // Hirtelen beteg szabadság: ha egymást váltó (staffPerShift > 0) csoportban
                    // egy munkanap beteg szabadságra vált, automatikusan keresünk rá helyettest.
                    val wasWorkingShift = gws.shiftTypes.any { it.code == oldCode }
                    if (newCode == ScheduleCalculator.CODE_SICK && oldCode != ScheduleCalculator.CODE_SICK &&
                        gws.group.staffPerShift > 0 && wasWorkingShift
                    ) {
                        val groupEmployees = repo().getEmployeesForGroup(gws.group.id)
                        val substitute = repo().findSickSubstitute(gws.group, groupEmployees, selYear, selMonth, day, employee.id, oldCode)
                        if (substitute != null) {
                            toast("${employee.name} beteg szabadságra került ($day. nap). Automatikus helyettes: ${substitute.name} ($oldCode műszak).")
                        } else {
                            toast("${employee.name} beteg szabadságra került ($day. nap), de nincs elérhető szabad helyettes - a műszak létszáma emiatt a szükséges alá csökkenhet!")
                        }
                    }
                    rebuildGrid()
                }
            }
            .setNegativeButton("Mégse", null)
            .show()
    }

    private fun copyPreviousMonthCarry() {
        var prevYear = selYear
        var prevMonth = selMonth - 1
        if (prevMonth < 1) { prevMonth = 12; prevYear -= 1 }
        val py = prevYear; val pm = prevMonth
        lifecycleScope.launch {
            val groups = repo().getGroupsWithShiftTypes()
            val employees = repo().getEmployees()
            val holidayMap = repo().getHolidayMap(py)
            val baseHours = repo().getMonthHours()[pm]?.hours ?: 0.0
            employees.forEach { emp ->
                val gws = groups.find { it.group.id == emp.groupId } ?: return@forEach
                val codes = repo().getMonthCodes(emp.id, py, pm)
                val carryIn = repo().getCarryIn(emp.id, py, pm)
                val summary = ScheduleCalculator.summarizeMonth(
                    gws.group.type, gws.group.dailyHours, gws.shiftTypes, emp.employmentFactor, baseHours,
                    codes, holidayMap, carryIn, py, pm
                )
                repo().setCarryIn(emp.id, selYear, selMonth, Math.round(summary.balance * 100.0) / 100.0)
            }
            rebuildGrid()
            toast("Előző havi (${AppRepository.MONTH_NAMES[pm - 1]} $py) egyenleg átmásolva bejövő óraként.")
        }
    }

    private fun rebuildCoverage(groups: List<GroupWithShiftTypes>, employees: List<EmployeeEntity>) {
        val b = _binding ?: return
        b.coverageContainer.removeAllViews()
        val relevant = groups.filter { it.group.staffPerShift > 0 && it.shiftTypes.isNotEmpty() }
        if (relevant.isEmpty()) {
            val tv = TextView(requireContext())
            tv.text = "Nincs olyan munkacsoport, amelyhez létszám-elvárás lenne beállítva (Csoportok fül)."
            tv.setTextColor(ContextCompat.getColor(requireContext(), R.color.text_muted))
            tv.textSize = 12f
            b.coverageContainer.addView(tv)
            return
        }
        lifecycleScope.launch {
            val dim = ScheduleCalculator.daysInMonth(selYear, selMonth)
            val codesForAll = repo().getMonthCodesForAll(selYear, selMonth)

            relevant.forEach { gws ->
                val title = TextView(requireContext())
                title.text = "${gws.group.name} (elvárt létszám/műszak: ${gws.group.staffPerShift} fő)"
                title.setTypeface(title.typeface, android.graphics.Typeface.BOLD)
                title.setPadding(0, dp(8), 0, dp(4))
                b.coverageContainer.addView(title)

                val groupEmployeeIds = employees.filter { it.groupId == gws.group.id }.map { it.id }

                val outer = LinearLayout(requireContext()).apply { orientation = LinearLayout.HORIZONTAL }
                val leftCol = LinearLayout(requireContext()).apply { orientation = LinearLayout.VERTICAL }
                val rightCol = LinearLayout(requireContext()).apply { orientation = LinearLayout.VERTICAL }

                leftCol.addView(cellTextView("Műszak", nameColWidth, R.color.schedule_header_bg, bold = true).also { it.gravity = Gravity.CENTER_VERTICAL or Gravity.START })
                val headerRow = LinearLayout(requireContext()).apply { orientation = LinearLayout.HORIZONTAL }
                for (d in 1..dim) headerRow.addView(cellTextView(d.toString(), dayColWidth, R.color.schedule_header_bg))
                rightCol.addView(headerRow)

                gws.shiftTypes.forEach { st ->
                    leftCol.addView(cellTextView("${st.label} (${st.code})", nameColWidth).also { it.gravity = Gravity.CENTER_VERTICAL or Gravity.START })
                    val row = LinearLayout(requireContext()).apply { orientation = LinearLayout.HORIZONTAL }
                    for (d in 1..dim) {
                        val codesThatDay = groupEmployeeIds.map { empId -> codesForAll[empId]?.get(d) }
                        val counts = ScheduleCalculator.shiftCoverage(gws.shiftTypes, codesThatDay)
                        val n = counts[st.code] ?: 0
                        val colorRes = when {
                            n < gws.group.staffPerShift -> R.color.danger
                            n > gws.group.staffPerShift -> R.color.warning
                            else -> R.color.success
                        }
                        row.addView(cellTextView(n.toString(), dayColWidth).also {
                            it.setTextColor(ContextCompat.getColor(requireContext(), colorRes))
                            it.setTypeface(it.typeface, android.graphics.Typeface.BOLD)
                        })
                    }
                    rightCol.addView(row)
                }

                val hscroll = android.widget.HorizontalScrollView(requireContext())
                hscroll.layoutParams = LinearLayout.LayoutParams(0, ViewGroup.LayoutParams.WRAP_CONTENT, 1f)
                hscroll.addView(rightCol)

                outer.addView(leftCol)
                outer.addView(hscroll)
                b.coverageContainer.addView(outer)
            }
        }
    }

    override fun onDestroyView() {
        super.onDestroyView()
        _binding = null
    }
}
