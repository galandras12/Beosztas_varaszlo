package hu.beosztasvarazslo.app.ui.groups

import android.os.Bundle
import android.view.LayoutInflater
import android.view.View
import android.view.ViewGroup
import androidx.appcompat.app.AlertDialog
import androidx.fragment.app.Fragment
import androidx.lifecycle.lifecycleScope
import androidx.recyclerview.widget.LinearLayoutManager
import hu.beosztasvarazslo.app.data.GroupWithShiftTypes
import hu.beosztasvarazslo.app.data.ShiftTypeEntity
import hu.beosztasvarazslo.app.data.WorkGroupEntity
import hu.beosztasvarazslo.app.databinding.DialogGroupEditBinding
import hu.beosztasvarazslo.app.databinding.FragmentGroupsBinding
import hu.beosztasvarazslo.app.databinding.ItemShiftTypeRowBinding
import hu.beosztasvarazslo.app.logic.GROUP_TYPE_GENERAL
import hu.beosztasvarazslo.app.logic.GROUP_TYPE_OFFICE
import hu.beosztasvarazslo.app.ui.common.confirmDialog
import hu.beosztasvarazslo.app.ui.common.repo
import hu.beosztasvarazslo.app.ui.common.toast
import kotlinx.coroutines.launch

class GroupsFragment : Fragment() {

    private var _binding: FragmentGroupsBinding? = null
    private val binding get() = _binding!!
    private lateinit var adapter: GroupAdapter

    override fun onCreateView(inflater: LayoutInflater, container: ViewGroup?, savedInstanceState: Bundle?): View {
        _binding = FragmentGroupsBinding.inflate(inflater, container, false)
        return binding.root
    }

    override fun onViewCreated(view: View, savedInstanceState: Bundle?) {
        super.onViewCreated(view, savedInstanceState)
        adapter = GroupAdapter(
            onEdit = { showGroupDialog(it) },
            onDelete = { gws ->
                lifecycleScope.launch {
                    val count = repo().employeeCountForGroup(gws.group.id)
                    if (count > 0) {
                        toast("Nem törölhető: $count dolgozó tartozik ehhez a csoporthoz.")
                    } else {
                        requireContext().confirmDialog(message = "Biztosan törlöd a(z) \"${gws.group.name}\" munkacsoportot?") {
                            lifecycleScope.launch {
                                repo().deleteGroup(gws.group)
                                loadGroups()
                                toast("Munkacsoport törölve.")
                            }
                        }
                    }
                }
            }
        )
        binding.groupsRecyclerView.layoutManager = LinearLayoutManager(requireContext())
        binding.groupsRecyclerView.adapter = adapter
        binding.fabAddGroup.setOnClickListener { showGroupDialog(null) }
        loadGroups()
    }

    override fun onResume() {
        super.onResume()
        loadGroups()
    }

    private fun loadGroups() {
        lifecycleScope.launch {
            val groups = repo().getGroupsWithShiftTypes().sortedBy { it.group.name.lowercase() }
            val rows = groups.map { GroupRow(it, repo().employeeCountForGroup(it.group.id)) }
            adapter.submit(rows)
        }
    }

