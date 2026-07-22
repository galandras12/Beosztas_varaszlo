package hu.beosztasvarazslo.app.ui.employees

import android.view.LayoutInflater
import android.view.ViewGroup
import androidx.recyclerview.widget.RecyclerView
import hu.beosztasvarazslo.app.data.EmployeeEntity
import hu.beosztasvarazslo.app.databinding.ItemEmployeeBinding
import hu.beosztasvarazslo.app.databinding.ItemEmployeeHeaderBinding

sealed class EmployeeListItem {
    data class Header(val title: String) : EmployeeListItem()
    data class Row(val employee: EmployeeEntity, val employmentFactor: String, val used: Int, val remaining: Int) : EmployeeListItem()
}

class EmployeeAdapter(
    private var items: List<EmployeeListItem> = emptyList(),
    private val onEdit: (EmployeeEntity) -> Unit,
    private val onDelete: (EmployeeEntity) -> Unit
) : RecyclerView.Adapter<RecyclerView.ViewHolder>() {

    companion object { const val TYPE_HEADER = 0; const val TYPE_ROW = 1 }

    fun submit(newItems: List<EmployeeListItem>) {
        items = newItems
        notifyDataSetChanged()
    }

    override fun getItemViewType(position: Int) = when (items[position]) {
        is EmployeeListItem.Header -> TYPE_HEADER
        is EmployeeListItem.Row -> TYPE_ROW
    }

    override fun getItemCount() = items.size

    inner class HeaderVH(val binding: ItemEmployeeHeaderBinding) : RecyclerView.ViewHolder(binding.root)
    inner class RowVH(val binding: ItemEmployeeBinding) : RecyclerView.ViewHolder(binding.root)

    override fun onCreateViewHolder(parent: ViewGroup, viewType: Int): RecyclerView.ViewHolder {
        val inflater = LayoutInflater.from(parent.context)
        return if (viewType == TYPE_HEADER) {
            HeaderVH(ItemEmployeeHeaderBinding.inflate(inflater, parent, false))
        } else {
            RowVH(ItemEmployeeBinding.inflate(inflater, parent, false))
        }
    }

    override fun onBindViewHolder(holder: RecyclerView.ViewHolder, position: Int) {
        when (val item = items[position]) {
            is EmployeeListItem.Header -> (holder as HeaderVH).binding.root.text = item.title
            is EmployeeListItem.Row -> {
                val vh = holder as RowVH
                vh.binding.tvEmployeeName.text = item.employee.name
                vh.binding.tvEmployeeDetails.text =
                    "Munkaidő-arány: ${item.employmentFactor} · Max. szabadság: ${item.employee.maxVacationDays} nap · " +
                    "Kivett: ${item.used} nap · Hátralévő: ${item.remaining} nap"
                vh.binding.btnEditEmployee.setOnClickListener { onEdit(item.employee) }
                vh.binding.btnDeleteEmployee.setOnClickListener { onDelete(item.employee) }
            }
        }
    }
}
