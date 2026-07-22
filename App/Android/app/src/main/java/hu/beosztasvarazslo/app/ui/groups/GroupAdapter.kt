package hu.beosztasvarazslo.app.ui.groups

import android.view.LayoutInflater
import android.view.ViewGroup
import androidx.recyclerview.widget.RecyclerView
import hu.beosztasvarazslo.app.data.GroupWithShiftTypes
import hu.beosztasvarazslo.app.databinding.ItemGroupBinding
import hu.beosztasvarazslo.app.logic.GROUP_TYPE_OFFICE

data class GroupRow(val gws: GroupWithShiftTypes, val employeeCount: Int)

class GroupAdapter(
    private var rows: List<GroupRow> = emptyList(),
    private val onEdit: (GroupWithShiftTypes) -> Unit,
    private val onDelete: (GroupWithShiftTypes) -> Unit
) : RecyclerView.Adapter<GroupAdapter.VH>() {

    fun submit(newRows: List<GroupRow>) {
        rows = newRows
        notifyDataSetChanged()
    }

    inner class VH(val binding: ItemGroupBinding) : RecyclerView.ViewHolder(binding.root)

    override fun onCreateViewHolder(parent: ViewGroup, viewType: Int): VH {
        val binding = ItemGroupBinding.inflate(LayoutInflater.from(parent.context), parent, false)
        return VH(binding)
    }

    override fun getItemCount() = rows.size

    override fun onBindViewHolder(holder: VH, position: Int) {
        val row = rows[position]
        val g = row.gws.group
        val typeLabel = if (g.type == GROUP_TYPE_OFFICE) "Iroda (hétfő-péntek)" else "Általános (váltásos/napi)"
        holder.binding.tvGroupName.text = "${g.name}  •  $typeLabel"
        val shiftDesc = row.gws.shiftTypes.joinToString(", ") { "${it.label} (${it.code}, ${it.hours} óra)" }
        holder.binding.tvGroupDetails.text = buildString {
            append("Napi óraszám: ${g.dailyHours} óra · Műszakok: ").append(shiftDesc.ifEmpty { "—" })
            if (g.staffPerShift > 0) append(" · Létszám/műszak: ${g.staffPerShift} fő")
            append(" · Dolgozók: ${row.employeeCount} fő")
        }
        holder.binding.btnEditGroup.setOnClickListener { onEdit(row.gws) }
        holder.binding.btnDeleteGroup.setOnClickListener { onDelete(row.gws) }
    }
}
