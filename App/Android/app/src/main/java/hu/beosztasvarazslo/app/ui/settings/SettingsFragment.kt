package hu.beosztasvarazslo.app.ui.settings

import android.app.DatePickerDialog
import android.os.Bundle
import android.view.LayoutInflater
import android.view.View
import android.view.ViewGroup
import androidx.fragment.app.Fragment
import androidx.lifecycle.lifecycleScope
import androidx.recyclerview.widget.LinearLayoutManager
import hu.beosztasvarazslo.app.data.AppRepository
import hu.beosztasvarazslo.app.databinding.FragmentSettingsBinding
import hu.beosztasvarazslo.app.databinding.ItemMonthHourRowBinding
import hu.beosztasvarazslo.app.logic.HungarianHolidays
import hu.beosztasvarazslo.app.ui.common.confirmDialog
import hu.beosztasvarazslo.app.ui.common.repo
import hu.beosztasvarazslo.app.ui.common.toast
import kotlinx.coroutines.launch
import java.util.Calendar

class SettingsFragment : Fragment() {

    private var _binding: FragmentSettingsBinding? = null
    private val binding get() = _binding!!

    private val monthRowBindings = mutableListOf<ItemMonthHourRowBinding>()
    private lateinit var holidayAdapter: HolidayAdapter
    private var pickedHolidayDate: String? = null

    override fun onCreateView(inflater: LayoutInflater, container: ViewGroup?, savedInstanceState: Bundle?): View {
        _binding = FragmentSettingsBinding.inflate(inflater, container, false)
        return binding.root
    }

    override fun onViewCreated(view: View, savedInstanceState: Bundle?) {
        super.onViewCreated(view, savedInstanceState)

        binding.etHolidayYear.setText(Calendar.getInstance().get(Calendar.YEAR).toString())

        holidayAdapter = HolidayAdapter(
            onToggleAuto = { row ->
                lifecycleScope.launch {
                    val year = currentHolidayYear()
                    repo().setHolidayRemoved(year, row.date, !row.isRemoved)
                    loadHolidays()
                }
            },
            onDeleteExtra = { row ->
                lifecycleScope.launch {
                    repo().deleteExtraHoliday(currentHolidayYear(), row.date)
                    loadHolidays()
                }
            }
        )
        binding.holidayRecyclerView.layoutManager = LinearLayoutManager(requireContext())
        binding.holidayRecyclerView.adapter = holidayAdapter

        binding.btnResetMonthHours.setOnClickListener {
            requireContext().confirmDialog(message = "Visszaállítod az alapértelmezett havi óraszám-táblát?") {
                lifecycleScope.launch {
                    repo().resetMonthHoursToDefault()
                    loadMonthHours()
                    toast("Alapértékek visszaállítva.")
                }
            }
        }

        binding.etHolidayYear.setOnFocusChangeListener { _, hasFocus -> if (!hasFocus) loadHolidays() }

        binding.btnPickHolidayDate.setOnClickListener { showDatePicker() }

        binding.btnAddHoliday.setOnClickListener {
            val date = pickedHolidayDate
            val name = binding.etHolidayName.text.toString().trim()
            if (date == null || name.isEmpty()) {
                toast("Add meg a dátumot és a nevet is.")
                return@setOnClickListener
            }
            lifecycleScope.launch {
                repo().addExtraHoliday(currentHolidayYear(), date, name)
                binding.etHolidayName.setText("")
                pickedHolidayDate = null
                binding.btnPickHolidayDate.text = "Dátum"
                loadHolidays()
                toast("Ünnepnap hozzáadva.")
            }
        }

        buildMonthHourRows()
        loadMonthHours()
        loadHolidays()
    }

    private fun currentHolidayYear(): Int =
        binding.etHolidayYear.text.toString().toIntOrNull() ?: Calendar.getInstance().get(Calendar.YEAR)

    private fun showDatePicker() {
        val cal = Calendar.getInstance()
        DatePickerDialog(requireContext(), { _, year, month, day ->
            val iso = "%04d-%02d-%02d".format(year, month + 1, day)
            pickedHolidayDate = iso
            binding.btnPickHolidayDate.text = iso
        }, cal.get(Calendar.YEAR), cal.get(Calendar.MONTH), cal.get(Calendar.DAY_OF_MONTH)).show()
    }

    private fun buildMonthHourRows() {
        binding.monthHoursContainer.removeAllViews()
        monthRowBindings.clear()
        for (m in 1..12) {
            val rowBinding = ItemMonthHourRowBinding.inflate(layoutInflater, binding.monthHoursContainer, false)
            rowBinding.tvMonthName.text = AppRepository.MONTH_NAMES[m - 1]
            binding.monthHoursContainer.addView(rowBinding.root)
            monthRowBindings.add(rowBinding)

            rowBinding.etHours.setOnFocusChangeListener { _, hasFocus -> if (!hasFocus) saveMonthRow(m, rowBinding) }
            rowBinding.etDays.setOnFocusChangeListener { _, hasFocus -> if (!hasFocus) saveMonthRow(m, rowBinding) }
        }
    }

    private fun saveMonthRow(month: Int, rowBinding: ItemMonthHourRowBinding) {
        val hours = rowBinding.etHours.text.toString().toDoubleOrNull() ?: return
        val days = rowBinding.etDays.text.toString().toIntOrNull() ?: return
        lifecycleScope.launch { repo().setMonthHours(month, hours, days) }
    }

    private fun loadMonthHours() {
        lifecycleScope.launch {
            val data = repo().getMonthHours()
            for (m in 1..12) {
                val rec = data[m] ?: continue
                val rb = monthRowBindings[m - 1]
                val hoursText = if (rec.hours == rec.hours.toLong().toDouble()) rec.hours.toLong().toString() else rec.hours.toString()
                rb.etHours.setText(hoursText)
                rb.etDays.setText(rec.days.toString())
            }
        }
    }

    private fun loadHolidays() {
        lifecycleScope.launch {
            val year = currentHolidayYear()
            val auto = HungarianHolidays.defaultHolidays(year)
            val removed = repo().getRemovedHolidays(year)
            val extra = repo().getExtraHolidays(year)
            val rows = mutableListOf<HolidayRow>()
            auto.toSortedMap().forEach { (date, name) -> rows.add(HolidayRow(date, name, isAuto = true, isRemoved = removed.contains(date))) }
            extra.sortedBy { it.date }.forEach { rows.add(HolidayRow(it.date, it.name, isAuto = false, isRemoved = false)) }
            holidayAdapter.submit(rows.sortedBy { it.date })
        }
    }

    override fun onDestroyView() {
        super.onDestroyView()
        _binding = null
    }
}
