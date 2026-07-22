package hu.beosztasvarazslo.app.ui.employees

import android.os.Bundle
import android.view.LayoutInflater
import android.view.View
import android.view.ViewGroup
import android.widget.ArrayAdapter
import androidx.appcompat.app.AlertDialog
import androidx.fragment.app.Fragment
import androidx.lifecycle.lifecycleScope
import androidx.recyclerview.widget.LinearLayoutManager
import hu.beosztasvarazslo.app.data.EmployeeEntity
import hu.beosztasvarazslo.app.data.GroupWithShiftTypes
import hu.beosztasvarazslo.app.databinding.DialogEmployeeEditBinding
import hu.beosztasvarazslo.app.databinding.FragmentEmployeesBinding
import hu.beosztasvarazslo.app.ui.common.confirmDialog
import hu.beosztasvarazslo.app.ui.common.repo
import hu.beosztasvarazslo.app.ui.common.toast
import kotlinx.coroutines.launch
import java.util.Calendar

class EmployeesFragment : Fragment() {

    private var _binding: FragmentEmployeesBinding? = null
    private val binding get() = _binding!!
    private lateinit var adapter: EmployeeAdapter

    override fun onCreateView(inflater: LayoutInflater, container: ViewGroup?, savedInstanceState: Bundle?): View {
        _binding = FragmentEmployeesBinding.inflate(inflater, container, false)
        return binding.root
    }

    override fun onViewCreated(view: View, savedInstanceState: Bundle?) {
        super.onViewCreated(view, savedInstanceState)
        binding.etVacationYear.setText(Calendar.getInstance().get(Calendar.YEAR).toString())

        adapter = EmployeeAdapter(
            onEdit = { showEmployeeDialog(it) },
            onDelete = { emp ->
                requireContext().confirmDialog(message = "Biztosan törlöd \"${emp.name}\" dolgozót? Ez a beosztási adatait is érvényteleníti.") {
                    lifecycleScope.launch {
                        repo().deleteEmployee(emp)
                        loadEmployees()
                        toast("Dolgozó törölve.")
                    }
                }
            }
        )
        binding.employeesRecyclerView.layoutManager = LinearLayoutManager(requireContext())
        binding.employeesRecyclerView.adapter = adapter
        binding.fabAddEmployee.setOnClickListener { showEmployeeDialog(null) }
        binding.etVacationYear.setOnFocusChangeListener { _, hasFocus -> if (!hasFocus) loadEmployees() }

        loadEmployees()
    }

    override fun onResume() {
        super.onResume()
        loadEmployees()
    }

    private fun currentYear(): Int = binding.etVacationYear.text.toString().toIntOrNull() ?: Calendar.getInstance().get(Calendar.YEAR)

    private fun loadEmployees() {
        lifecycleScope.launch {
            val year = currentYear()
            val groups = repo().getGroupsWithShiftTypes().sortedBy { it.group.name.lowercase() }
            val employees = repo().getEmployees()
            val items = mutableListOf<EmployeeListItem>()
            groups.forEach { gws ->
                val emps = employees.filter { it.groupId == gws.group.id }.sortedBy { it.name.lowercase() }
                if (emps.isEmpty()) return@forEach
                items.add(EmployeeListItem.Header(gws.group.name))
                emps.forEach { emp ->
                    val used = repo().yearVacationUsed(emp.id, year)
                    items.add(EmployeeListItem.Row(emp, formatNum(emp.employmentFactor), used, emp.maxVacationDays - used))
                }
            }
            adapter.submit(items)
        }
    }

    private fun showEmployeeDialog(existing: EmployeeEntity?) {
        lifecycleScope.launch {
            val groups = repo().getGroupsWithShiftTypes().sortedBy { it.group.name.lowercase() }
            if (groups.isEmpty()) {
                toast("Előbb hozz létre legalább egy munkacsoportot a Csoportok fülön.")
                return@launch
            }
            val dialogBinding = DialogEmployeeEditBinding.inflate(LayoutInflater.from(requireContext()))
            val groupNames = groups.map { it.group.name }
            dialogBinding.spGroup.adapter = ArrayAdapter(requireContext(), android.R.layout.simple_spinner_dropdown_item, groupNames)

            val existingGroupIndex = existing?.let { e -> groups.indexOfFirst { it.group.id == e.groupId } } ?: 0
            dialogBinding.spGroup.setSelection(if (existingGroupIndex >= 0) existingGroupIndex else 0)

            if (existing != null) {
                dialogBinding.etEmpName.setText(existing.name)
                dialogBinding.etFactor.setText(formatNum(existing.employmentFactor))
                dialogBinding.etMaxVacation.setText(existing.maxVacationDays.toString())
                dialogBinding.etNotes.setText(existing.notes)
            } else {
                dialogBinding.etFactor.setText("1")
                dialogBinding.etMaxVacation.setText("20")
            }

            AlertDialog.Builder(requireContext())
                .setTitle(if (existing == null) "Új dolgozó" else "Dolgozó szerkesztése")
                .setView(dialogBinding.root)
                .setNegativeButton("Mégse", null)
                .setPositiveButton("Mentés", null)
                .create()
                .apply {
                    setOnShowListener {
                        getButton(AlertDialog.BUTTON_POSITIVE).setOnClickListener {
                            val name = dialogBinding.etEmpName.text.toString().trim()
                            val factor = dialogBinding.etFactor.text.toString().toDoubleOrNull()
                            val maxVacRaw = dialogBinding.etMaxVacation.text.toString()
                            val notes = dialogBinding.etNotes.text.toString().trim()
                            val selectedGroup: GroupWithShiftTypes = groups[dialogBinding.spGroup.selectedItemPosition]

                            if (name.isEmpty()) { toast("A név megadása kötelező."); return@setOnClickListener }
                            val maxVac = maxVacRaw.toIntOrNull()
                            if (maxVac == null || maxVac < 0) {
                                toast("A max. kiadható szabadság megadása kötelező (0 vagy több).")
                                return@setOnClickListener
                            }

                            val employee = EmployeeEntity(
                                id = existing?.id ?: 0,
                                name = name,
                                groupId = selectedGroup.group.id,
                                employmentFactor = if (factor == null || factor <= 0) 1.0 else factor,
                                maxVacationDays = maxVac,
                                notes = notes
                            )
                            lifecycleScope.launch {
                                repo().saveEmployee(employee)
                                loadEmployees()
                                toast("Dolgozó elmentve.")
                                dismiss()
                            }
                        }
                    }
                }
                .show()
        }
    }

    private fun formatNum(d: Double): String = if (d == d.toLong().toDouble()) d.toLong().toString() else d.toString()

    override fun onDestroyView() {
        super.onDestroyView()
        _binding = null
    }
}