    private fun showGroupDialog(existing: GroupWithShiftTypes?) {
        val dialogBinding = DialogGroupEditBinding.inflate(LayoutInflater.from(requireContext()))
        val shiftRowBindings = mutableListOf<ItemShiftTypeRowBinding>()

        fun addShiftRow(code: String = "", label: String = "", hours: String = "") {
            val rb = ItemShiftTypeRowBinding.inflate(LayoutInflater.from(requireContext()), dialogBinding.shiftTypesContainer, false)
            rb.etCode.setText(code)
            rb.etLabel.setText(label)
            rb.etHours.setText(hours)
            rb.btnRemove.setOnClickListener {
                dialogBinding.shiftTypesContainer.removeView(rb.root)
                shiftRowBindings.remove(rb)
            }
            dialogBinding.shiftTypesContainer.addView(rb.root)
            shiftRowBindings.add(rb)
        }

        fun syncForOfficeType() {
            val isOffice = dialogBinding.rbOffice.isChecked
            dialogBinding.btnAddShiftType.visibility = if (isOffice) View.GONE else View.VISIBLE
            shiftRowBindings.forEach { it.btnRemove.visibility = if (isOffice) View.GONE else View.VISIBLE }
            if (isOffice) {
                dialogBinding.shiftTypesContainer.removeAllViews()
                shiftRowBindings.clear()
                val dh = dialogBinding.etDailyHours.text.toString().ifEmpty { "8" }
                addShiftRow("M", "Munka", dh)
            }
        }

        if (existing != null) {
            dialogBinding.etGroupName.setText(existing.group.name)
            dialogBinding.rbOffice.isChecked = existing.group.type == GROUP_TYPE_OFFICE
            dialogBinding.rbGeneral.isChecked = existing.group.type != GROUP_TYPE_OFFICE
            dialogBinding.etDailyHours.setText(formatNum(existing.group.dailyHours))
            dialogBinding.etStaffPerShift.setText(existing.group.staffPerShift.toString())
            if (existing.shiftTypes.isEmpty()) addShiftRow("M", "Munka", formatNum(existing.group.dailyHours))
            else existing.shiftTypes.forEach { addShiftRow(it.code, it.label, formatNum(it.hours)) }
        } else {
            dialogBinding.etDailyHours.setText("8")
            dialogBinding.etStaffPerShift.setText("0")
            addShiftRow("M", "Munka", "8")
        }

        dialogBinding.rgGroupType.setOnCheckedChangeListener { _, _ -> syncForOfficeType() }
        dialogBinding.etDailyHours.setOnFocusChangeListener { _, hasFocus -> if (!hasFocus) syncForOfficeType() }
        dialogBinding.btnAddShiftType.setOnClickListener { addShiftRow() }
        syncForOfficeType()

        AlertDialog.Builder(requireContext())
            .setTitle(if (existing == null) "Új munkacsoport" else "Munkacsoport szerkesztése")
            .setView(dialogBinding.root)
            .setNegativeButton("Mégse", null)
            .setPositiveButton("Mentés", null)
            .create()
            .apply {
                setOnShowListener {
                    getButton(AlertDialog.BUTTON_POSITIVE).setOnClickListener {
                        val name = dialogBinding.etGroupName.text.toString().trim()
                        if (name.isEmpty()) { toast("A munkacsoport neve kötelező."); return@setOnClickListener }
                        val type = if (dialogBinding.rbOffice.isChecked) GROUP_TYPE_OFFICE else GROUP_TYPE_GENERAL
                        val dailyHours = dialogBinding.etDailyHours.text.toString().toDoubleOrNull() ?: 0.0
                        val staffPerShift = dialogBinding.etStaffPerShift.text.toString().toIntOrNull() ?: 0
                        val shiftTypes = shiftRowBindings.mapNotNull { rb ->
                            val code = rb.etCode.text.toString().trim()
                            if (code.isEmpty()) return@mapNotNull null
                            ShiftTypeEntity(
                                groupId = 0,
                                code = code,
                                label = rb.etLabel.text.toString().trim(),
                                hours = rb.etHours.text.toString().toDoubleOrNull() ?: 0.0
                            )
                        }
                        if (shiftTypes.isEmpty()) { toast("Legalább egy műszaktípus szükséges."); return@setOnClickListener }

                        val group = WorkGroupEntity(
                            id = existing?.group?.id ?: 0,
                            name = name, type = type, dailyHours = dailyHours, staffPerShift = staffPerShift
                        )
                        lifecycleScope.launch {
                            repo().saveGroup(group, shiftTypes)
                            loadGroups()
                            toast("Munkacsoport elmentve.")
                            dismiss()
                        }
                    }
                }
            }
            .show()
    }

    private fun formatNum(d: Double): String = if (d == d.toLong().toDouble()) d.toLong().toString() else d.toString()

    override fun onDestroyView() {
        super.onDestroyView()
        _binding = null
    }
}
