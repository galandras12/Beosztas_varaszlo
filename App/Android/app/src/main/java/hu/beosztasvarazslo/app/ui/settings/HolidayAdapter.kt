package hu.beosztasvarazslo.app.ui.settings

import android.view.LayoutInflater
import android.view.ViewGroup
import androidx.recyclerview.widget.RecyclerView
import hu.beosztasvarazslo.app.databinding.ItemHolidayBinding

data class HolidayRow(val date: String, val name: String, val isAuto: Boolean, val isRemoved: Boolean)

class HolidayAdapter(
    private var rows: List<HolidayRow> = emptyList(),
    private val onToggleAuto: (HolidayRow) -> Unit,
    private val onDeleteExtra: (HolidayRow) -> Unit
) : RecyclerView.Adapter<HolidayAdapter.VH>() {

    fun submit(newRows: List<HolidayRow>) {
        rows = newRows
        notifyDataSetChanged()
    }

    inner class VH(val binding: ItemHolidayBinding) : RecyclerView.ViewHolder(binding.root)

    override fun onCreateViewHolder(parent: ViewGroup, viewType: Int): VH {
        val binding = ItemHolidayBinding.inflate(LayoutInflater.from(parent.context), parent, false)
        return VH(binding)
    }

    override fun getItemCount() = rows.size

    override fun onBindViewHolder(holder: VH, position: Int) {
        val row = rows[position]
        holder.binding.tvDate.text = row.date
        holder.binding.tvName.text = row.name
        holder.binding.tvType.text = if (row.isAuto) "automatikus" else "egyéni"
        holder.binding.root.alpha = if (row.isRemoved) 0.45f else 1f
        if (row.isAuto) {
            holder.binding.btnAction.text = if (row.isRemoved) "Visszaállítás" else "Kikapcsolás"
            holder.binding.btnAction.setOnClickListener { onToggleAuto(row) }
        } else {
            holder.binding.btnAction.text = "Törlés"
            holder.binding.btnAction.setOnClickListener { onDeleteExtra(row) }
        }
    }
}
