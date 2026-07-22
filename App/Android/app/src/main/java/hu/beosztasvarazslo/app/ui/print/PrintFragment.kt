package hu.beosztasvarazslo.app.ui.print

import android.content.Intent
import android.os.Bundle
import android.view.LayoutInflater
import android.view.View
import android.view.ViewGroup
import android.widget.ArrayAdapter
import androidx.core.content.FileProvider
import androidx.fragment.app.Fragment
import androidx.lifecycle.lifecycleScope
import hu.beosztasvarazslo.app.data.AppRepository
import hu.beosztasvarazslo.app.databinding.FragmentPrintBinding
import hu.beosztasvarazslo.app.logic.GROUP_TYPE_OFFICE
import hu.beosztasvarazslo.app.logic.PrintDocument
import hu.beosztasvarazslo.app.logic.PrintRenderer
import hu.beosztasvarazslo.app.logic.PrintRow
import hu.beosztasvarazslo.app.logic.ScheduleCalculator
import hu.beosztasvarazslo.app.ui.common.repo
import hu.beosztasvarazslo.app.ui.common.toast
import kotlinx.coroutines.launch
import java.io.File
import java.text.SimpleDateFormat
import java.util.Calendar
import java.util.Date
import java.util.Locale

class PrintFragment : Fragment() {

    private var _binding: FragmentPrintBinding? = null
    private val binding get() = _binding!!
    private var lastExportedFile: File? = null
    private var lastExportedMime: String? = null

    override fun onCreateView(inflater: LayoutInflater, container: ViewGroup?, savedInstanceState: Bundle?): View {
        _binding = FragmentPrintBinding.inflate(inflater, container, false)
        return binding.root
    }

    override fun onViewCreated(view: View, savedInstanceState: Bundle?) {
        super.onViewCreated(view, savedInstanceState)
        val cal = Calendar.getInstance()
        binding.etPrintYear.setText(cal.get(Calendar.YEAR).toString())
        binding.spPrintMonth.adapter = ArrayAdapter(requireContext(), android.R.layout.simple_spinner_dropdown_item, AppRepository.MONTH_NAMES)
        binding.spPrintMonth.setSelection(cal.get(Calendar.MONTH))

        binding.btnExportPdf.setOnClickListener { export(asPdf = true) }
        binding.btnExportJpg.setOnClickListener { export(asPdf = false) }
        binding.btnShareLast.setOnClickListener { shareLast() }

        updateSummary()
        binding.etPrintYear.setOnFocusChangeListener { _, hasFocus -> if (!hasFocus) updateSummary() }
        binding.spPrintMonth.setOnItemSelectedListener(object : android.widget.AdapterView.OnItemSelectedListener {
            override fun onItemSelected(parent: android.widget.AdapterView<*>?, v: View?, position: Int, id: Long) = updateSummary()
            override fun onNothingSelected(parent: android.widget.AdapterView<*>?) {}
        })
    }

    private fun selectedYear() = binding.etPrintYear.text.toString().toIntOrNull() ?: Calendar.getInstance().get(Calendar.YEAR)
    private fun selectedMonth() = binding.spPrintMonth.selectedItemPosition + 1

    private fun updateSummary() {
        lifecycleScope.launch {
            val year = selectedYear(); val month = selectedMonth()
            val groups = repo().getGroupsWithShiftTypes()
            val employees = repo().getEmployees()
            val count = employees.count { emp -> groups.any { it.group.id == emp.groupId } }
            binding.tvPrintSummary.text =
                "Exportálandó: ${AppRepository.MONTH_NAMES[month - 1]} $year ($month. hónap), $count dolgozó, " +
                "${groups.count { g -> employees.any { it.groupId == g.group.id } }} munkacsoport."
        }
    }

    private suspend fun buildDocument(): PrintDocument {
        val year = selectedYear(); val month = selectedMonth()
        val groups = repo().getGroupsWithShiftTypes().sortedBy { it.group.name.lowercase(Locale.ROOT) }
        val employees = repo().getEmployees()
        val holidayMap = repo().getHolidayMap(year)
        val baseHours = repo().getMonthHours()[month]?.hours ?: 0.0
        val dim = ScheduleCalculator.daysInMonth(year, month)

        val dowLetters = arrayOf("V", "H", "K", "Sze", "Cs", "P", "Szo")
        val dayHeaders = (1..dim).map { d ->
            val dow = dowLetters[java.time.LocalDate.of(year, month, d).dayOfWeek.value % 7]
            "$d\n$dow"
        }

        val rows = mutableListOf<PrintRow>()
        for (gws in groups) {
            val groupEmployees = employees.filter { it.groupId == gws.group.id }.sortedBy { it.name.lowercase(Locale.ROOT) }
            if (groupEmployees.isEmpty()) continue
            rows.add(PrintRow(gws.group.name, isGroupHeader = true, dayTexts = emptyList(), balanceText = ""))
            for (emp in groupEmployees) {
                val codes = repo().getMonthCodes(emp.id, year, month)
                val carryIn = repo().getCarryIn(emp.id, year, month)
                val summary = ScheduleCalculator.summarizeMonth(
                    gws.group.type, gws.group.dailyHours, gws.shiftTypes, emp.employmentFactor, baseHours,
                    codes, holidayMap, carryIn, year, month
                )
                val dayTexts = (1..dim).map { d ->
                    if (gws.group.type == GROUP_TYPE_OFFICE) {
                        if (!ScheduleCalculator.isOfficeWorkday(holidayMap, year, month, d)) "·" else (codes[d] ?: "")
                    } else codes[d] ?: ""
                }
                rows.add(PrintRow(emp.name, isGroupHeader = false, dayTexts = dayTexts, balanceText = String.format(Locale.getDefault(), "%.1f", summary.balance)))
            }
        }

        return PrintDocument(
            title = "${AppRepository.MONTH_NAMES[month - 1]} $year – $month. hónap – havi munkabeosztás",
            dayHeaderLabels = dayHeaders,
            rows = rows
        )
    }

    private fun export(asPdf: Boolean) {
        lifecycleScope.launch {
            try {
                val doc = buildDocument()
                val year = selectedYear(); val month = selectedMonth()
                val stamp = SimpleDateFormat("yyyyMMdd_HHmm", Locale.getDefault()).format(Date())
                val baseName = "beosztas-${AppRepository.MONTH_NAMES[month - 1]}-$year-$stamp"
                val file = if (asPdf) {
                    PrintRenderer.generatePdf(requireContext(), doc, "$baseName.pdf")
                } else {
                    PrintRenderer.generateJpeg(requireContext(), doc, "$baseName.jpg")
                }
                lastExportedFile = file
                lastExportedMime = if (asPdf) "application/pdf" else "image/jpeg"
                toast((if (asPdf) "PDF" else "JPG") + " elmentve: ${file.name}")
            } catch (e: Exception) {
                toast("Hiba az export során: ${e.message}")
            }
        }
    }

    private fun shareLast() {
        val file = lastExportedFile
        val mime = lastExportedMime
        if (file == null || mime == null) {
            toast("Előbb készíts egy PDF vagy JPG exportot.")
            return
        }
        val uri = FileProvider.getUriForFile(requireContext(), "hu.beosztasvarazslo.app.fileprovider", file)
        val intent = Intent(Intent.ACTION_SEND).apply {
            type = mime
            putExtra(Intent.EXTRA_STREAM, uri)
            addFlags(Intent.FLAG_GRANT_READ_URI_PERMISSION)
        }
        startActivity(Intent.createChooser(intent, "Megosztás"))
    }

    override fun onDestroyView() {
        super.onDestroyView()
        _binding = null
    }
}
